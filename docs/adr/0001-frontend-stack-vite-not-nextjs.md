# 1. Frontend stack is Vite + React, not Next.js

* Status: accepted
* Date: 2026-09-18

## Context and Problem Statement

`AGENTS.md`'s `Project` table declares Murat's default stack as ".NET (C#) API + Next.js
(TypeScript) frontend + PostgreSQL", and `U5` requires theming via `next-themes`'s `ThemeProvider`.
Voltflow's actual frontend (`frontend/`) is built on Vite + React 19 + MUI, with no Next.js
dependency anywhere in `frontend/package.json`, and is already a substantial, working application
(40+ components, i18n, routing, MUI theme). `next-themes` is a Next.js-App-Router-specific package
and does not apply to a Vite SPA.

This is a real conflict between the document's stated default and the shipped code, not a rule that
was simply skipped — it needs a recorded decision (Session start step 3 / R2), not a silent fix in
either direction.

## Decision Drivers

* `AGENTS.md`'s own `Project` section says the stack fields are "still per-project: a project with a
  hard constraint this stack doesn't fit... overrides these fields, and the rest of the document is
  unaffected" — a per-project override is the document's own escape hatch for exactly this situation.
* The frontend is not a stub; it is an existing, working Vite application. Migrating it to Next.js
  would mean rewriting routing, the build pipeline, and every component's theme access, for no
  functional gain — Voltflow doesn't need Next.js features (SSR, RSC, file-based routing) that Vite
  lacks.
* MUI already ships its own `ThemeProvider` (`@mui/material/styles`), which covers the same
  responsibility `next-themes` would (light/dark mode, persisted preference) without a Next.js
  runtime underneath it.

## Considered Options

* **A. Update `AGENTS.md`'s `Project` table to Vite + React, and close `U5` via this ADR.**
* B. Migrate `frontend/` from Vite to Next.js to match the document's stated default.
* C. Leave the document and code inconsistent and revisit later.

## Decision Outcome

Chosen option: **A** — update the `Project` table's Stack, Package manager, Install, and Run fields
to reflect Vite + React + npm (the project also uses npm, not pnpm — `package-lock.json` is
committed at both the root and in `frontend/`, no `pnpm-lock.yaml` exists anywhere in the repo), and
close `U5` in the Closed rules table, referencing this ADR.

Option B was rejected: the cost (a full frontend rewrite) is disproportionate to the benefit (matching
a *default* that only applies "when no other stack constraint has been stated" — this project has
now stated one). Option C was rejected because `AGENTS.md` requires every closed rule to have an ADR
on file (R7) — leaving it silently inconsistent is a silent skip, not a neutral no-op.

### Consequences

* Good, because the document now matches what actually ships, and future contributors reading
  `AGENTS.md` won't be misled into thinking Next.js APIs are available.
* Good, because `U5`'s underlying goal (a single, consistent theming mechanism, not ad hoc
  per-component styling) is still met — via MUI's `ThemeProvider`, already in use
  (`frontend/src/theme.ts`).
* Neutral, because if a future requirement genuinely needs SSR/RSC, this ADR would need to be
  revisited and the migration cost paid then, not avoided forever.
