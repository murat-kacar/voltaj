# 4. Q4 contract testing: Spectral required now, Pact closed for now

* Status: accepted
* Date: 2026-09-18

## Context and Problem Statement

Q4 (MUST/GATE): "Endpoints are tested against the contract (Spectral); separately evolving client uses
Pact." Spectral applies unconditionally. Pact (PactNet backend / Pact JS frontend) is specifically for
a *separately evolving* consumer — a client released on its own cadence, independent of the API.
Voltflow's frontend and backend live in one repo (`Voltflow.sln` + `frontend/`), share one CI/deploy
pipeline (`compose.yml` builds both), and are released together — there is no independently-versioned
external consumer today.

## Decision Drivers

* `AGENTS.md`'s own Closed-rules guidance explicitly lists Q4 as a typical scale-based closure
  candidate (alongside G9, T2, Q11, Q12, D6) — "never assume this without deciding it explicitly per
  project," which this ADR is doing.
* R4 (no bloat): PactNet/Pact JS is real infrastructure (a Pact Broker or file-based contract
  exchange, consumer + provider verification tests) with no consumer to verify against yet.

## Considered Options

* **A. Wire Spectral into `make gates` now (done - see `.spectral.yaml`, `openapi.yaml` passes with
  0 errors); close Pact via this ADR until a separately-released consumer exists.**
* B. Stand up PactNet + Pact JS now, verifying the one in-repo frontend as if it were an external
  consumer.

## Decision Outcome

Chosen option: **A**. Add a `Closed rules in this project` row for Q4 in `AGENTS.md`, scoped
specifically to the Pact half of the rule — Spectral is not closed, it is implemented and gating.

Option B was rejected: consumer-driven contract testing exists to catch a provider change breaking a
consumer *it cannot see being changed alongside it* — that scenario doesn't exist here yet, so the
tests would verify nothing a same-PR `make gates` run (typecheck + Spectral) doesn't already catch.

### Consequences

* Good: the actually load-bearing half of Q4 (Spectral) is real today, not deferred.
* Neutral: if the frontend and backend ever split into independently deployed/released artifacts (or
  a third-party integrates against the API), this ADR should be revisited and Pact stood up before
  that split ships — re-open Q4's Closed-rules row at that point.
