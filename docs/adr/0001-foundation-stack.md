# ADR 0001: Foundation Stack

## Status

Accepted

## Context

The project needs a fast-moving foundation that keeps the frontend and backend in one language, supports game-heavy UI, and stays affordable during validation.

## Decision

- Use F# across the stack.
- Use Fable and Elmish for the browser client.
- Use ASP.NET Core Web API for the server boundary.
- Use Supabase as the preferred free-tier BaaS target for optional auth, storage, and relational persistence.
- Preserve a guest-play path and do not require sign-in for all users by default.
- Keep the API as the trusted boundary even if Supabase is adopted for hosted infrastructure.
- Keep planning artifacts in `docs/` and architecture decisions in `docs/adr/`.
