# Brain Games Todo

## Read Me First

Before starting any new work:

1. Read `docs/plan.md`.
2. Read this file.
3. Update the progress markers before and after meaningful work.
4. Keep the “Next Up” section honest and specific.
5. Do not assume auth is mandatory; guest play is part of the current plan.
6. Use the `ui-ux-pro-max` skill before major UI work.

## Progress Board

- `[x]` Repository scaffolded
- `[x]` Shared domain contracts created
- `[x]` Initial F# API vertical slice created
- `[x]` Fable client build verified locally
- `[x]` Shared score-rule tests passing
- `[x]` Server split into modules instead of one file
- `[x]` Fantomas tool configured for repo-local formatting
- `[ ]` Supabase auth wired in
- `[ ]` Supabase persistence wired in
- `[ ]` Persistence boundary introduced for in-memory vs Postgres-backed storage
- `[ ]` SPA wired to registration/profile/leaderboard endpoints
- `[ ]` End-to-end score submission verified through the SPA

## In Progress

- Introduce persistence under the existing server API without forcing sign-in.

## Next Up

1. Add a storage abstraction in `src/Server` so the current in-memory flow and a Postgres-backed flow can share the same server logic.
2. Persist player profiles and score attempts in Postgres while keeping guest registration available.
3. Wire onboarding, leaderboard fetch, profile fetch, and score submission from the SPA to the server.
4. Use the `ui-ux-pro-max` skill before reworking the onboarding/profile/leaderboard UI.
5. Add optional Supabase auth only after persistence and guest-flow wiring are in place.
6. Run the server locally and verify the leaderboard/profile endpoints with real requests against the chosen persistence mode.

## Notes To Future Sessions

- Keep this file and `docs/plan.md` aligned.
- If priorities change, update both docs before making broad code changes.
- Do not remove guest play unless the user explicitly changes product direction.
- Treat Supabase as the preferred hosted backend path, not as a mandatory requirement for all users.
- Keep server-side score validation authoritative even after persistence and auth are added.
