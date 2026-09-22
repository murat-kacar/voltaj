# 0001 Frontend Stack: Vite + React SPA
## Context and Problem Statement
We need to select a frontend framework for the Voltflow web application and its accompanying mobile application.
## Decision Drivers
* The mobile application will be deployed as a native app wrapping a WebView (Capacitor).
* SEO and Server-Side Rendering (SSR) are not required for our internal/B2B tooling.
* Build speed, simplicity, and low operational overhead are prioritized.
## Considered Options
* Option 1: Next.js (React Framework with SSR/SSG capabilities)
* Option 2: Vite + React (Pure Single Page Application)
## Decision Outcome
Chosen option: **Vite + React (Pure SPA)**, because a pure client-side application is the most natural fit for a Capacitor-based mobile deployment. Next.js introduces unnecessary complexity (Node.js server requirements or strict static export limitations) for features (SSR/SEO) that provide no value to this specific project context.
