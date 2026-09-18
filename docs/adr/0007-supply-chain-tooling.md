# 7. Supply-chain tooling: gitleaks, CycloneDX, dependency audit, license policy, ASVS data (R4, G1, G7, G8, G10)

* Status: accepted
* Date: 2026-09-18

## Context and Problem Statement

G7 needs a secret scan and a vulnerability scan that can fail a build; G8 needs an SBOM generator; G10 needs
a license policy over what ships; G1 needs the ASVS 5.0 requirement list to audit against. R4 asks for every
new tool or dependency to be recorded here. Constraints: the same command must run on a laptop and in CI
(`make` targets that call `scripts/`, so CI holds no logic of its own), every tool is pinned, no tool needs a
running Docker daemon, and nothing is installed system-wide.

## Decision Outcome

| Need | Tool | Pin and verification |
|-|-|-|
| Secret scan (G7) | gitleaks 8.30.1 (MIT), pre-built binary bootstrapped by `scripts/install-gitleaks.sh` into the git-ignored `.tools/` | The SHA-256 of every supported archive is committed in the script; a mismatch deletes the download and fails. Rules: `.gitleaks.toml` extends the default rule set with one custom rule (ADR 0006) |
| Vulnerability scan (G7) | `dotnet list package --vulnerable --include-transitive` (.NET SDK) and `npm audit --audit-level=low` - no new dependency | `scripts/check-dependencies.sh` parses the JSON, because the .NET command exits 0 even when it finds something, and fails closed when a feed is unreachable (a `problems` entry is "unknown", not "clean") |
| SBOM (G8) | CycloneDX .NET tool `CycloneDX` 6.2.0 (Apache-2.0) as a repo-local tool; `npm sbom --sbom-format cyclonedx` (built into npm) | Version pinned in `dotnet-tools.json` with `rollForward: false`; `scripts/validate-sbom.mjs` checks structure and that a known component is present, so an empty or truncated BOM fails |
| License policy (G10) | Own script `scripts/check-licenses.mjs` (no dependency) over the SBOMs' SPDX data, with `license-policy.json` | SPDX expressions (`AND`, `OR`, `WITH`, parentheses) are evaluated properly; an exception must carry a reason |
| ASVS data (G1) | The official `OWASP/ASVS` release `v5.0.0_release`, flat JSON, converted once by `scripts/asvs-init.mjs` into `docs/security/asvs-5.0.0-audit.json` | The asset name and SHA-256 are recorded in the file header; the requirement text is CC BY-SA 4.0 and is attributed there. Not a build or runtime dependency |

Rejected, and why:

* **The gitleaks Docker image.** It needs a running Docker daemon (there was none when this was set up) and a
  container cannot follow a git worktree's `.git` pointer file to the main repository's object store without
  extra mounts.
* **GitHub secret scanning as the only mechanism.** `make` cannot invoke it, so it is not a gate a developer
  can run and prove before pushing.
* **A per-ecosystem license checker for each of NuGet and npm.** Two tools, two policies to keep in step, two
  new dependencies (R4) - while the SBOM already carries the SPDX data for both.

### Consequences

* Good: `make check-secrets`, `make audit`, `make license-policy` and `make check-asvs` share one interface
  and were each proven with planted violations (and, for the tools that fail closed, with an unavailable
  source), not just on the clean repository.
* Neutral: the first `make check-secrets` on a machine downloads about 20 MB; `.tools/` and `sbom/` are
  git-ignored.
* The license allow-list (MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC, 0BSD, PostgreSQL, BlueOak-1.0.0,
  CC0-1.0, Unlicense, Zlib) is a conservative, permissive-only default proposed together with the tooling;
  the owner has not reviewed it license by license. Widening it, or adding an exception, is an ordinary
  reviewed diff of `license-policy.json`.
* Limits, stated plainly: the scripts have been run on Windows with Git Bash only; the Linux and macOS
  branches of `install-gitleaks.sh` and the CI workflow `.github/workflows/security.yml` have not been run.
  The vulnerability gate is only as good as NuGet's and npm's advisory feeds.
