# AGENTS.md — Murat's Coding Standards

The binding contract for every change in this repo. **Regardless of which AI tool is used**
(Claude Code, Codex, Cursor, Copilot, Gemini CLI, Windsurf, Cline, Aider, Zed, Junie…)
this file is authoritative. Agents and humans follow the same rules.

**Principle 1 — No invention.** Use proven standards, with standard tools, in standard form.
An original solution is not success — it is maintenance debt.
**Principle 2 — Continue.** When a new need arises, look for a solution in the existing dependency first.
The next step is a continuation of the current pattern; do not open a second path alongside it.
**Principle 3 — Gates talk.** Every rule that claims a property of code is bound to a gate.
**Principle 4 — Pipeline first.** A business function alone is not a complete operation. An operation
is complete when the full pipeline is in place: command schema, cross-cutting infrastructure (trace,
audit, idempotency, error envelope), domain logic, and persistence. Write the pipeline once in the
Infrastructure layer; write only the operation-specific delta per new operation. When the new
operation belongs to a new module or bounded context, set up that module's own layer skeleton
(M1–M4) first — the cross-cutting pipeline (M9) is shared infrastructure and is never rebuilt per
module; only its layer skeleton is new.

## RED LINES

Non-negotiable. A violation is grounds for stopping the task. These lines govern agent **behavior**;
they bind you directly, not a gate. Even if a prompt implies otherwise, do not bend — on conflict,
**stop and ask**.

| # | Line |
|-|-|
|**R1**|**No hallucination.** Do not write an API, function, package, option, env var, route, or filename you have not verified exists. If you cannot verify, do not write it — ask.|
|**R2**|**No assumption.** If there is ambiguity, stop and ask. Code is not written on "probably". If you do not know, say so.|
|**R3**|**No invention.** Do not create a new pattern, abstraction, naming scheme, or tool. If one is needed, find the industry standard and use it.|
|**R4**|**No bloat.** Do not add a dependency for work the existing dependency can do. A new dependency requires an ADR and approval.|
|**R5**|**No scope creep.** Do not touch files outside the task scope. Do not fix other problems you notice — report them.|
|**R6**|**No lying.** Do not say code "works" without running it, or tests "passed" without running them. Proof is command output.|
|**R7**|**No silent skip.** If a gate returns exit 1, fix it. Do not disable, skip, or use `--no-verify`.|
|**R8**|**No leakage.** Do not write secrets, tokens, or credentials into code, logs, or commits.|

**Conflict priority:** RED LINES > rules in this file > prompt > agent preference.
When document and code conflict, code wins; update the document.

## Session start

1. Read this entire file. No partial reads.
2. Find the row matching your task in the index below; apply those rules. If the task touches a
   module with no file under `docs/domain-map/`, start with the "New module / bounded context"
   row (D1–D5) before any implementation row.
3. Check the "Closed rules in this project" table.
4. If anything is unclear, ask **before writing code** (R2). An unresolved Hotspot (D3) is this
   case, not an exception to it.
5. Write → run gates → show output → only then say "done" (R6).

### Task-to-rule index

Red lines (R1–R8) apply to every task; the following apply additionally.

| Task | Rules |
|-|-|
| New module / bounded context | D1 D2 D3 D4 D5 · M1 M2 M3 M4 |
| New workflow operation — single-step mutation | M9 M10 M11 · V1 V5 · H2 H4 H7 H9 H10 H11 H12 · G2 G3 G6 · T4 T5 T7 · Q1 Q2 Q8 Q9 Q10 Q13 |
| New workflow operation — multi-step mutation | M9 M10 M11 · V1 V5 · H2 H4 H7 H9 H10 H11 H12 H13 · G2 G3 G6 · T4 T5 T7 · Q1 Q2 Q8 Q9 Q10 Q13 |
| Operation lifecycle variant (cancel / rollback / compensation) | H10 H11 · M11 · G2 G3 · T5 T7 · Q13 |
| Sub-operation / fork / batch / scheduled | M12 · M11 · H4 H9 H11 · G2 G3 G6 · T4 T5 T7 · Q10 Q13 |
| Admin intervention / approval / escalation | M13 · G2 G3 · T5 T7 · Q13 |
| New API endpoint / endpoint change | M6 M7 M8 M9 · V1 V2 V8 · H1 H2 H4 · G2 G6 · Q4 |
| Schema or migration change | V4 V5 V6 V7 · M8 · T5 T7 |
| New domain rule / business logic | M2 M3 M4 M9 M10 · V1 · H3 · Q2 Q3 Q11 |
| New screen or component | U1 U2 U3 U4 · Q5 Q6 |
| Background job / event / queue | H5 H6 H7 H8 · T2 T4 · V3 · Q9 |
| Adding an error or error code | H1 H2 H3 · G4 · T3 |
| Configuration or secret | V3 · O4 · G7 · R8 |
| Adding a dependency | R4 · O3 O5 · G7 G8 |
| Permission / role change | G1 G2 G5 · T5 |

