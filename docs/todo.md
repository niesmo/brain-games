# Brain Games Todo

## Read Me First

Before starting any new work:

1. Read `docs/plan.md`.
2. Read this file.
3. Update the progress markers before and after meaningful work.
4. Keep the “Next Up” section honest and specific.

## Progress Board

- `[x]` Repository scaffolded
- `[x]` Shared domain contracts created
- `[x]` Initial F# API vertical slice created
- `[x]` Fable client build verified locally
- `[x]` Shared score-rule tests passing
- `[ ]` Supabase auth wired in
- `[ ]` Supabase persistence wired in
- `[ ]` SPA wired to registration/profile/leaderboard endpoints
- `[ ]` End-to-end score submission verified through the SPA

## In Progress

- Connect the playable client loop to the API contracts already present on the server.

## Next Up

1. Wire onboarding, leaderboard fetch, and score submission from the SPA to the server.
2. Run the server locally and verify the leaderboard/profile endpoints with real requests.
3. Replace local-dev registration with real Supabase auth.
4. Persist scores, profiles, and streaks in Supabase/Postgres.

## Notes To Future Sessions

- Keep this file and `docs/plan.md` aligned.
- If priorities change, update both docs before making broad code changes.
- Do not treat Supabase as optional for production; the local-dev auth path is only a bootstrap path.
