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

1. Start both services with Docker Compose:
   - `docker compose up --build`
2. Open the UI at:
   - `http://localhost:5173`
3. The API is available at:
   - `http://localhost:5189`

Manual startup still works if you prefer:

1. Start the API:
   - `dotnet run --project src/Server/BrainGames.Server.fsproj`
2. Restore local dotnet tools:
   - `dotnet tool restore`
3. Install frontend dependencies:
   - `cd src/Client`
   - `npm install`
4. Start the client:
   - `npm run dev -- --host 0.0.0.0 --port 5173`

If the UI appears blank, confirm the frontend dev server is running on `http://localhost:5173`. The API does not currently serve the SPA itself.

## Formatting

- `dotnet fantomas .`

## Current State

- Playable local memory game client built in F# and Elmish
- F# API compiles with boot, leaderboard, profile, and score-submission endpoints
- Server code is split into modules instead of being concentrated in one file
- Shared score contracts and tests are in place
- Fantomas is configured as the standard F# formatter
- Persistence is planned before optional auth, and guest play remains part of the intended product flow

## Operating Rule

Before making further changes, read `docs/plan.md` and `docs/todo.md` first. Keep both files up to date whenever scope or progress changes.
