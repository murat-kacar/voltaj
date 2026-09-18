# Domain Map — Identity & Access

Bounded context per D1/D2/D5/D6. Derived from `docs/architecture/voltflow-domain-models.md` §1.2–1.3
and `voltflow-workflows-and-failure-modes.md` §1 (VUT `01.*`), formalized into Event Storming
vocabulary, cross-checked against `src/Voltflow.Domain/Identity/`, `AuthService.cs`, `AuthEndpoints.cs`.

## Classification (D6)

**Generic subdomain.** Authentication/authorization is a solved problem with well-known patterns
(password hashing, JWT, session revocation) — not where Voltflow differentiates. Build, don't rebuild;
resist adding bespoke auth logic beyond what's already here.

## Ubiquitous language (D2)

- **User** (`AppUser`): a system operator/employee account (Admin, Office, Technician, Finance,
  Manager, Viewer role). **Not to be confused with** *Customer* (Customers context) — a business party
  being served, never a system operator, and never holds a Role.
- **Session** (`UserSession`): a persisted, revocable login record (SHA-256 token hash + expiry).
  **Not to be confused with** the "session context" field that will live on every M11 Command object —
  that's per-request metadata captured at command time, this is the standing login record it's
  captured from.
- **Approval**: an admin approving a *pending AppUser account*. **Not to be confused with** Quote
  acceptance (Quotes context) or ProgressBilling approval (Projects context) — same English word,
  three different state machines.

## Actors

- Anonymous visitor (register, login, request password reset)
- Registered User / AppUser (any approved role)
- Admin (approve users, assign roles)

## Commands (imperative verb)

- `RegisterUser` (name, email, password, optional dev-only OTP)
- `LoginUser` (email, password)
- `RevokeSession` (token)
- `RequestPasswordReset` (email)
- `CompletePasswordReset` (token, new password, optional email for dev-only OTP path)
- `ApproveUser` (userId)
- `AssignRole` (userId, roleName)

## Domain Events (past tense)

- `UserRegistered` (pending approval, unless dev-only OTP path taken)
- `UserLoggedIn`
- `SessionRevoked`
- `PasswordResetRequested`
- `PasswordResetCompleted`
- `UserApproved`
- `RoleAssigned`

## Policies (event → command reaction)

None identified. No event in this context currently triggers an automatic command — every transition
here is a direct user/admin action. (Contrast with WorkOrders, where `MaintenanceContractDue`
triggers `CreateWorkOrder` automatically.)

## Read Models

- `GetCurrentUser` ("me")
- `ListPendingApprovals`
- `ListUsersWithRoles`

## Depends on / Depended on by (D5)

- **Depends on:** nothing — this is the foundational context.
- **Depended on by:** every other context (every M11 Command's "actor identity and role" field
  resolves through here).

## Hotspots (D3)

1. **RESOLVED (2026-09-18):** `AuthService.RegisterAsync`/`CompletePasswordResetAsync` accepted a
   hardcoded `"000000"` master OTP/reset-token in *every* environment, unconditionally — the
   password-reset branch was a full account-takeover path given only a target email (CWE-798). Fixed:
   both branches now require `IHostEnvironment.IsDevelopment()`. See
   `docs/adr/0002-hosting-abstractions-for-environment-gating.md` and
   `docs/architecture/voltflow-agents-compliance-roadmap.md`.
2. **OPEN:** `RequestPasswordReset` generates and stores a hashed token, but no code path
   (`EmailSender`, notification service, outbox message) was found that actually delivers it to the
   user. Either this is intentionally out of scope for now (manual/ops-assisted reset), or it's a real
   functional gap — needs a decision, not an assumption.
