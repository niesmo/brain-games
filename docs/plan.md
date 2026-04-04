# Brain Games Plan

## Read Me First

Any future work session should read this file and `docs/todo.md` before making changes. Update both files whenever the implementation state, priorities, or next steps change.

## Product Direction

Brain Games exists to give people a healthier alternative to empty social scrolling by making concentration, recall, and learning feel competitive and fun. The first release focuses on one memory game, short lessons, repeat play, and a leaderboard loop strong enough to bring people back daily.

## Current Architecture

- Web-first product with an F# stack end to end
- `Fable + Elmish` SPA frontend
- ASP.NET Core F# API for trusted score handling and profile aggregation
- Supabase is the intended free-tier BaaS for auth, persistence, and file storage
- Shared F# contracts keep frontend and backend aligned

## Current Vertical Slice

- `Memory Match Sprint` playable local game loop in the Fable client
- F# API endpoints for boot data, profile data, leaderboard data, and score submission
- Shared domain contracts and score rules used across projects
- In-repo planning artifacts and ADR trail for future sessions

## Verified Progress

- `dotnet restore` succeeds for the solution
- Shared, server, client, and test projects build locally
- Frontend production build succeeds with Vite/Fable
- Score rule tests pass
- Supabase wiring is still pending

## Immediate Next Steps

1. Add real Supabase auth and profile persistence.
2. Connect the SPA to the existing API endpoints for registration, profile, leaderboard, and score submission.
3. Persist attempts, leaderboard queries, and streaks in Supabase/Postgres instead of in-memory storage.
4. Add anti-cheat hardening to the score submission endpoint.
5. Introduce a second game category only after the memory loop is stable.
