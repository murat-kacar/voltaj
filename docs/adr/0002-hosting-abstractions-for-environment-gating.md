# 2. Add Microsoft.Extensions.Hosting.Abstractions to Voltflow.Application (R4)

* Status: accepted
* Date: 2026-09-18

## Context and Problem Statement

While drafting the Identity bounded-context domain map (D1), a hardcoded authentication bypass was
found in `AuthService.cs`: both registration (`RegisterAsync`) and password reset
(`CompletePasswordResetAsync`) accepted the literal string `"000000"` as a master OTP/token,
unconditionally, in every environment. For password reset in particular, this let anyone reset any
user's password given only their email address — a full account-takeover path (CWE-798, hardcoded
credentials), with no environment or config guard anywhere in the method.

Murat's decision: keep the shortcut for local/QA convenience, but make it inert outside
`Development`. That requires the running environment to be checkable from `Voltflow.Application`,
which — being a plain class library, not an ASP.NET Core web host — did not previously have access to
`IHostEnvironment`.

## Decision Drivers

* R4 (no bloat): a new dependency needs an ADR; this one is being recorded rather than skipped.
* R3 (no invention): `IHostEnvironment`/`.IsDevelopment()` is the standard, industry-wide .NET
  mechanism for this exact check — inventing a custom "is this dev" flag would violate R3 instead.
* Precedent already in this file: `AuthService` already takes `Microsoft.AspNetCore.Identity`'s
  `IPasswordHasher<AppUser>` directly as a constructor dependency — a standard framework abstraction
  injected straight into Application, not re-wrapped in a bespoke interface. This is the same pattern.

## Considered Options

* **A. Add `Microsoft.Extensions.Hosting.Abstractions` (the lightweight, interface-only package) to
  `Voltflow.Application.csproj` and inject `IHostEnvironment` directly into `AuthService`.**
* B. Define a bespoke `IEnvironmentContext` interface in Application, implemented in Infrastructure by
  wrapping `IHostEnvironment`.
* C. Pass an `Action<AuthOptions>`-style config flag instead of checking the environment at all.

## Decision Outcome

Chosen option: **A**. `Microsoft.Extensions.Hosting.Abstractions` carries no runtime/hosting baggage —
it is the interface-only package (`IHostEnvironment`, `IHostingEnvironment` extension methods), safe
for a class library to depend on, and it is what every other .NET project in this solution already
uses transitively via the full `Microsoft.Extensions.Hosting` package (see
`Voltflow.Worker.csproj`). Pinned to `10.0.12` to match the version already used elsewhere in the
solution.

Option B was rejected: wrapping a standard framework abstraction in a second, custom interface is
exactly the kind of invented indirection R3 warns against, for no behavioral gain — `IHostEnvironment`
is already an interface, already mockable, already the tool every .NET engineer reaching for this
capability expects.

Option C was rejected: environment detection is what `IHostEnvironment` is for; a separate config flag
would duplicate information ASP.NET Core already tracks (`ASPNETCORE_ENVIRONMENT`) and could drift
out of sync with it.

### Consequences

* Good: `AuthService`'s two `"000000"` branches are now gated by `_environment.IsDevelopment()`, with
  regression tests (`AuthServiceTests.RegisterAsync_MasterOtp_*`,
  `AuthServiceTests.CompletePasswordResetAsync_MasterOtp_ShouldFail_OutsideDevelopment`) proving the
  bypass is inert outside Development.
  Bad: none identified — the package adds no transitive dependencies beyond what the solution already
  carries.
