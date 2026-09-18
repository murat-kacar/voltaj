# 9. No default administrator outside Development (G7, ASVS V6.3.2) - supersedes the decision of ADR 0006

* Status: accepted - supersedes the decision of ADR 0006 (option B of that ADR)
* Date: 2026-09-18

## Context and Problem Statement

ADR 0006 accepted, for the time being, that `SeedData` creates an approved administrator with a built-in
default password whenever `Seed:AdminEmail` / `Seed:AdminPassword` are not configured, and it recorded that the
application has no working password-change path outside Development. The owner then asked for the risk to be
removed. It was not hypothetical: `docker-compose.deploy.yml` turns seeding on without passing either value, so
the production database had been seeded with that administrator on its first deploy.

## Considered Options

The options of ADR 0006: **A** (no default at all - everything configured, the E2E scripts read the credential
from the environment) and **B** (the default exists in Development only, explicit configuration everywhere else).

## Decision Outcome

Chosen option: **B**. It removes the exposure where it matters (any deployed environment) and leaves the local
workflow and the 78 files that use the Development default untouched.

* `SeedData.ApplyAsync` takes the `IHostEnvironment` (the mechanism of ADR 0002). Outside Development an
  administrator is seeded only when both `Seed:AdminEmail` and `Seed:AdminPassword` are configured. Otherwise the
  seed still creates roles and reference data, logs a warning and creates no user.
* The seed never modifies a user that already exists, so restarting the API cannot revive an account that was
  disabled by hand.
* Development is unchanged: the default administrator, `tests/requests.http` and the E2E scripts keep working
  against a local run.
* `docker-compose.deploy.yml` and `deploy.yml` forward two optional secrets, `SEED_ADMIN_EMAIL` and
  `SEED_ADMIN_PASSWORD`, to `Seed__AdminEmail` / `Seed__AdminPassword`, so a fresh environment can be given its
  first administrator without any default. Empty - the state of every existing environment - changes nothing.
* Tests: `SeedDataTests` (six), proven to fail when the environment gate is disabled.

### Consequences

* Good: a new production or staging environment has no known credentials; ASVS V6.3.2 holds by construction
  outside Development.
* Neutral: the default still exists in Development and its literal stays in `SeedData.cs`, so the gitleaks
  allowlist entry for that file stays.
* Bad / follow-up:
  * A database that was seeded earlier keeps its default account until it is disabled by hand - the roadmap
    records what was done on the live server; any other environment (for example staging) needs the same check.
  * A fresh environment has no administrator until the two secrets are set (see the runbook).
  * The application still has no working password reset or change path outside Development (ADR 0006).
  * The E2E suite had been run against production and left approved test accounts and test data there. It
    must only be pointed at a local or disposable environment.
