# 6. Accept, for now, the seeded default administrator credential (G7, ASVS V6.3.2)

* Status: superseded by ADR 0009 - option B was implemented the same day (originally accepted as a risk acceptance, not a fix)
* Date: 2026-09-18

## Context and Problem Statement

`SeedData.ApplyAsync` (`src/Voltflow.Infrastructure/Seed/SeedData.cs`, lines 61-63) creates an approved,
email-verified administrator when no user with the configured email exists. The email is `Seed:AdminEmail`
(default `admin@voltflow.com`); the password is `Seed:AdminPassword`, which falls back to a literal written
in that file. `docker-compose.deploy.yml` sets `Database__SeedOnStartup: "true"` and passes no `Seed__*`
value, and `deploy.yml` writes none into the environment - so an environment created from the deploy compose
on an empty database starts with an administrator whose credentials are in the source.

The source is public: `murat-kacar/voltaj` is a PUBLIC repository (checked with `gh repo view` on
2026-09-18) and the literal is in every commit since the first (`e744f9d`, 2026-09-15). It appears in 78
tracked files: `SeedData.cs`, `tests/requests.http`, `tests/ui-e2e/test-helpers/auth.ts` and 75 scripts under
`tests/api-e2e/`. Git history cannot be un-published, so whatever is done to the code now, the credential has
to be treated as known to anyone.

Rules involved: **G7** ("Secrets are not kept in the repo") and **ASVS 5.0.0 V6.3.2** (level 1, so inside the
declared Level 2): default accounts such as "root", "admin" or "sa" must be absent or disabled. V6.3.2 is
recorded as `not-met` in `docs/security/asvs-5.0.0-audit.json`.

## Considered Options

* **A. Remove the default.** Require `Seed:AdminEmail` and `Seed:AdminPassword` outside Development and fail
  at startup when they are missing (V3), supply them from deployment secrets, and make the E2E scripts read
  the credential from the environment instead of the literal.
* **B. Keep the default in Development only** (the pattern of ADR 0002) and require explicit values
  everywhere else.
* **C. Accept the risk for now, contain it so it cannot spread, and report it.**

## Decision Outcome

Chosen option: **C**, by the owner's decision of 2026-09-18 ("Kodu değiştirme, raporla" - do not change the
code, report it). The deferral is a scheduling decision, not a judgement that the risk is small: it is a
critical finding (CWE-798) and options A/B remain the fix.

Containment that is in place:

* `.gitleaks.toml` has a custom rule, `voltflow-default-admin-password`, and allows the literal only in
  `src/Voltflow.Infrastructure/Seed/SeedData.cs` and under `tests/`. `make check-secrets` fails if it appears
  anywhere else, so the exposure cannot grow - and once option A/B lands, deleting that one allowlist entry
  makes the gate enforce the removal.
* The ASVS audit carries V6.3.2 as `not-met` with this ADR as evidence, so the finding is listed by
  `make check-asvs` until it is fixed.

### Consequences

* Bad: any environment already created from the deploy compose on a fresh database has an administrator
  whose credentials are public, unless someone changed the stored password by hand. That is a present
  exposure, not a potential one.
* Bad, found while writing this ADR and after the owner decided: **the application cannot change a password
  outside Development.** `AuthService.RequestPasswordResetAsync` stores only a SHA-256 of a random token and
  hands the token to nobody - there is no mail or SMS sender anywhere in `src/`, and
  `EVT_01401_PasswordResetRequested` is defined in `VoltflowTaxonomy` but never published. There is no
  change-password endpoint either. So `POST /auth/password-reset/complete` only succeeds through the
  Development-only `000000` shortcut, and "rotate the password through the app" is not an available
  mitigation.
* What can be done without touching the code (derived from reading it, not exercised against a deployed
  environment): register a second account (`POST /auth/register`), approve it and give it the Admin role while
  signed in as the default administrator (`POST /auth/users/{id}/approve`, `POST /auth/users/{id}/roles`),
  then set the default account's `IsApproved` to false in the database - the seed creates a missing user but
  never re-approves an existing one, so that holds - or replace its `PasswordHash` with a hash produced by
  ASP.NET Core Identity's `PasswordHasher<AppUser>`. Do not delete the row: with `SeedOnStartup` on, the next
  start would recreate it with the default password.
* Follow-up, not done here: option A/B in code; a working password reset/change path (deliver the token, or
  add an authenticated change-password endpoint); E2E credentials taken from the environment.

### Revisit

Now, if any environment seeded from the deploy compose holds real data or is reachable from the internet;
otherwise before the first such environment exists. Also when the E2E credential plumbing is done. When
option A/B lands: delete the `SeedData.cs` allowlist entry in `.gitleaks.toml` and set V6.3.2 to `met` with
the evidence.
