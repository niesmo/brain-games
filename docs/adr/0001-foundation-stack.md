# ADR 0001: Foundation Stack

## Status

Accepted

## Context

The project needs a fast-moving foundation that keeps the frontend and backend in one language, supports game-heavy UI, and stays affordable during validation.

## Decision

- Use F# across the stack.
- Use Fable and Elmish for the browser client.
- Use ASP.NET Core Web API for the server boundary.
- Use Supabase as the default free-tier BaaS target for auth, storage, and relational persistence.
- Keep planning artifacts in `docs/` and architecture decisions in `docs/adr/`.
