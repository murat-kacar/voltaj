#!/usr/bin/env bash
# D1/D2/D5 gate: every existing bounded context must have a domain-map file.
set -u

contexts="identity customers quotes workorders finance inventory projects reminders"
missing=0

for ctx in $contexts; do
  file="docs/domain-map/${ctx}.md"
  if [ ! -f "$file" ]; then
    echo "MISSING: $file (D1/D2/D5 — see docs/architecture/voltflow-agents-compliance-roadmap.md Phase 1)" >&2
    missing=1
  fi
done

if [ "$missing" -eq 1 ]; then
  exit 1
fi

echo "check-domain-map: all bounded contexts have a domain-map file."
