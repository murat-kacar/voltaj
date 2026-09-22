# 0004 API Strategy: Contract Testing Scope
## Context and Problem Statement
We need to ensure that the mobile frontend and web application do not unexpectedly break due to backend API changes, given that they may evolve and deploy independently.
## Decision Drivers
* The mobile application (Capacitor WebView) cannot be forced to update immediately when backend changes are deployed.
* API endpoints must remain backwards compatible.
## Considered Options
* Option 1: E2E Integration Tests (Brittle, slow, environment-dependent)
* Option 2: Consumer-Driven Contract Testing (Pact + Spectral)
## Decision Outcome
Chosen option: **Consumer-Driven Contract Testing (Pact + Spectral)**, because it ensures API contracts are never broken silently (Q4). The backend will test its endpoints against the OpenAPI contract using Spectral, while the separately evolving client uses Pact to guarantee compatibility.
