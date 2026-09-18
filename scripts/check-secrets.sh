#!/usr/bin/env bash
# G7 gate: no secrets in the repository - the WHOLE git history and the working tree are scanned with
# gitleaks (pinned + checksum-verified by scripts/install-gitleaks.sh; rules and the recorded exceptions
# live in .gitleaks.toml). Findings are printed redacted, so a real secret is never echoed into a log.
# A public repository keeps history forever, which is why history is scanned and not just the tree.
set -uo pipefail

gitleaks=$(bash scripts/install-gitleaks.sh) || { echo "check-secrets: could not install gitleaks - result unknown" >&2; exit 1; }
fail=0

echo "check-secrets: scanning git history (all refs)..." >&2
"$gitleaks" git . --config .gitleaks.toml --redact --no-banner --verbose --exit-code 1 || fail=1

echo "check-secrets: scanning working tree..." >&2
"$gitleaks" dir . --config .gitleaks.toml --redact --no-banner --verbose --exit-code 1 || fail=1

if [ "$fail" -eq 1 ]; then
  echo "check-secrets: FAILED - see the findings above (values are redacted). Remove the secret AND rotate it:" >&2
  echo "  once committed to a public repository it has to be treated as burned." >&2
  exit 1
fi
echo "check-secrets: no secrets found in git history or the working tree."
