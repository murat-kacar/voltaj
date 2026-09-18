#!/usr/bin/env bash
# G7 gate: a known-vulnerable dependency fails the build - NuGet (transitive packages included) and npm
# (frontend and root). Any severity fails: the way out is an upgrade, not a threshold. The scan fails
# CLOSED - if the advisory source cannot be reached the result is "unknown", never a silent pass.
# Usage: check-dependencies.sh [nuget target: a .sln or .csproj, default Voltflow.sln]
set -uo pipefail

target="${1:-Voltflow.sln}"
fail=0

echo "check-dependencies: NuGet audit of $target (transitive packages included)..." >&2
raw=$(dotnet list "$target" package --vulnerable --include-transitive --format json 2>&1)
json=$(printf '%s\n' "$raw" | sed -n '/^{/,$p')

if [ -z "$json" ]; then
  echo "check-dependencies: FAILED - 'dotnet list package' produced no JSON, so the audit result is unknown:" >&2
  printf '%s\n' "$raw" | tail -n 5 >&2
  fail=1
else
  report=$(printf '%s' "$json" | node -e '
    let text = "";
    process.stdin.on("data", chunk => text += chunk).on("end", () => {
      const audit = JSON.parse(text);
      const problems = (audit.problems || []).map(p => `${p.level || "problem"}: ${p.text}`);
      const hits = [];
      for (const project of audit.projects || [])
        for (const framework of project.frameworks || [])
          for (const kind of ["topLevelPackages", "transitivePackages"])
            for (const pkg of framework[kind] || [])
              for (const vuln of pkg.vulnerabilities || [])
                hits.push(`${pkg.id} ${pkg.resolvedVersion} [${vuln.severity}] ${vuln.advisoryurl}`);
      if (problems.length) { console.log("audit could not complete:\n" + problems.join("\n")); process.exit(4); }
      if (hits.length) { console.log([...new Set(hits)].join("\n")); process.exit(3); }
    });')
  status=$?
  if [ "$status" -eq 3 ]; then
    echo "check-dependencies: FAILED - vulnerable NuGet packages:" >&2
    printf '%s\n' "$report" >&2
    fail=1
  elif [ "$status" -ne 0 ]; then
    echo "check-dependencies: FAILED - NuGet audit result is unknown:" >&2
    printf '%s\n' "$report" >&2
    fail=1
  fi
fi

# --audit-level=low: any advisory fails. npm audit exits non-zero on findings AND when it cannot reach
# the registry, which is the fail-closed behaviour wanted here.
for dir in frontend .; do
  echo "check-dependencies: npm audit in $dir ..." >&2
  (cd "$dir" && npm audit --audit-level=low) || fail=1
done

if [ "$fail" -eq 1 ]; then exit 1; fi
echo "check-dependencies: no known-vulnerable NuGet or npm dependencies."
