#!/usr/bin/env bash
# G8 gate: generate the CycloneDX SBOMs of what actually ships, then prove they are real (not empty, not
# malformed) before anything downstream trusts them.
#   backend  - CycloneDX .NET tool, pinned in dotnet-tools.json; test projects and dev dependencies excluded
#   frontend - `npm sbom` (built into npm, no extra dependency); production dependencies only
# Output goes to sbom/ (gitignored); CI uploads it as a build artifact.
set -euo pipefail

mkdir -p sbom

echo "generate-sbom: restoring pinned .NET tools..." >&2
dotnet tool restore >&2

echo "generate-sbom: backend BOM (CycloneDX .NET)..." >&2
dotnet dotnet-CycloneDX Voltflow.sln -o sbom -fn backend.cdx.json -F Json -t -ed -spv 1.6 >&2

echo "generate-sbom: frontend BOM (npm sbom, production dependencies)..." >&2
(cd frontend && npm sbom --sbom-format cyclonedx --omit dev) > sbom/frontend.cdx.json

node scripts/validate-sbom.mjs sbom
