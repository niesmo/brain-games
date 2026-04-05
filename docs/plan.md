# Brain Games Plan

## Read Me First

Any future work session should read this file and `docs/todo.md` before making changes. Update both files whenever the implementation state, priorities, or next steps change.

AI agents should treat this file as the current execution plan unless the user explicitly overrides it.

## Product Direction

Brain Games exists to give people a healthier alternative to empty social scrolling by making concentration, recall, and learning feel competitive and fun. The first release focuses on one memory game, short lessons, repeat play, and a leaderboard loop strong enough to bring people back daily.

## Current Architecture

- Web-first product with an F# stack end to end
- `Fable + Elmish` SPA frontend
- ASP.NET Core F# API for trusted score handling and profile aggregation
- The server has been split into multiple modules; do not collapse new server work back into one file
- Fantomas is the standard formatter for F# code in this repo
- Supabase is the preferred hosted option for auth, persistence, and file storage, but auth is optional for end users
- Shared F# contracts keep frontend and backend aligned

## Delivery Rules For Future Sessions

- Do not force sign-in for all users. Guest play must remain a supported path unless the user explicitly changes product direction.
- Keep the API as the trusted boundary for score validation, profile aggregation, and persistence rules.
- Prefer persistence work before auth work. Durable profiles and score storage come before optional account login.
- Use the `ui-ux-pro-max` skill before substantial UI implementation or redesign work.
- Read `docs/ui-reference/README.md` when a future UI task needs a premium dark analytics/dashboard direction.
- Keep `docs/plan.md` and `docs/todo.md` aligned whenever priorities or architecture decisions change.

## Current Vertical Slice

- `Memory Match Sprint` playable local game loop in the Fable client
- Client UI now uses a game-first dark dashboard shell with the board as the primary focus
- F# API endpoints for boot data, profile data, leaderboard data, and score submission
- Shared domain contracts and score rules used across projects
- In-repo planning artifacts and ADR trail for future sessions
- Server code is modularized into configuration, seed data, store logic, and endpoint mapping
- Repo-local Fantomas tooling is installed for formatting
- Docker-based local development is available for running client and server together
- A reusable dashboard style reference lives in `docs/ui-reference/README.md`

## Verified Progress

- `dotnet restore` succeeds for the solution
- Shared, server, client, and test projects build locally
- Frontend production build succeeds with Vite/Fable
- Score rule tests pass
- Server modularization is complete
- Fantomas tooling is configured and runnable with `dotnet fantomas .`
- Docker local-dev workflow is configured for the client and server
- A reusable premium dark dashboard UI reference has been documented
- The client has been redesigned into a game-first dashboard shell and pushed to `main`
- Supabase persistence and optional auth wiring are still pending

## Recommended Hosting Shape

- Host the client as a static SPA build from `src/Client`
- Host the API as a separate ASP.NET Core service from `src/Server`
- Use separate environments for `local`, `staging`, and `production`
- Keep the API and SPA as separate deployable units for now rather than serving the SPA from ASP.NET Core
- Use managed Postgres for persistence once storage is enabled

## Auth And Persistence Strategy

- Guest play remains the default path
- Optional auth should unlock durable cross-device identity, not basic access to the product
- Persistence should be implemented before optional auth
- Persist `player_profiles` and `score_attempts` first
- Keep games and lesson metadata code-seeded until content management becomes necessary
- Keep score validation and ranking logic on the server even after persistence is added

## Immediate Next Steps

1. Add a persistence boundary in the server so in-memory and Postgres-backed storage can coexist behind the same application logic.
2. Persist players and score attempts in Postgres without forcing auth.
3. Connect the SPA to the existing API endpoints for guest registration, leaderboard fetch, profile fetch, and score submission.
4. Replace placeholder dashboard/support metrics in the client with real API-backed data.
5. Add optional auth after persistence, keeping guest play intact.
6. Add anti-cheat hardening to the score submission endpoint.
7. Introduce a second game category only after the memory loop, persistence, and onboarding loop are stable.