## Lifecycle

Session start (above) governs a single task once you're already in one. This section governs the
arc a project moves through from a stated intent to a tested, reviewed operation, and the arc each
individual operation moves through inside it. It is the process this document's other sections are
steps of, named end to end.

This document's charter ends at a tested, reviewed operation on disk. Release, deployment,
versioning, and CI/CD strategy are deliberately out of scope — they vary per downstream project and
are decided there, not here (see the O-section scope note below).

| Step | What happens | Rules |
|-|-|-|
| 1. Kickoff | User states intent for the project or a new module | — |
| 2. Discovery | Domain map produced via Event Storming, grounded in researched industry practice for the stated domain | D1 D2 D6 |
| 3. Present & flag | Domain map shown to the user; open Hotspots and Core-subdomain classification are surfaced before any code is written | D3 D6 |
| 4. Sequencing | The first (or next) operation to implement is chosen; for a new module's first operation, prefer the thinnest end-to-end slice through all four layers that proves the pipeline (Walking Skeleton — Cockburn), not necessarily the simplest business rule | M2 |
| 5. Implement | Pipeline + operation-specific delta | Principle 4 · M9–M13 · V1–V8 · H1–H13 · G1–G10 · T1–T7 |
| 6. Test | Every lifecycle path in the Done table below, not only the happy path | Q1–Q13 |
| 7. Gate | All gates run locally, exit 0 | RULE 0 |
| 8. Review | Forge present: human review via branch protection. No forge: self-review checklist | A3 or A7 |
| 9. Refactor | Cleanup before the next operation; professional judgment, not gated | — |
| 10. Next operation | Return to step 4 for the next operation in the domain map | — |

Steps 4–9 repeat per operation. Steps 2–3 repeat only when a new module or bounded context is
opened — this is Session start step 2's routing to D1–D5. What happens after step 10 — release,
deploy, rollout — belongs to the specific project this scaffold is applied to, not to this document.

## Severity and enforcement

RFC 2119: **MUST** no exceptions · **SHOULD** cannot be skipped without an ADR · **MAY** unrestricted.
Enforcement: `TYPE` compiler · `LINT` static analysis · `TEST` test suite · `GATE` CI or branch protection.

> **RULE 0 (MUST):** Every MUST in the tables below claims a property of code and is bound to
> `TYPE`/`LINT`/`TEST`/`GATE`. A rule without a gate is not ignored — **the gate is built**.
> Criteria requiring human judgment that cannot be gated live in the PR template, not here.
> The brevity of this file is a consequence of this rule, not a target.

## Project

This is Murat's default stack — filled in once so a new project doesn't re-research tooling from
scratch. It is still per-project: a project with a hard constraint this stack doesn't fit (e.g. an
existing JVM shop) overrides these fields, and the rest of the document is unaffected — the M/V/H/G/T/Q
rule catalog does not assume any particular language. No code is written with unfilled fields (R2).

