# 3. API versioning: implicit v1, versioned path only from the first breaking change

* Status: accepted
* Date: 2026-09-18

## Context and Problem Statement

M7 requires "API versioning is SemVer; breaking change increments version, retired endpoint announced
with `Sunset` header." Today's entire surface is unversioned (`/api/workorders`, `/api/quotes`, ...).
Satisfying M7 could mean either (a) immediately renaming every route to `/api/v1/...`, or (b) treating
today's surface as an implicit v1 and only introducing an explicit version marker at the first actual
breaking change.

## Decision Drivers

* R5 (no scope creep): Phase 2's task is contract-first documentation (M6) and a versioning *policy*
  (M7), not a repo-wide route rename. A rename touches all 9 `Endpoints` files, `frontend/src/api.ts`,
  and every test that hits a hardcoded path (e.g. `ConcurrencyAndIdempotencyTests.cs`,
  `AuthIntegrationTests.cs`) - real blast radius for zero behavior change.
- Principle 1 (no invention): "implicit v1, explicit version from the first breaking change" is an
  established, common REST convention (many public APIs ship this way), not a bespoke scheme.

## Considered Options

* **A. Implicit v1 today; the first breaking change introduces a versioned path (or header) for the
  new behavior, with `Sunset` on the retired one.**
* B. Rename every route to `/api/v1/...` now, before any breaking change exists.

## Decision Outcome

Chosen option: **A**. `openapi.yaml`'s `info.version` is set to `1.0.0` to make this explicit; the
policy itself (what "breaking" means, and the `Sunset`/version-bump mechanism) is recorded here so
it's applied consistently the first time it's actually needed, rather than invented ad hoc under
deadline pressure at that point.

Option B was rejected: it is a same-day repo-wide rename justified only by "the doc says v1", not by
any actual API evolution need — the kind of premature, ceremonial change R5 and Principle 1 both argue
against.

### Consequences

* Good: zero route churn today; M7's actual intent (a defined, followed policy for breaking changes)
  is satisfied without inventing work.
* Neutral: the first real breaking change will need to decide path-based (`/api/v2/...`) vs.
  header-based (`Api-Version` / `Accept` media-type) versioning at that time — this ADR does not
  pre-decide that, only that some version marker plus `Sunset` is required then.
