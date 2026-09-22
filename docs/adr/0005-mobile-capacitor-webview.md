# 0005 Mobile Strategy: Capacitor WebView
## Context and Problem Statement
Voltflow requires a mobile presence for certain field operations (e.g., Quick Sales, Work Orders) alongside the desktop web interface. We need to decide how to deliver this mobile application.
## Decision Drivers
* Limited engineering resources; we cannot maintain separate codebases for iOS, Android, and Web.
* The mobile application shares identical business rules and UI workflows with the web version.
## Considered Options
* Option 1: React Native / Flutter (Native UI)
* Option 2: Capacitor (WebView wrapper around the Vite/React SPA)
## Decision Outcome
Chosen option: **Capacitor WebView**, because the mobile app is simply the same Vite/React UI wrapped in a native shell. It inherits all UI styling and accessibility rules (U1-U5, Q5/Q6) as-is, meaning no separate UI rules or dedicated mobile maintenance is required.
