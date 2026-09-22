# 0002 Architecture: Modular Monolith Default
## Context and Problem Statement
We need to establish the baseline architectural deployment pattern for the Voltflow backend.
## Decision Drivers
* Premature microservice adoption introduces distributed systems complexity, operational overhead, and network latency.
* We must preserve the ability to scale out specific domains later if business needs demand it.
## Considered Options
* Option 1: Microservices Architecture
* Option 2: Modular Monolith (Strict Layering per Context)
## Decision Outcome
Chosen option: **Modular Monolith**, because it provides the deployment simplicity of a single process while enforcing strict bounded context boundaries (M2-M5) via tooling (ArchUnitNET). Microservices will only be adopted by exception (via a new ADR) when a specific context graduates to require independent scaling or deployment life cycles.
