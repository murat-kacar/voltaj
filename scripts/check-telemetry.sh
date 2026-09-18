#!/usr/bin/env bash
# T1 lint: instrumentation uses OpenTelemetry, and names/attributes follow its semantic conventions.
# Static check over src/ (migrations excluded) - deliberately simple grep rules, each one a real
# convention: deprecated attribute names are forbidden, custom span tags live under a known namespace,
# metric instruments use dotted lowercase names, and no second tracing/metrics stack is referenced.
set -u

fail=0
report() { echo "T1 VIOLATION: $1" >&2; fail=1; }

sources=$(find src -name '*.cs' -not -path '*/Migrations/*' -not -path '*/obj/*' -not -path '*/bin/*')

# 1. Deprecated (pre-1.21) attribute names.
deprecated='"(http\.method|http\.status_code|http\.url|http\.target|http\.host|http\.scheme|http\.flavor|http\.user_agent|net\.peer\.name|net\.peer\.port|net\.host\.name|net\.host\.port|net\.transport)"'
for file in $sources; do
  hits=$(grep -nE "SetTag\($deprecated|AddTag\($deprecated" "$file" || true)
  [ -n "$hits" ] && report "$file uses a deprecated semantic-convention attribute name (see OTel HTTP semconv: http.request.method, http.response.status_code, url.*, server.*):
$hits"
done

# 2. Every literal span tag name must sit under a semantic-convention namespace or voltflow.*
allowed='^(voltflow|http|url|error|messaging|db|server|client|network|exception|rpc|code|service|user_agent)\.'
for file in $sources; do
  for name in $(grep -ohE '(SetTag|AddTag)\("[^"]+"' "$file" | sed -E 's/^[A-Za-z]+\("//; s/"$//'); do
    echo "$name" | grep -qE "$allowed" || report "$file sets span tag '$name' outside a semantic-convention namespace or voltflow.*"
  done
done

# 3. Metric instrument names: lowercase, dotted namespace (e.g. voltflow.outbox.dead_letter).
for file in $sources; do
  for name in $(grep -ohE 'Create(Counter|UpDownCounter|Histogram|Gauge|ObservableCounter|ObservableGauge|ObservableUpDownCounter)<[a-z]+>\("[^"]+"' "$file" | sed -E 's/.*\("//; s/"$//'); do
    echo "$name" | grep -qE '^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+$' || report "$file creates metric '$name' - use a dotted lowercase name, not an underscore/Prometheus-style one"
  done
done

# 4. ActivitySource names live under Voltflow.*
for file in $sources; do
  for name in $(grep -ohE 'new\s+(System\.Diagnostics\.)?ActivitySource\("[^"]+"|WorkerActivitySourceName\s*=\s*"[^"]+"' "$file" | sed -E 's/.*"([^"]+)"$/\1/'); do
    echo "$name" | grep -qE '^Voltflow\.' || report "$file declares ActivitySource '$name' outside Voltflow.*"
  done
done

# 5. OpenTelemetry is what is referenced - and no second tracing/metrics stack alongside it.
grep -qE 'PackageReference Include="OpenTelemetry' src/*/*.csproj || report "no OpenTelemetry package is referenced by any project under src/"
other=$(grep -nE 'PackageReference Include="(prometheus-net|App\.Metrics|Jaeger|Zipkin|Datadog\.|NewRelic\.|Microsoft\.ApplicationInsights|Elastic\.Apm|Sentry)' src/*/*.csproj || true)
[ -n "$other" ] && report "a second tracing/metrics stack is referenced next to OpenTelemetry (R3/R4):
$other"

if [ "$fail" -eq 1 ]; then exit 1; fi
echo "check-telemetry: instrumentation is OpenTelemetry-only and follows the naming conventions."
