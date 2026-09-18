# Voltflow — AGENTS.md Compliance Roadmap

Date: 2026-09-18. Source of truth: `/AGENTS.md` (now present at repo root; was previously only in the
main checkout, not this worktree — copied over as part of this pass). Grounded against the review
pasted into this session, then cross-checked directly against the code (spot-checked Q1, M11, T1, V8,
O1/O2, D1/D2/D5, M6, O3, O6 — all confirmed accurate against actual files). Rule IDs below refer to
`AGENTS.md` sections D/M/V/H/G/T/Q/U/O/A.

This is a planning document, not a rule. It does not get gated; `AGENTS.md` does. Update or delete
sections here freely as phases complete — unlike `AGENTS.md`, this file has no "merge not append"
obligation.

## 0. Decisions A and B — resolved 2026-09-18

Both recommended options confirmed. Recorded here for the record; details kept below for context.

- **Decision A → keep Vite, update the Stack/Package-manager/Install/Run fields, close U5.** Done:
  `AGENTS.md`'s `Project` table now says Vite + React + npm; `docs/adr/0001-frontend-stack-vite-not-nextjs.md`
  written; `U5` added to the Closed rules table.
- **Decision B → Discovery-first.** Phase 1 runs before Phase 3, as laid out below.

### Decision A — Stack field (`Project` table) vs. actual frontend

`AGENTS.md`'s `Project` table declares `Next.js (TypeScript) frontend`; `U5` requires `next-themes`
`ThemeProvider`. The actual frontend (`frontend/vite.config.ts`, `frontend/src/theme.ts`, 40+
components) is Vite + React + MUI, and it's substantial, not a stub.

- **Recommended:** update the `Project` table's Stack row to say what's actually running (Vite +
  React + MUI), and close `U5` in the Closed-rules table with an ADR: *"Project uses Vite, not
  Next.js; `next-themes` doesn't apply. MUI's own `ThemeProvider` + a `localStorage`-backed mode
  toggle serves the same role."* `AGENTS.md` explicitly allows this per-project override ("a project
  with a hard constraint this stack doesn't fit... overrides these fields"). Small effort.
- **Alternative:** migrate the frontend from Vite to Next.js to match the document's default. This is
  a rewrite of the entire `frontend/` tree (routing, build, every component's theme access) and would
  dwarf every other item in this roadmap combined. Only worth it if there's a real reason (SSR,
  RSC, etc.) beyond "match the default."

### Decision B — Sequencing: Discovery-first (per the document) vs. risk-first (per the original list)

`AGENTS.md` Session start step 2 is explicit: a task touching a module with no `docs/domain-map/`
file starts with D1–D5 *before any implementation row*. All six existing modules (Customers, Quotes,
WorkOrders, Finance, Inventory, Projects) have zero domain-map files today, and almost every phase
below touches one of them.

- **Recommended:** Discovery first (Phase 1 below), because it's the document's own mandated order,
  and because M11's Command catalogue and the H9/H13 audit-status vocabulary are supposed to be
  *derived from* the Event Storming output (Commands, Domain Events, Policies), not invented ahead of
  it. Doing Phase 3 before Phase 1 risks naming things twice.
- **Alternative:** keep the original risk-ranked order (Q1 → H9 → M11 → V8 → …) and run Discovery as a
  parallel, lower-urgency track. Gets a passing `make gates` sooner; risk is re-touching M11/H-series
  naming once the domain map surfaces vocabulary that wasn't available yet.

Both decisions are independent — pick each on its own merits. The phase order below assumes the
recommended answer to Decision B; if the alternative is chosen instead, Phase 1 and Phase 3 swap
places and everything else is unaffected.

## Out-of-band fix: hardcoded auth bypass found during Phase 1 Discovery

While drafting the Identity context's domain map, `AuthService.cs` turned out to contain a hardcoded
`"000000"` master OTP/reset-token bypass in both `RegisterAsync` and `CompletePasswordResetAsync`,
unconditional in every environment — the password-reset branch alone was a full account-takeover path
(CWE-798), needing only a target email address. This predates and is unrelated to any AGENTS.md rule
ID, but was severe enough to fix immediately rather than only note as a Hotspot. Per Murat's decision:
kept for local/QA convenience, gated behind `IHostEnvironment.IsDevelopment()`.

- Fixed: [AuthService.cs](../../src/Voltflow.Application/Services/AuthService.cs) (both branches now
  require `_environment.IsDevelopment()`).
- New dependency recorded: `docs/adr/0002-hosting-abstractions-for-environment-gating.md` (R4).
- Regression tests added: `AuthServiceTests.RegisterAsync_MasterOtp_ShouldNotAutoApprove_OutsideDevelopment`,
  `..._ShouldAutoApprove_InDevelopment`, `CompletePasswordResetAsync_MasterOtp_ShouldFail_OutsideDevelopment`
  — all passing (`dotnet test --filter FullyQualifiedName~AuthServiceTests`: 6/6).
- This will also be recorded as a resolved Hotspot in `docs/domain-map/identity.md` (Phase 1, below),
  not just here.

## How graphify fits in