| Field | Value | | Gate | Command |
|-|-|-|-|-|
| Stack | .NET (C#) API + Vite + React (TypeScript) frontend + PostgreSQL; mobile via WebView (Capacitor) | | All gates | `make gates` |
| Package manager | NuGet (backend) + npm (frontend) | | Architecture (M2–M5, M9, M10, M11, Q3) | `make arch` — ArchUnitNET (backend layers) + dependency-cruiser (frontend layers) |
| Install | `make install` — `dotnet restore` + `npm ci` + `npm ci --prefix frontend` | | UI (U2, U3) | `make style` — stylelint |
| Run | `make dev` — `dotnet run --project src/Voltflow.Api` + `dotnet run --project src/Voltflow.Worker` + `npm run dev --prefix frontend` | | Type check | `make typecheck` — `dotnet build` + `tsc -b --noEmit` |
| | | | Test | `make test` — `dotnet test` (xUnit + Testcontainers + FsCheck [Q11] + Stryker.NET [Q12]) + `npm test --prefix frontend` (Vitest/Playwright + fast-check [Q11] + Stryker [Q12]) |
| | | | Discovery (D1, D2, D5) | `make check-domain-map` |
| | | | Review (A7) | `make check-review` |

`make` targets are the single entry point (O6); each wraps the real per-stack tool named above —
none of it is invented, all of it is named. Contract testing (Q4) uses Spectral (language-agnostic
CLI) + PactNet (backend) / Pact JS (frontend). Tracing (T1) uses the OpenTelemetry .NET SDK and
OpenTelemetry JS SDK. The commit gate (O1, O2) is wired via husky + commitlint from the frontend's
`package.json`, which covers the whole repo's git hooks regardless of backend language. Until every
`make` target is a real, working script instead of an alias for nothing, no gate exists — running
one intentionally fails rather than silently passing. This is not an error, it is RULE 0.

**Mobile via WebView (Capacitor):** the mobile app is the same Vite/React UI wrapped in a native
shell, not a separate implementation — it inherits U1–U5 and Q5/Q6 as-is; no new UI rule is needed
for it.

## Source of truth

Rules live in this file; **data** lives in the project's standard files below. This file contains no data.
Do not write code in a related layer until the project has set up these files.

| File | Standard | Content |
|-|-|-|
| `catalog-info.yaml` | Backstage | component, owner, dependencies, ASVS level |
| `openapi.yaml` | OpenAPI 3.1 | HTTP contract; code is generated from this |
| `asyncapi.yaml` | AsyncAPI + CloudEvents | event contract |
| `docs/domain-map/<context>.md` | Event Storming + Bounded Context Canvas | one file per bounded context: commands, events, actors, policies, read models, ubiquitous language |
| `docs/adr/` | MADR | architectural decisions |
| `docs/reviews/<operation-id>.md` | — (A7) | self-review checklist when no forge is present |
| `.dependency-cruiser.cjs` / ArchUnit | — | layer rules |
| `commitlint.config.*` | Conventional Commits | commit format |
| `.github/PULL_REQUEST_TEMPLATE.md` | — | Definition of Done |

## D — Discovery

Runs before the first operation of a module is implemented. Discovery produces the domain map that
M9–M13 and the task index below operate on; it is not optional preamble, it is where module and
operation boundaries are decided.

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|D1|Before a module's first operation is implemented, its domain map is produced via Event Storming: Commands (imperative-verb name), Domain Events (past-tense name), Actors, Policies (event → command reactions), Read Models, and external dependencies; saved to `docs/domain-map/<context>.md`, one file per bounded context|MUST|GATE|
|D2|Each context's ubiquitous language is documented with an explicit "not to be confused with" entry for any term that recurs with a different meaning in another context (e.g. "User" in Auth vs. in Reporting)|MUST|GATE|
|D3|An unresolved Hotspot (an explicit open question surfaced during Event Storming) is ambiguity under R2; it blocks implementation of the operations it affects until resolved or explicitly deferred with a written reason|MUST|GATE|
|D4|A Policy (event → command reaction) found during Discovery is implemented as a sub-operation (M12); a Read Model is a query — H7 and H9–H13 do not apply to it, V8 (pagination) does|MUST|LINT|
|D5|Module-to-module relationships default to a lightweight depends-on list under the modular-monolith default (M1); the full DDD Context Map vocabulary (Partnership, Shared Kernel, Customer-Supplier, Conformist, Anti-corruption Layer, Open Host Service, Published Language, Separate Ways) is used only once a context graduates to a microservice under M1's ADR exception|MUST|GATE|
|D6|Strategic classification (Core / Supporting / Generic subdomain) is recorded per context to inform build-vs-buy and investment decisions (Principle 1, R4)|SHOULD|GATE|

## M — Architecture

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|M1|Default structure is modular monolith; microservices only with ADR|MUST|GATE|
|M2|Layer order `Domain → Application → Infrastructure → Presentation`; dependencies only inward|MUST|TEST|
|M3|Domain contains no framework, ORM, or transport type; exits via DTO only|MUST|LINT|
|M4|Interfaces defined in Application, implemented in Infrastructure|MUST|LINT|
|M5|Cross-module direct database access and joins are forbidden|MUST|TEST|
|M6|Contract is written first; client and server types are generated from the schema|MUST|GATE|
|M7|API versioning is SemVer; breaking change increments version, retired endpoint announced with `Sunset`|MUST|GATE|
|M8|Consumer ignores unknown fields (tolerant reader)|MUST|TEST|
|M9|Cross-cutting concerns — trace propagation, structured logging, audit write, error envelope (RFC 9457), idempotency check, outbox — are implemented once in the Infrastructure layer as pipeline middleware and applied uniformly to every operation; per-operation duplication of these concerns is forbidden|MUST|LINT|
|M10|Operation-specific code is limited to: the Command schema, domain validation rules, business logic, result type, and operation-scoped error codes; all other concerns are inherited from the pipeline without modification|MUST|LINT|
|M11|Every operation is initiated by an immutable Command object that carries a `command_id` and captures: actor identity and role, trigger source (UI screen, API caller, scheduler, event), exact input payload, session context, and timestamp; the Command is recorded to the audit log as-is — never as a derived or interpreted form; all downstream logic reads from the Command, not from re-fetched state; in a fork (M12) the parent `command_id` is propagated to each sub-operation's own `operation_id`|MUST|LINT|
|M12|When an operation spawns sub-operations (fork, batch item, scheduled execution), each sub-operation is a first-class operation with its own Command object, `operation_id`, `trace_id`, and audit trail; the parent operation's terminal status reflects all sub-operation outcomes; sub-operations are independently cancellable and retryable|MUST|TEST|
|M13|Every operation that can be administratively overridden or requires pre-execution approval defines an explicit command type for each intervention (e.g. `ApproveCommand`, `AdminOverrideCommand`, `RejectCommand`); admin and approval operations carry the same pipeline requirements as user-initiated operations (M9 M11 H9 T5 T7) and are not exempt from audit|MUST|TEST|

## V — Boundary and data

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|V1|Parsed at the boundary, not validated (parse, don't validate); result is a domain-meaningful type|MUST|TYPE|
|V2|All external input passes through a single entry point; no second input path|MUST|LINT|
|V3|Configuration is also parsed; if invalid, it fails at startup, not on the first request|MUST|TEST|
|V4|Schema migration uses Expand/Contract in three steps; single-step column deletion or rename is forbidden|MUST|GATE|
|V5|Uniqueness and integrity are also guaranteed at the database level|MUST|TEST|
|V6|Conflicting updates are not overwritten; returns conflict error with version token|MUST|TEST|
|V7|Floating point is forbidden in financial domains; time is stored as UTC, system clock is injected|MUST|LINT|
|V8|Unbounded list responses are forbidden; a ceiling is required, pagination via `Link` (RFC 8288)|MUST|TEST|

## H — Errors and resilience

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|H1|All errors return an RFC 9457 Problem Details body carrying trace and operation IDs|MUST|TEST|
|H2|Error codes are catalogued; no code outside the catalogue can be emitted|MUST|GATE|
|H3|Business rule errors are values, not exceptions (Result); silent swallowing is forbidden|MUST|LINT|
|H4|Mutation endpoints carry the Command's `command_id` as idempotency key; re-sending the same `command_id` with the same payload is a safe no-op returning the original result; same `command_id` with a different payload returns conflict (409)|MUST|TEST|
|H5|Retries use exponential backoff + jitter; ceiling is defined, after ceiling goes to DLQ|MUST|TEST|
|H6|DLQ fill triggers an alarm; silent accumulation is forbidden|MUST|GATE|
|H7|Every mutation writes business data and its audit record in a single transaction; when a broker exists, event publication is also atomic within that same transaction; consumers are idempotent|MUST|TEST|
|H8|When a dependency is down, return a controlled rejection; do not produce incorrect results|MUST|TEST|
|H9|Before executing a mutation, the Command object and `status: pending` are written to the audit log in a separate, prior transaction (intent-first / WAL principle); this write precedes and is independent of the H7 transaction; on completion H7's transaction updates the status to `completed` or `failed` with the error code; a `pending` record that never resolves is detectable and alerts|MUST|TEST|
|H10|Every mutation that modifies persistent state must define either a rollback path (within transaction boundary) or a compensating operation (Saga pattern) for post-commit undo; the compensation is a first-class operation with its own Command object, `operation_id`, and audit record; it is tested independently|MUST|TEST|
|H11|An operation in `pending` or `in_progress` state must be explicitly cancellable via a Cancel command; cancellation transitions the operation to `cancelled`, triggers compensation for any completed steps, and is recorded in the audit log with actor identity|MUST|TEST|
|H12|Every operation defines a maximum execution duration in configuration; on timeout the operation transitions to `timed_out` and the cancellation flow (H11) is invoked automatically; no operation waits unboundedly|MUST|TEST|
|H13|A multi-step operation records the completion status of each step independently in the audit log; partial failure is surfaced as a distinct terminal status (`partially_completed`), not silently swallowed or collapsed into total failure; each failed step carries its error code|MUST|TEST|

## G — Security

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|G1|Target OWASP ASVS 5.0 level is declared and audited|MUST|GATE|
|G2|Default deny; authorization on the server, UI hiding is UX only|MUST|TEST|
|G3|PII is masked in logs, traces, and error output|MUST|TEST|
|G4|Stack traces, tokens, and secrets do not reach the client; no distinguishing message on auth failure|MUST|TEST|
|G5|Raw tokens are not stored; use digests and revocation lists|MUST|TEST|
|G6|Every mutation has rate or concurrency limiting at its entry point (V2), regardless of transport; an HTTP endpoint returns `Retry-After`, a non-HTTP entry point (job, consumer, scheduler) returns the equivalent catalogued error (H2)|MUST|TEST|
|G7|Secrets are not kept in the repo; secret and vulnerability scanning is gated (e.g. gitleaks, package-manager audit) in CI or a local pre-commit gate|MUST|GATE|
|G8|SBOM is generated — CycloneDX or SPDX (EU CRA: Dec 11, 2027)|MUST|GATE|
|G9|Build provenance is produced at SLSA L3 level, signed with Sigstore/cosign|SHOULD|GATE|
|G10|OpenSSF Scorecard and SPDX license policy run in CI|SHOULD|GATE|

## T — Telemetry

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|T1|Instrumentation uses OpenTelemetry; names and attributes conform to semantic conventions|MUST|LINT|
|T2|Distributed trace propagates via W3C Trace Context; does not break at service boundaries|MUST|TEST|
|T3|Logs are structured; free-text is forbidden|MUST|LINT|
|T4|Every request carries an `operation_id` returned in the response; for a single operation `operation_id` equals the triggering `command_id`; for sub-operations (M12) each carries its own `operation_id` with the parent `command_id` as context|MUST|TEST|
|T5|Audit trail is immutable and append-only; contains actor, operation_id, trace_id, and before/after values; PII is masked|MUST|TEST|
|T6|Health endpoints are separate: liveness, readiness, startup|MUST|TEST|
|T7|Audit log is stored in a table separate from the business data table; the two are linked by `operation_id`; they carry independent retention and access policies|MUST|TEST|

## Q — Testing and quality

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|Q1|Integration tests run against real dependencies (Testcontainers)|MUST|GATE|
|Q2|Tests are isolated, run in parallel; seed data is deterministic|MUST|TEST|
|Q3|Layer dependency is verified by architectural test (ArchUnit / dependency-cruiser)|MUST|TEST|
|Q4|Endpoints are tested against the contract (Spectral); separately evolving client uses Pact|MUST|GATE|
|Q5|Test selectors use `data-testid`; fragile structural selectors are forbidden|MUST|LINT|
|Q6|Accessibility is automatically audited against WCAG 2.2 AA target|MUST|GATE|
|Q7|Latency and bundle size budgets are a CI gate; thresholds are kept in configuration|SHOULD|GATE|
|Q8|Tests follow a size-based split — small (single process, no I/O), medium (localhost-only I/O, e.g. Testcontainers per Q1), large (real dependencies, multi-feature E2E); the majority are small, a minority are large|MUST|GATE|
|Q9|Every dependency-failure and timeout path (H8, H12) has a fault-injection test that simulates unavailability, added latency, or timeout at the boundary|MUST|TEST|
|Q10|Every conflict-prone mutation (V6) and idempotency path (H4) has a concurrency test that fires two or more simultaneous requests at the same resource and asserts the outcome against a correctness model|MUST|TEST|
|Q11|Domain logic with a bounded input space (M10) is verified with property-based tests in addition to example-based tests|SHOULD|TEST|
|Q12|Test suite effectiveness is measured with mutation testing; a mutation survival rate above the configured threshold fails the gate|SHOULD|GATE|
|Q13|Every lifecycle and state-machine path — rollback/compensation (H10), cancellation (H11), partial failure (H13), fork/sub-operation outcomes (M12), admin/approval intervention (M13) — is tested by explicitly driving the operation into that state (mid-flight cancel, forced per-step failure, mixed sub-operation outcomes, admin override, approval rejection) and asserting both the resulting status and its audit trail; a happy-path test alone does not satisfy the TEST enforcement on these rules|MUST|TEST|

## U — UI

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|U1|Single component library: Material UI, Ant Design, Bootstrap, or shadcn/ui. Do not mix|MUST|LINT|
|U2|Glassmorphism is forbidden; `backdrop-filter` is blocked by stylelint|MUST|LINT|
|U3|Hard-coded colors and dimensions are forbidden; use design tokens|MUST|LINT|
|U4|A new component library, state manager, or styling approach cannot be added alongside the existing one|MUST|GATE|
|U5|Theme is managed with `next-themes` `ThemeProvider` (Next.js)|SHOULD|GATE|

## O — Process and operations

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|O1|Traceability key is the issue ID. Branch `<type>/<ISSUE>-<name>`, commit body contains `Refs:`|MUST|GATE|
|O2|Commit format is Conventional Commits; commitlint is gated|MUST|GATE|
|O3|Every architectural decision is written in MADR format under `docs/adr/`|MUST|GATE|
|O4|Configuration comes from environment variables, not embedded in code (12-Factor)|MUST|LINT|
|O5|Build is reproducible: lock file is committed, versions are pinned|MUST|GATE|
|O6|Clone → single command → running state; setup depends on a script, not a document|MUST|GATE|

CI/CD pipeline composition, dependency-update tooling, and release/branching strategy (feature flags
vs. release branches) are release-and-deploy-adjacent and out of this document's charter (see
Lifecycle) — they are decided per downstream project, not standardized here.

## A — Agent gates

Mechanical support for the red lines. R1–R8 govern behavior; these bind infrastructure.

| ID | Rule | Level | Enforcement |
|-|-|:-:|-|
|A1|Accessible tool and file scope is listed; default deny|MUST|GATE|
|A2|Irreversible actions (production deploy, data deletion, external message, payment) require human approval|MUST|GATE|
|A3|When a forge is present, agent changes pass human review via branch protection|MUST|GATE|
|A4|A route, column, event, or error code not defined in the schema cannot be added|MUST|GATE|
|A5|Generated code is not manually edited; the generator is re-run|MUST|GATE|
|A6|Agent commits are marked; every change is revertable with a single command|MUST|GATE|
|A7|When no forge is present (per the Closed rules table, A3), every operation is self-reviewed before being marked done: the full diff is read against the rules listed in the operation's task-index row, each rule is checked off, and the checklist with its outcome is written to `docs/reviews/<operation-id>.md`; a `done` claim (R6) without this file present and complete is a silent skip (R7)|MUST|GATE|

## Closed rules in this project

Every closed rule is listed here. A rule not in this table cannot be skipped — skipping
is a silent pass (R7). Permanent closure also requires an ADR under `docs/adr/`.

| Rule | Reason | ADR |
|-|-|-|
| U5 | Project's frontend is Vite + React, not Next.js (see Stack field above); `next-themes` requires the Next.js App Router and doesn't apply. MUI's own `ThemeProvider` plus a `localStorage`-backed mode toggle serves the same role. | `docs/adr/0001-frontend-stack-vite-not-nextjs.md` |
| Q4 (Pact half only) | Frontend and backend are one repo, one release train — no separately-evolving external consumer exists yet to verify against. Spectral (the other half of Q4) is not closed; it runs against `openapi.yaml` in `make contract`. | `docs/adr/0004-q4-contract-testing-scope.md` |
| G9 | No build provenance (SLSA L3, Sigstore/cosign) is produced: `deploy.yml` builds and pushes the images to GHCR for a single host, and nothing verifies provenance downstream, so an attestation would be an unchecked artifact. Reopen when the images gain a second consumer or a verification step. | `docs/adr/0008-close-g9-slsa-provenance-and-scorecard.md` |
| G10 (Scorecard half only) | OpenSSF Scorecard is not run yet: its branch-protection, code-review and pinned-dependency checks would only report decisions that are still open (A3/A7, roadmap Phase 8). The other half of G10, the SPDX license policy, is not closed; it runs as `make license-policy`. | `docs/adr/0008-close-g9-slsa-provenance-and-scorecard.md` |

This table starts empty for every new project instantiated from this document; nothing is
pre-closed. Example of the mechanism: `A3|No forge in use; substitute review process is formalized
and gated as A7|—`. When a forge is set up later, delete that row and re-enable A3. A rule not in
this table cannot be skipped — skipping is a silent pass (R7). Permanent closure also requires an
ADR under `docs/adr/`. Rules that commonly get closed on scale grounds in a small, low-stakes
project — never assume this without deciding it explicitly per project — are typical candidates:
`G9`, `Q4`, `T2`, `Q11`, `Q12`, `D6`. `Q8`, `Q9`, and `Q10` are MUST and are not candidates for
closure on scale grounds alone; close them only with an ADR explaining why the risk they cover
(untested failure paths, untested concurrency) is accepted.

## Done

Tests were run **and** output was shown **and** all gates returned exit 0. Without all three,
"done" cannot be said (R6).

For a workflow operation specifically: a business function alone is not done. Done requires that
every lifecycle path is implemented and tested — not only the happy path:

| Path | Rule | Tested by (how) |
|-|-|-|
| Happy path | M9 M10 M11 · H4 H7 H9 · T4 T5 T7 · H2 | Q1 Q2 Q8 |
| Authorization, PII, rate/concurrency limit | G2 G3 G6 | **gap — no Q-rule defines the technique yet** |
| Rollback / compensation | H10 | Q13 |
| Cancellation | H11 | Q13 |
| Timeout | H12 | Q9 |
| Partial failure (multi-step only) | H13 | Q13 |
| Sub-operations (multi-step only: fork / batch / scheduled) | M12 | Q10 Q13 |
| Admin / approval intervention | M13 | Q13 |

The "Rule" column says what must be true; the "Tested by" column says which Q-rule defines the
technique. A rule with TEST enforcement and no matching Q-rule executed is not done — it is untested.

A path without a test does not exist.

**Belongs in the PR template** (not here, because it requires human judgment): untrusted text not
interpreted as instructions · RED/USE metrics, SLO and alarm ownership · backup, RTO, RPO and restore
drill · library default appearance preserved.

(Test pyramid balance is gated mechanically by Q8, and bounded-context/ubiquitous-language alignment
by D1/D2 — neither is left to PR judgment. Deployment rollback — releasing a previous build version
if the current one breaks — is release strategy and is out of this document's charter entirely, per
Lifecycle; it is not merely deferred to the PR template, and it is distinct from H10's operation-level
compensation, which undoes a completed business operation's data effects, not a bad deploy.)
