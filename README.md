# Brain Games

Brain Games is a web-first F# project for brain-training games, lightweight lessons, and competitive leaderboards. The current milestone is a foundation slice centered on a playable local memory game, a compiled F# API, and a Supabase-ready backend shape.

## Stack

- Frontend: F# with Fable, Elmish, and Vite
- Backend: ASP.NET Core Web API in F#
- Shared contracts: F# class library
- Planned BaaS: Supabase free tier for auth, database, and asset storage

## Repo Layout

- `src/Client`: Fable SPA scaffold and memory-game vertical slice UI
- `src/Server`: F# API for boot data, leaderboard, profile, and score submission
- `src/Shared`: shared DTOs and score rules
- `tests/Server.Tests`: shared/domain tests
- `docs/`: project plan, todo list, ADRs, and backend bootstrap notes

## Local Setup

1. Start the API:
   - `dotnet run --project src/Server/BrainGames.Server.fsproj`
2. Install frontend dependencies:
   - `cd src/Client`
   - `npm install`
3. Start the client:
   - `npm run dev`

The client expects the API at `https://localhost:7243` by default and will fall back to `http://localhost:5074` if needed.

## Current State

- Playable local memory game client built in F# and Elmish
- F# API compiles with boot, leaderboard, profile, and score-submission endpoints
- Shared score contracts and tests are in place
- Supabase persistence/auth is planned next, but not wired yet

## Operating Rule

Before making further changes, read `docs/plan.md` and `docs/todo.md` first. Keep both files up to date whenever scope or progress changes.