`graphify` (project-scoped MCP server, `.mcp.json`) parses this repo with Tree-sitter into a queryable
knowledge graph — callers/callees, blast radius, community/module clustering — and exposes it as MCP
tools (`query_graph`, `get_node`, `get_neighbors`, `get_community`, `god_nodes`, `graph_stats`,
`shortest_path`, `list_prs`, `get_pr_impact`, `triage_prs`). The main checkout already has a fresh
graph (`graphify-out/graph.json`, generated today); this session's connection to it was failing with
`Connection closed` because the `mcp` Python package (graphify's own MCP-transport dependency) wasn't
installed for the interpreter its `.mcp.json` entry points at (`C:\Python314\python.exe`). Root cause
confirmed by running the server directly: `ModuleNotFoundError: No module named 'mcp'`. Fixed by
running `python -m pip install "graphifyy[mcp]"` against that interpreter — installed cleanly
(`mcp-2.2.0`). This session can't hot-reload a project-scoped stdio server's connection itself
(confirmed: `reconnect_session_connector` only redials claude.ai "connector"-kind servers, not
project `.mcp.json` ones) — **reconnect it via `/mcp` in this session, or restart the session**, and
its tools should come up clean afterward.

Once connected, planned uses per phase (not required to start Phase 0/1, genuinely additive from
Phase 1 on):

| Phase | graphify tool | Why |
|-|-|-|
| 1 — Discovery | `god_nodes`, `get_community`, `graph_stats` | Cross-check each module's Core/Supporting/Generic call (D6) and its dependency list (D5) against actual call-graph clustering instead of eyeballing folders |
| 3 — Command/audit pipeline | `get_neighbors`, `shortest_path`, `query_graph` | Before touching `M9`'s shared pipeline, get every real caller of `StampCreatedBy`/`ExecutionGuardFilter` in one call instead of grepping service-by-service; verify no second, undiscovered write path exists (V2) |
| 4 — Boundary/data | `query_graph` ("who calls `ListAsync` without pagination") | Confirms the full V8 blast radius in one query instead of the manual grep list below, in case a caller was missed |
| Every phase, PR time | `list_prs`, `get_pr_impact`, `triage_prs` | Once each phase starts landing as PRs, use these for change-impact triage before merge |

## Already compliant — no work needed

| Rule | Evidence |
|-|-|
| M1–M4 | Modular monolith; `Domain → Application → Infrastructure → Presentation` layering; Domain has no framework/ORM types; interfaces in Application, impls in Infrastructure |
| H1 | RFC 9457 Problem Details with `traceId` + `operationId` extensions |
| H3 | `Result<T>` pattern; no swallowed exceptions |
| H4 | `ExecutionGuardFilter` — idempotency key + payload-hash mismatch + response replay |
| H7 | Business data + audit event written in the same `SaveChanges` |
| V5, V6 | DB-level uniqueness/integrity; `Version` token + `DbUpdateConcurrencyException` → 409 |
| G2–G4, G6 | Default-deny server-side authorization; PII redaction in `DbContext`; generic 500s; `UseMutationPolicy()` on every mutation endpoint |
| T4, T7 | `X-Operation-Id` on every response; `AuditEvents` table separate from business tables |
| U1, U2 | Material UI only; no `backdrop-filter` |
| O4 | Config from env vars, enforced via `${VOLT_JWT_KEY:?...}` in `compose.yml` |

## Closed-rules candidates (decide explicitly, don't silently skip)

`AGENTS.md` itself names these as typical scale-based closure candidates: `G9`, `Q4`, `T2`, `Q11`,
`Q12`, `D6`. Each still needs either implementation or an ADR-backed row in the Closed rules table —
silence isn't an option (R7). `Q8`, `Q9`, `Q10` are explicitly **not** closeable on scale grounds
alone per the document. Worth a real per-rule decision in Phase 6/7, not a blanket "close everything
SHOULD."

Decided so far: `Q4` (Pact half only - ADR 0004), `G9` and the OpenSSF Scorecard half of `G10` (Phase 6 -
ADR 0008). `D6` was implemented in Phase 1 and `T2` in Phase 5, so neither is a candidate any more. Still
open: `Q11`, `Q12`.

## Phase 0 — Tooling skeleton — done, with honest gaps flagged

Nothing below has a real gate to run against until this exists (RULE 0: a `make` target that doesn't
work yet is a failing gate, not a missing one).

| Item | State |
|-|-|
| `Makefile` | Created. `install`, `dev`, `typecheck`, `test`, `check-domain-map` are real and run actual tools. `arch`, `style`, `check-review` intentionally exit 1 with a message pointing at what's missing (no ArchUnitNET/dependency-cruiser/stylelint config yet; `check-review` also blocked on the A3/A7 branch-protection question in Phase 8) — per RULE 0, an honest failure, not a fake pass. `check-domain-map` calls `scripts/check-domain-map.sh`, verified working (correctly fails today, listing all 6 missing domain-map files). |
| `catalog-info.yaml` | Created — component, owner, dependencies (postgres/redis resources) filled in. **ASVS 5.0 target level (G1) left as `UNSET-pending-decision`** — this is a real risk/business decision (Level 1/2/3), not something to assume; Voltflow handles customer PII and billing data, which argues for at least Level 2, but needs an explicit answer. |
| `make` itself | **Not installed on this dev machine's shell** (`make: command not found` in Git Bash) — install via e.g. `choco install make`, or use WSL/MSYS2, before any `make` target can actually run here. Targets were verified by running their underlying commands directly instead (e.g. `bash scripts/check-domain-map.sh`). |

Running `make gates` right now will still fail (`arch`, `style`, `check-review` aren't wired) — that's
the accurate current compliance state, not a bug. `check-domain-map` now passes: all 8 contexts have a
domain-map file as of Phase 1 below.

## Phase 1 — Discovery (D1, D2, D5, D6) — done, Hotspots need your read (Lifecycle step 3)

All 8 files written; `make check-domain-map` passes. Per AGENTS.md's own Lifecycle step 3, this is the
"Present & flag" checkpoint — the map (and the ~13 open Hotspots inside the 8 files, one already
resolved) needs to be read before Phase 2/3 implementation work builds on the vocabulary it defines.
Nothing below is blocking in the D3 sense (no Hotspot here requires a behavior change to *existing*
shipped code, unlike the auth bypass above) — but several affect naming/sequencing choices Phase 3
(Command objects, Policies as M12 sub-operations) will make.

One file per bounded context, Event Storming vocabulary (Commands, Domain Events, Actors, Policies,
Read Models), ubiquitous-language "not to be confused with" entries, depends-on list (D5 — modular
monolith default, full Context Map vocabulary not needed since no context has graduated to a
microservice), Core/Supporting/Generic classification (D6).

Cross-checked against `src/Voltflow.Domain/*` folders + `Endpoints`/`Services` — the original review's
6-module list was incomplete: **Identity** and **Reminders** are real, implemented modules (their own
`Domain/` folder, services, and — for Identity — endpoints) that were missing from it, so they get
domain-map files too. **Suppliers/Purchasing** (`voltflow-domain-models.md` §2.7) is documented on
paper only — no `Domain/`, `Service`, or `Endpoints` file exists for it — so per D1's own trigger
("before a module's first operation is implemented") it does **not** get a domain-map file yet; it's
noted as a Hotspot instead. `OperationsEndpoints.cs` (outbox DLQ list/replay) and the
`MaintenanceProcessor`/`ReminderProcessor` background workers are cross-cutting/policy mechanics
(M9, D4), not bounded contexts of their own.

| Context | File | Classification (D6) |
|-|-|-|
| Identity & Access | `docs/domain-map/identity.md` | Generic |
| Customers | `docs/domain-map/customers.md` | Core |
| Quotes | `docs/domain-map/quotes.md` | Core |
| WorkOrders (incl. Maintenance Contracts) | `docs/domain-map/workorders.md` | Core |
| Inventory | `docs/domain-map/inventory.md` | Supporting |
| Finance (Billing + Payments) | `docs/domain-map/finance.md` | Core |
| Projects (Progress Billing) | `docs/domain-map/projects.md` | Core |
| Reminders | `docs/domain-map/reminders.md` | Supporting |

Retroactive discovery on already-built modules: still real work (D1 doesn't say "only for
not-yet-built modules"), but any Hotspot found (D3) that would require behavior change goes in the
map as an open question, not a blocking rewrite — flag it, don't silently redesign shipped code (the
auth bypass above was the one exception severe enough to fix immediately rather than only flag).

## Phase 2 — Contract-first API surface (M6, M7, Q4) — done

| Item | Result |
|-|-|
| `openapi.yaml` | Written by reverse-documenting all 48 operations across the 9 real `Endpoints` files (verified against source, not the original review's assumptions) — 42 schemas, security scheme, shared error responses. `make contract` (Spectral, `spectral:oas` ruleset) passes: **0 errors, 54 style warnings** (missing per-operation `description` alongside `summary`, and a missing `info.contact` — left as a known, non-blocking polish item, not worth ~48 repetitive edits for the marginal gain). M6's direction now flips: this file is the source going forward, `Endpoints/*.cs` is verified against it. |
| `asyncapi.yaml` | Written from `OutboxMessage.cs` (the real envelope) and the real `EventTypes` catalogue in `VoltflowTaxonomy.cs` — 22 messages, 1 channel (in-process Postgres-table outbox, no external broker today). Per-event `data` payload shapes are intentionally left untyped — tracing each to its producer is flagged as a Hotspot inside the file, not done in this pass. |
| API versioning (M7) | Resolved via `docs/adr/0003-api-versioning-policy.md`: today's surface is treated as implicit v1 (`openapi.yaml`'s `info.version: 1.0.0`); no repo-wide `/v1/` rename was done (rejected — real blast radius across 9 endpoint files + `frontend/src/api.ts` + tests, for zero behavior change). Policy recorded for when the first real breaking change happens. |
| Q4 | Spectral: real and gating (`make contract`, folded into `make gates`). Pact: closed via `docs/adr/0004-q4-contract-testing-scope.md` and a new `AGENTS.md` Closed-rules row — one repo, one release train, no separately-evolving consumer to verify against yet. |

## Phase 3 — Command object + audit pipeline (M11, H9–H13, M12, M13) — walking skeleton done

The largest structural phase — touches every mutation across every module. Per AGENTS.md's own
Walking Skeleton principle (Lifecycle step 4), this pass builds the shared pipeline once and proves
it end-to-end on one representative service, rather than hand-editing all ~35 mutation endpoints in
one sweep — that rollout is explicitly the next step, not attempted here.

### What was built (shared infrastructure, real today)

- **`Command`** (`Voltflow.Application.Commands`) — immutable record: `CommandId`, `ParentCommandId`,
  actor id/roles, trigger source, `CommandType`, verbatim `PayloadJson`, timestamp (M11's required
  fields).
- **`CommandRecord`** (Infrastructure entity) + **`ICommandJournal`** (`BeginAsync` writes `Pending` in
  its own immediate transaction; `MarkResolved` updates the tracked entity without saving, so a
  caller's own `SaveChanges` resolves it atomically; `ResolveNowAsync` is the standalone fallback;
  `IsPendingAsync` is H9's "detectable" query). EF migration `AddCommandRecord` applied cleanly to a
  full from-scratch database (see note below on how this was verified).
- **`CommandAuditFilter`** (Api.Security) — wired into `UseMutationPolicy()` (chain:
  RateLimit → ExecutionGuard → **CommandAudit** → handler), so it only fires for genuine new
  executions, not idempotency replays. `CommandId` reuses the existing `IOperationContext.OperationId`
  (T4's "operation_id equals the triggering command_id" — no new parallel identifier invented).
  **This one change means M11 capture + H9's pending-write now happen for every mutation endpoint in
  the API**, not just the pilot service — verified by construction (it's in the shared
  `UseExecutionPolicy` extension every `.UseMutationPolicy()` call already used).

### Rollout to all 9 services — done

Starting from the `WorkOrderService.ExecuteActionAsync` pilot (13 operations: Assign, MarkAsEnRoute,
ReportNoShow, CompleteSafetyChecklist, Start, CheckIn, CheckOut, PutOnHold, Resume, Cancel, Complete,
ApproveForBilling, Invoice, AddMaterial), the same pattern was rolled into every remaining service.
Full backend suite after the rollout: **122/122 passing**, solution builds with 0 errors.

Two variants were used, depending on how each method actually persists (verified per-repository, not
assumed):

- **Piggyback (`MarkResolved`, no separate save)** — for every mutation that goes through the generic
  `Repository<T>.AddAsync`/`UpdateAsync` (confirmed by checking each repository for overrides): the
  Command's resolution rides in the **same transaction** as the business mutation — literal H9
  compliance. Covers: `WorkOrderService.CreateAsync` + its 13 `ExecuteActionAsync` operations,
  `CustomerService` (Create, ConvertToActive), `QuoteService` (Create + its 5 `ExecuteActionAsync`
  operations), `BillingService.CreateAsync`, `ProjectService` (Create, AddPhase),
  `InventoryService.ReserveAsync`, `AuthService` (Register, ApproveUser, CompletePasswordReset).
- **Explicit follow-up (`ResolveNowAsync`, its own immediate transaction)** — for the four mutations
  whose persistence happens inside a **custom repository method** that already calls its own
  `SaveChanges` internally, where piggybacking isn't possible without changing that method's own
  transaction boundary: `QuoteService.ConvertAcceptedToWorkOrderAsync`,
  `InventoryService.AdjustAsync`, `PaymentService.CreateAsync` and `.AllocateToInvoiceAsync`,
  `AuthService.AssignRoleAsync` and `.RevokeSessionAsync` (both use repositories that don't inherit the
  generic `Repository<T>` base). Not literally "H7's transaction," but still real, immediate,
  precisely-coded resolution — strictly better than waiting for the filter's generic fallback.
- Every mutation endpoint now has H9 coverage one way or the other; `LoginAsync` and
  `RequestPasswordResetAsync` are intentionally untouched — their endpoints use
  `.UseMutationRateLimit()` only, not `.UseMutationPolicy()`, so no Command is ever opened for them
  (consistent with the existing pipeline wiring, not a gap introduced here).

### Migration verification incident (disclosed, not hidden)

Verifying the `AddCommandRecord` migration applied cleanly against real Postgres, an environment-variable
connection-string override (`ConnectionStrings__DefaultConnection`, intended to point `dotnet ef
database update` at an isolated throwaway container on port 5555) **did not take effect** - `dotnet ef`
fell back to `Voltflow.Infrastructure/DependencyInjection.cs`'s hardcoded default
(`Host=localhost;Port=5433;...`), which is the long-running (44h-old) local dev Postgres container,
not the isolated one. The migration **did** apply there — confirmed purely additive
(`CreateTable`/`CreateIndex` only, no `Drop`/`Alter` on any existing table; the migration file was
read in full to verify this before reporting it). No data was touched or lost, and the new,
currently-unused `CommandRecords` table is in fact the correct eventual schema for that environment
once this feature ships there — but it was an **unconfirmed action on a shared, persistent resource**,
which shouldn't happen without asking first. Flagged here rather than silently left out. The throwaway
container created for the intended isolated check was removed. Next time: pass `--connection`
explicitly to `dotnet ef` rather than relying on an environment variable that this project's
design-time host-building doesn't reliably pick up.

### Deferred, not attempted this pass

| Rule | Why deferred |
|-|-|
| H10 (compensation) | Needs real per-operation domain design (what does "undo" mean for each operation?), not a generic pipeline hook. WorkOrder's Hold→auto-checkout Hotspot (Phase 1) is the natural first candidate. |
| H11 (formal Cancel command) | Depends on H10 existing first (cancellation is defined in terms of triggering compensation for completed steps). |
| H12 (timeout) | Needs a duration config surface + a scheduled sweep; natural to build alongside M12 (below), since a timeout's cancellation flow is itself a sub-operation. |
| H13 (partial failure) | Multi-step only (per AGENTS.md's Done table) — no multi-step operation has been touched yet. |
| M12 (sub-operations) | "N/A yet — build alongside the first operation that needs it," per this roadmap's own Phase 3 table from the first pass. Quotes' Hotspot 1 (should conversion-to-WorkOrder become a Policy) is the natural candidate. |
| M13 (admin/approval commands) | Same — no admin-override/approval flow has been formalized yet. |

Original per-rule table (still accurate as the target state, not yet fully built):

| Rule | Current state (verified) | Target |
|-|-|-|
| M11 | No Command objects anywhere in `src/` (checked: no `record/class *Command`) — services take plain DTOs (`CreateWorkOrderRequest`, etc.) | Immutable Command record per operation: `command_id`, actor identity/role, trigger source, exact payload, session context, timestamp; recorded to audit as-is |
| H9 | Audit write happens in the *same* `SaveChanges` as the mutation (`VoltflowDbContext.StampCreatedBy`) | Separate prior transaction writes `status: pending` before the mutation transaction; completion updates to `completed`/`failed`; an unresolved `pending` row is detectable |
| H10 | `WorkOrder.Cancel` exists but is a domain state change, not a compensating operation | First-class compensation: own Command object, `operation_id`, audit record, tested independently (Saga pattern) |
| H11 | Same `Cancel` method — no formal Cancel *command*, no compensation trigger for completed steps | Explicit Cancel command callable on `pending`/`in_progress` operations; triggers H10 compensation for completed steps; actor-attributed audit entry |
| H12 | No execution timeout config anywhere | Per-operation max duration in config; timeout transitions to `timed_out` and invokes H11's cancel flow automatically |
| H13 | No `partially_completed` status | Multi-step operations record each step's status independently; partial failure is its own terminal status, not collapsed into total failure |
| M12 | N/A yet (no forks exist) — build alongside the first operation that needs it | Sub-operations get their own Command, `operation_id`, `trace_id`, audit trail; parent status reflects all children |
| M13 | N/A yet | Explicit command type per admin/approval intervention, same pipeline requirements as user-initiated ops |

Primary touch points (from the `ListAsync`/service grep already run): `WorkOrderService`,
`QuoteService`, `ProjectService`, `CustomerService`, `BillingService` and their endpoints — same set
Phase 4 touches, so sequence these together per-module rather than doing two separate passes.

## Phase 4 — Data boundary hygiene (V4, V7, V8) — done

| Rule | What was built |
|-|-|
| **V8** | `PagedResult<T>` + `PaginationDefaults` (`Voltflow.Application.Common`) — default limit 50, hard ceiling 100 (clamped, never rejected). All 7 previously-unbounded list operations now paginate: Customers, Quotes (incl. the `customerId` filter), WorkOrders (incl. the technician-scoped view), Projects, Billing entries, Payments, Sales invoices. Each repository got a `ListPagedAsync`/`GetByProjectPagedAsync`/etc. using `.Skip().Take()` + a separate `.CountAsync()`; each endpoint sets an RFC 8288 `Link` header (`PaginationLinkHeaderExtensions.ApplyPaginationHeaders`, using `QueryHelpers` for correct URL construction) plus `X-Total-Count`, while the **response body stays a bare array** — no breaking change to the existing frontend or tests. `openapi.yaml` updated to match (all 7 operations now declare `limit`/`offset` query params and the `Link`/`X-Total-Count` response headers) — `make contract` still 0 errors, 54 (pre-existing, unrelated) warnings. Verified: `PaginationDefaultsTests` (ceiling/floor clamping) + `CustomerServicePaginationTests` (repository called with normalized values, oversized `limit` clamped before the query runs). |
| **V7** | `Entity.SetCreatedAt(DateTime)` and `Touch(DateTime)` added; `VoltflowDbContext` now takes an injected `TimeProvider?` (defaults to `TimeProvider.System`) and uses it in `StampCreatedBy` for both `CreatedAt` (on Add) and `UpdatedAt` (on Modify) - the one central place every entity's audit timestamps already flowed through, so no per-entity constructor changes were needed. `Infrastructure.AddInfrastructure` registers `TimeProvider.System` via `TryAddSingleton`. `ApiTestFixture` already had a `FakeTimeProvider` registered and waiting for exactly this (built for `MaintenanceProcessor`, previously unused by `VoltflowDbContext`) - confirms this was the intended design, just not wired through. Verified: `TimeProviderStampingTests` - `CreatedAt`/`UpdatedAt` proven to equal the fake clock's value, not real wall-clock time. Scattered business-logic `DateTime.UtcNow` calls (session/token expiry, outbox retry scheduling) are **not** covered by this pass - flagged as a follow-up, not attempted here (see below). |
| **V4** | `scripts/check-migrations.sh` (`make check-migrations`, folded into `make gates`): finds migrations new on this branch (committed-since-`main` or still untracked/uncommitted - a local run catches it before commit too), scans only each one's `Up()` method (its `Down()` legitimately drops/renames to reverse itself, which is correct and not a violation), and fails if `DropColumn`/`RenameColumn`/`DropTable`/`RenameTable` appears there. Verified both directions: passes on the real `AddCommandRecord` migration (pure `CreateTable`/`CreateIndex`), and a scratch migration with a real `DropColumn` was confirmed to fail the gate before being deleted. The 15+ pre-existing migrations are correctly not retroactively checked - this gates the *next* migration, not history. |

Full backend suite after Phase 4: **134/134 passing**, solution builds with 0 errors.

### Deferred, not attempted this pass

- V7's scattered `DateTime.UtcNow` calls outside the audit pipeline (e.g. `UserSession`/`PasswordResetToken` expiry math in `AuthService`, `OutboxMessage`'s retry backoff) - real, but a much longer tail than the central fix; worth its own pass if session/token-expiry testability becomes a concrete need.

## Phase 5 — Observability (T1, T2, T6) — done

Taken right after Phase 4, ahead of H10/M12: those are design-heavy and you want to customize them, so
they wait for your input (see the status footer) rather than being designed here.

| Rule | What was built |
|-|-|
| **T1** | OpenTelemetry .NET SDK registered once (`Infrastructure/Observability/TelemetryServiceCollectionExtensions.AddVoltflowTelemetry`) and used by both the API and the Worker; packages (all 1.18.0, stable) recorded in `docs/adr/0005-opentelemetry-dotnet-sdk.md` (R4). OTLP export is switched on only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (O4) - unset means spans are still recorded in-process but nothing tries to connect. `OperationTraceMiddleware` **no longer creates its own server span** (it had a raw-path span name and the deprecated `http.method`/`http.status_code`); the ASP.NET Core instrumentation makes the one server span with the standard name/attributes and the middleware only adds `voltflow.*` attributes to it. The two `System.Diagnostics.Metrics` counters in `ApiMetrics` (underscore names, raw-path label, never exported) were removed - standard ASP.NET Core metrics cover it. Gate: `scripts/check-telemetry.sh` (`make check-telemetry`, in `make gates`) - deprecated attribute names, tag namespaces, dotted metric names, `Voltflow.*` source names, and "no second tracing stack"; **proven to fail** on four planted violations (which also caught a gap in its own `ActivitySource` rule, fixed) and to pass on the real code. |
| **T2** | Two service boundaries, both tested. *Inbound HTTP*: a request carrying a W3C `traceparent` is served inside the caller's trace, with exactly one server span (`InboundTraceContextTests`). *API -> Worker (the transactional outbox)*: `OutboxMessage` now stores the producing span's `traceparent`/`tracestate` (two nullable columns, migration `AddOutboxTraceContext` - `Up()` is `AddColumn` only, so it passes the V4 gate; it was **generated only, not applied to any database**), and `OutboxProcessor` starts a `process {EventType}` consumer span parented on it, with `messaging.*` attributes and `error.type` on failure (`OutboxTraceContextTests`). |
| **T6** | `/health` (liveness - no dependency check), `/ready` (readiness - database reachable), `/startup` (new - initialization finished **and** no pending migrations; `503` with `starting` / `migrations_pending` otherwise). The three probes moved into `Api/Health/HealthEndpoints.cs` unchanged in behavior; the startup decision is pure logic (`StartupProbe`) so all three branches are unit-tested, and an integration test asserts the three endpoints answer three different questions. Probes and `/metrics` are excluded from traces. Runbook and the Turkish usage guide document `/startup`. |

Full backend suite after Phase 5: **148/148 passing**, 0 build errors. `make check-domain-map`,
`check-migrations`, `check-telemetry` and `contract` all pass for real.

### Corrections to earlier phases, found while doing this one

- **`openapi.yaml` (Phase 2) was not complete:** the root-level `/health`, `/ready`, `/metrics` routes are
  mapped in `Program.cs`, not in an `Endpoints` file, and were missed. They are now in the contract (with
  the new `/startup`), as A4 requires - a route not in the schema cannot be added.
- **`asyncapi.yaml` (Phase 2) overstated the event catalogue:** only **three** event types are ever
  written to the outbox and accepted by `AuditOutboxPublisher` - `AuditEventRecorded`, `WorkOrderCompleted`
  and `TechnicianEnRoute` (the last is queued on *check-in*). The 21 `EVT_*` names in
  `VoltflowTaxonomy.EventTypes` are declared but unpublished (and would dead-letter, since the publisher
  rejects unknown types). The file now marks each message `x-published` and documents the real ones.

- **The Worker never had a clock registered (found by a smoke test of the Worker host, fixed):**
  `MaintenanceWorker` has always resolved `TimeProvider`, but the Worker's own composition root never
  registered one, so the recurring-maintenance policy (contract due -> auto work order) failed on every
  cycle in the deployed Worker. Pre-existing (`git show HEAD` confirms), masked in tests because
  `ApiTestFixture` registers a `FakeTimeProvider`. Fixed as the missing half of Phase 4's V7 - I had
  registered the clock only in the API's root. Verified by starting the Worker host against an
  unreachable database (nothing real touched): the DI error is gone, only the intentional
  connection failure remains. Worth knowing: until this fix ships, maintenance contracts in any deployed
  Worker have not been generating work orders.

### Found, reported, not fixed (out of this phase's scope)

- **T5:** the audit trail must contain `trace_id`, but neither `AuditEvent` nor the `CommandRecord` from
  Phase 3 stores it (only `operation_id`). Now that a real trace id exists it is a small additive column each.
- The legacy `/metrics` endpoint is still a hand-rolled Prometheus text exposition of three counters. The
  standard replacement (`OpenTelemetry.Exporter.Prometheus.AspNetCore`) is **beta-only on NuGet**
  (1.18.0-beta.1, no stable release - checked), so swapping it is a design decision for you, not a default.
- Frontend tracing (OpenTelemetry JS) is not done - the trace starts at the API server span. Needs a
  decision on CORS for `traceparent` and collector exposure.
- The `/ready` probe checks PostgreSQL only although the API also depends on Redis (rate limiting, session
  cache); the runbook documents it as PostgreSQL-only, so it was left as documented.

## Phase 6 — Security & supply chain (G1, G7, G8, G9, G10) — done, with one critical finding and one gate that is red on purpose

Decisions taken with you at the start of this phase: ASVS target **Level 2**; the seeded default admin
credential is **reported, not changed**; G9 is **closed by ADR, not implemented**. The tools and their pins
are recorded in `docs/adr/0007-supply-chain-tooling.md` (R4).

| Rule | What was built, and what it proves |
|-|-|
| **G7 secrets** | `make check-secrets` -> `scripts/check-secrets.sh`: gitleaks 8.30.1 over the **whole git history and the working tree**, findings redacted. The binary is bootstrapped into the git-ignored `.tools/` by `scripts/install-gitleaks.sh` against SHA-256 values **committed in the script** (proven: a wrong hash is refused and the download deleted). `.gitleaks.toml` = default rules + one custom rule for the seeded admin password + narrow, reason-annotated allowlists (generated directories; two documented placeholders). Real repo: history (12 commits) clean, working tree clean. Proven to fail on a planted GitHub token in the working tree, and on one that was committed and then deleted - a scan of the current tree alone misses that, which is why history is scanned too. The gate also caught **me** once - I wrote the seeded password into the ASVS evidence text, the custom rule flagged it, I removed it. |
| **G7 vulnerabilities** | `make audit` -> `scripts/check-dependencies.sh`: `dotnet list package --vulnerable --include-transitive` (its JSON is parsed, since the command exits 0 even with findings) plus `npm audit --audit-level=low` for `frontend/` and the repo root; it fails **closed** when a feed is unreachable. Real repo: no known vulnerabilities in NuGet or npm. Proven to fail on a scratch project pinned to a vulnerable Newtonsoft.Json (GHSA-5crp-9r3c-p9vr) and on fake `dotnet` outputs (vulnerable, and feed error). |
| **G8** | `make sbom` -> CycloneDX SBOMs into `sbom/` (git-ignored; CI uploads them as an artifact): backend by the CycloneDX .NET tool 6.2.0 (81 components, spec 1.6), frontend by `npm sbom` (67 components, spec 1.5). `scripts/validate-sbom.mjs` rejects an empty, truncated or invalid BOM (proven on four broken variants). |
| **G10, licenses** | `make license-policy` -> `scripts/check-licenses.mjs` + `license-policy.json`: full SPDX expression evaluation (`AND` / `OR` / `WITH` / parentheses); an exception must carry a reason. All 148 real components are permissive (Apache-2.0, BSD-3-Clause, ISC, MIT, PostgreSQL). Proven to flag exactly the six planted violations. **The allow-list is a conservative default I proposed; you have not reviewed it license by license.** |
| **G1** | Level 2 declared in `catalog-info.yaml`. `docs/security/asvs-5.0.0-audit.json` holds all **345** requirements of the official ASVS 5.0.0 release (downloaded from `OWASP/ASVS`, SHA-256 in the header, CC BY-SA 4.0 attributed - no requirement id or text comes from memory). `make check-asvs` fails while any requirement in scope for the declared level is `not-assessed`, while an assessed one has no evidence, or when a row was added or deleted; proven with 19 scratch audit variants. **It is red today, on purpose: 253 requirements are in scope at Level 2; 36 are `not-applicable` (all of V10 OAuth/OIDC and V17 WebRTC - reproducible greps are recorded as the evidence); 1 is `not-met` (V6.3.2, below); 216 are still `not-assessed`.** Assessing them is a per-requirement code review that I deliberately did not fake. |
| **G9, G10 (Scorecard)** | Closed by `docs/adr/0008-close-g9-slsa-provenance-and-scorecard.md` and two rows in the `AGENTS.md` Closed-rules table. The SPDX half of G10 is implemented and is **not** closed. |
| **CI** | New `.github/workflows/security.yml` (a new file - `ci.yml` and `deploy.yml` are untouched): on push, pull request and weekly; `permissions: contents: read`; every action pinned to a commit SHA. Job `supply-chain` runs `make check-secrets audit license-policy` and uploads the SBOMs; job `asvs-audit` runs `make check-asvs` and is red until the audit is complete - a separate job so it never hides the other result, and deliberately not `continue-on-error`. |

**Not run on GitHub.** The workflow was validated locally: it parses, every pinned SHA was checked against
GitHub to be the release its comment names, every `with:` key is an input of that action, every `make` target
exists, and six planted defects were each caught. It has not executed on a GitHub runner. The Linux and macOS
branches of `install-gitleaks.sh` were not run either - only Windows with Git Bash was. The first push will
tell.

### CRITICAL finding - fixed in code (ADR 0009); live-server remediation below

`SeedData` created an approved administrator `admin@voltflow.com` with a built-in default password whenever
`Seed:AdminEmail` / `Seed:AdminPassword` were not configured, and `docker-compose.deploy.yml` turns seeding on
(`Database__SeedOnStartup: "true"`) without passing either value. The repository is public and the literal is in
every commit since the first, in 78 tracked files (`SeedData.cs`, `tests/requests.http`,
`tests/ui-e2e/test-helpers/auth.ts`, 75 API E2E scripts). An environment created from the deploy compose on an empty
database therefore had an administrator whose credentials are public; ASVS V6.3.2 recorded it as `not-met`.
[ADR 0006](../adr/0006-accepted-risk-seeded-default-admin-credential.md) accepted the risk for the time being; the
owner asked for it to be removed the same day ([ADR 0009](../adr/0009-no-default-administrator-outside-development.md)).

- **Code (done):** outside Development no administrator is seeded unless `Seed:AdminEmail` and `Seed:AdminPassword`
  are both configured, and the seed never modifies an existing user (a disabled account stays disabled across
  restarts). `docker-compose.deploy.yml` and `deploy.yml` forward the optional secrets `SEED_ADMIN_EMAIL` /
  `SEED_ADMIN_PASSWORD`. Six new tests (`SeedDataTests`), proven to fail when the gate is disabled; the full suite is
  154/154. `deploy.yml` only runs on `main`/`staging`, so its change was checked statically (YAML, `envs`/`env`
  consistency, heredoc) rather than executed.
- **Found while checking the live server (read-only):** its database had been seeded on the first deploy, so the
  default administrator existed there; it also held ten leftover E2E test accounts (six approved) and E2E test data,
  because the E2E suite had been run against production. No sign of misuse was found in the logs the server keeps
  (every SSH login was key-based; the default administrator had 22 sessions, which coincide with the E2E runs), but
  the site sits behind a CDN that hides client addresses, so that cannot be proven.
- **Live remediation: pending.** One data change, after a backup of the identity tables: the owner's own account
  becomes an approved administrator; the default account and the approved E2E accounts are set to not approved and
  their sessions revoked. Nothing is deleted.
- **Also added:** `scripts/harden-vps.sh` (SSH by key only, fail2ban, ufw allowing only 22/80/443), to be run by the
  owner on the server.


### Found, reported, not fixed (out of this phase's scope)

- The password-reset flow cannot complete outside Development (above) - a functional gap for real users too.
- All 75 API E2E scripts, `tests/requests.http` and `tests/ui-e2e/test-helpers/auth.ts` hard-code the default
  admin credential - the prerequisite work for removing the default from the code.
- The actions in `ci.yml` and `deploy.yml` are referenced by tag, not commit SHA (only `security.yml` pins);
  relevant to Scorecard and to Phase 8.
- Pre-existing, seen in the test build (identical package versions in `HEAD`): `Npgsql.EntityFrameworkCore.PostgreSQL`
  10.0.3 resolves `Microsoft.EntityFrameworkCore.Relational` 10.0.4 while EF Core itself is 10.0.12, so MSBuild
  warns (MSB3277) and picks 10.0.4. Harmless today; worth aligning the Npgsql provider version.
- **Next for G1:** assess the 216 open ASVS rows. Largest chapters first: V6 (34), V1 (27), V3 (19), V7 (18),
  V15 (13)... Each row needs a status and evidence someone can re-check; graphify helps find the code.

## Phase 7 — Test depth (Q1, Q2, Q5, Q6, Q8–Q13)

| Rule | Current state (verified) | Target |
|-|-|-|
| Q1 | `ApiTestFixture.cs:28` sets `Database:Provider = "InMemory"`; `Voltflow.Tests.csproj` has no Testcontainers/Npgsql reference at all | Swap for `Testcontainers.PostgreSql`; this is the highest-leverage single fix in the whole roadmap — everything else's tests currently run against a provider that doesn't share Postgres's transaction/constraint/concurrency semantics |
| Q5 | No `data-testid` in any frontend component | Add to components as Playwright coverage grows |
| Q8 | No explicit small/medium/large test split | Classify existing tests, gate the ratio |
| Q9 | No fault-injection tests for H8/H12 paths | Add once H12 (Phase 3) exists |
| Q10 | No concurrency tests for V6/H4 paths | Dual-request tests against the same resource |
| Q13 | Cancel path partially covered; timeout/compensation/partial-failure/fork paths untested | Depends on Phase 3 landing first |
| Q11, Q12 | SHOULD — no FsCheck/fast-check, no Stryker.NET/Stryker JS | Closed-rules candidates if the team decides property/mutation testing isn't warranted yet — still needs an ADR |

## Phase 8 — Process scaffolding (O1, O2, O3, A7)

| Rule | Current state (verified) | Target |
|-|-|-|
| O1, O2 | Root `package.json` has only `@playwright/test` as a devDependency — no husky, no commitlint | Wire husky + commitlint from `frontend/package.json` (per `AGENTS.md`'s own note that this covers the whole repo's hooks); branch naming `<type>/<ISSUE>-<name>` |
| O3 | No `docs/adr/` | MADR template; backfill ADRs for decisions already made (modular monolith, JWT auth, and Decision A above once resolved) |
| A7 | No `docs/reviews/`; unclear whether GitHub branch protection (A3) is actually turned on | **Needs a direct answer, not an assumption:** if branch protection is on, A3 applies and A7 gets a Closed-rules row; if not, A7's self-review-checklist mechanism needs building |

## Suggested order

Decision A + B (both fast) → Phase 0 → Phase 1 (or swap with Phase 3 per Decision B) → Phase 2 →
Phase 3 → Phase 4 (same modules as Phase 3, sequence together) → Phase 5 → Phase 6 → Phase 7 →
Phase 8. Q1 (Testcontainers) can realistically start in parallel with Phase 0 since it doesn't depend
on anything else here — it's independent of Discovery, Command objects, or any other phase, and it's
what every later phase's own tests will run against.

**Status as of 2026-09-18: Decisions A+B, Phase 0, Phase 1, Phase 2, Phase 3's M11+H9 rollout, Phase 4
(V8/V7/V4), Phase 5 (T1/T2/T6) and Phase 6 (G7/G8/G10-licenses built, G1 declared with its audit record and
gate, G9 and G10-Scorecard closed by ADR) are all done.** `make gates` still stops at `arch` (first
not-yet-wired target) — `typecheck`, `contract`, `check-domain-map`, `check-migrations`,
`check-telemetry`, `check-secrets`, `audit` and `license-policy` all pass for real; `style`, `arch`, and
`check-review` still correctly fail pending their own build-out, and `check-asvs` is **red on purpose**
(216 of 253 in-scope ASVS requirements not yet assessed). Full backend suite: **148/148 passing**, solution
builds with 0 errors.

**Needs your attention first:** the seeded default admin credential (Phase 6, ADR 0006) - a critical finding
that is public, and the app has no working password change outside Development. Please reconfirm the
"report, don't change" decision.

**On hold by decision, not by omission:** H10, H11, H12, H13, M12 and M13 (Phase 3's design-heavy
remainder). You want to customize those designs yourself, so they were deliberately not started - the
natural first candidates when you pick them up are WorkOrder's Hold→auto-checkout Hotspot (H10) and the
Quotes conversion-to-WorkOrder Hotspot (M12). Next in the mechanical queue: Phase 7 (test depth, incl. Q1
Testcontainers) and Phase 8 (process: commitlint/husky, MADR backfill, A7); in parallel, the ASVS
assessment pass (Phase 6's remaining work) can proceed chapter by chapter.
