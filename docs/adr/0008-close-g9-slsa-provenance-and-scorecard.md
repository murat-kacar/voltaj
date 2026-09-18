# 8. Close G9 (SLSA provenance) and the OpenSSF Scorecard half of G10, for now

* Status: accepted
* Date: 2026-09-18

## Context and Problem Statement

G9 (SHOULD): "Build provenance is produced at SLSA L3 level, signed with Sigstore/cosign". G10 (SHOULD):
"OpenSSF Scorecard and SPDX license policy run in CI". `AGENTS.md` names G9 among the rules that are typical
candidates for closure on scale grounds, and asks for that to be decided explicitly per project.

What is true today (checked 2026-09-18):

* `.github/workflows/deploy.yml` builds three images with `docker/build-push-action@v5`, pushes them to GHCR
  (`packages: write`) and deploys to a single host over SSH. It has no `id-token: write` permission and
  produces no attestation.
* Nothing consumes provenance: no deploy step verifies an attestation, and the images have no consumer other
  than that one host.
* The repository is public, so attestations and Scorecard results would be published publicly.
* The actions in `ci.yml` and `deploy.yml` are referenced by tag (for example `actions/checkout@v4`), not by
  commit SHA. The new `security.yml` pins by SHA; the older workflows are not touched in this pass.
* Whether GitHub branch protection and review are on is still an open owner question (A3/A7, roadmap Phase 8).

## Considered Options

* **A. Implement both now** - an attestation step in `deploy.yml` plus a Scorecard workflow.
* **B. Close both by ADR, with explicit reopen triggers.**

## Decision Outcome

Chosen option: **B**.

* **G9.** The owner decided on 2026-09-18 to close it by this ADR rather than modify the deploy workflow.
  Reason: provenance nobody verifies is an unchecked artifact, and SLSA L3 additionally needs an isolated,
  hardened build through a reusable workflow - a larger change to the deploy pipeline than this pass takes on.
* **G10, the Scorecard half only.** Closed in the same ADR on the same footing. Scorecard includes checks for
  branch protection, code review and pinned dependencies - exactly the open decisions above - so running it
  now would only report those decisions as failures. The other half of G10, the SPDX license policy, is
  implemented (`make license-policy`, ADR 0007) and is **not** closed.

### Consequences

* Neutral: no provenance is produced and no Scorecard runs; the deployed images cannot be traced back to a
  build by a signature.
* Reopen G9 when the images gain a consumer other than the single deploy host, when a deploy step that
  verifies provenance (for example `gh attestation verify`) is introduced, or when a second deployment target
  appears.
* Reopen the Scorecard half after A3/A7 is decided and the workflow actions are pinned by SHA (roadmap Phase
  8); look at the first result privately before publishing anything.
* Both rows in the "Closed rules in this project" table of `AGENTS.md` are deleted when the rule is reopened.
