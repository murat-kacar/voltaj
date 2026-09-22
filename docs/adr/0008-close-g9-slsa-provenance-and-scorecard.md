# 0008 Security: SLSA Provenance and OpenSSF Scorecard
## Context and Problem Statement
To meet internal security standards and adhere to upcoming compliance regulations (e.g., EU CRA), we need to establish software supply chain security verification processes.
## Decision Drivers
* Supply chain attacks via compromised dependencies are a high risk.
* Build provenance must be verifiable.
## Considered Options
* Option 1: Ad-hoc manual audits
* Option 2: Automated SLSA L3 Provenance + OpenSSF Scorecard in CI
## Decision Outcome
Chosen option: **Automated SLSA L3 Provenance + OpenSSF Scorecard in CI**, because it systematically enforces supply chain security (G9, G10). Build provenance will be produced at the SLSA L3 level and signed using Sigstore/cosign, while the OpenSSF Scorecard will continuously evaluate our repository security posture in CI.
