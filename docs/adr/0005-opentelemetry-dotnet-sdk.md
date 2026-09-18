# 5. Add the OpenTelemetry .NET SDK, ASP.NET Core instrumentation and OTLP exporter (R4, T1)

* Status: accepted
* Date: 2026-09-18

## Context and Problem Statement

T1 requires instrumentation to use OpenTelemetry, with names and attributes that follow the semantic
conventions; T2 requires a W3C Trace Context that does not break at service boundaries. Today the API
creates its own `ActivitySource` server span in `OperationTraceMiddleware` with legacy attribute names
(`http.method`, `http.status_code`) and a span name that embeds raw path ids, and there is no
OpenTelemetry SDK in any project - so nothing is collected or exported at all. `AGENTS.md`'s `Project`
section already names "the OpenTelemetry .NET SDK" as the tracing tool; R4 still asks for the concrete
packages to be recorded.

## Considered Options

* **A. Three packages: `OpenTelemetry.Extensions.Hosting` (SDK + DI/hosting integration) and
  `OpenTelemetry.Exporter.OpenTelemetryProtocol` in `Voltflow.Infrastructure`, so the API and the Worker
  share one registration; `OpenTelemetry.Instrumentation.AspNetCore` in `Voltflow.Api` only (a web
  project).** All three at 1.18.0, stable.
* B. Also add EF Core / Npgsql / HttpClient / runtime instrumentation packages now.
* C. Keep the hand-made `ActivitySource` and add nothing.
* D. Also add the Prometheus exporter to replace the hand-rolled `/metrics` text endpoint.

## Decision Outcome

Chosen option: **A**.

* The ASP.NET Core instrumentation package produces the server span (standard name, standard
  `http.*`/`url.*` attributes, W3C extraction of an incoming `traceparent`) and the standard HTTP server
  metrics - so `OperationTraceMiddleware` stops creating a second server span and only adds
  `voltflow.*` attributes to the existing one. That removes the invented span and the non-conformant
  attributes rather than renaming them.
* The OTLP exporter is only enabled when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (O4: configuration comes
  from the environment); with it unset the SDK still records spans in-process, nothing tries to connect
  to a collector, and tests stay hermetic.
* Instrumentation packages live where their framework dependency already exists: the Worker and
  Infrastructure stay free of the ASP.NET Core shared framework.

Option B was deferred, not rejected: database and outbound-HTTP spans are useful but nothing in T1/T2
needs them yet, and each is a further dependency under R4. Option C was rejected because nothing
would be exported. Option D was deferred because the Prometheus exporter is still a pre-release package
and replacing `/metrics` is a design decision to take with the owner, not here.

### Consequences

* Good: spans follow the semantic conventions by construction, the trace continues across the
  transactional outbox (see `OutboxMessage.TraceParent`) into the Worker, and exporting to any OTLP
  collector needs only an environment variable.
* Neutral: the existing hand-rolled `/metrics` endpoint is untouched; its two `System.Diagnostics.Metrics`
  counters (non-conformant names, raw-path label, never exported) were removed because the standard
  ASP.NET Core metrics now cover requests, failures and duration.
* Follow-up: the frontend is not instrumented (OpenTelemetry JS) - the trace currently starts at the
  API server span. That is a separate decision (CORS for `traceparent`, collector exposure).
