namespace BrainGames.Server

open System
open BrainGames.Shared
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

[<RequireQualifiedAccess>]
module Endpoints =
    let map (app: WebApplication) =
        app.MapGet("/api/health", Func<IResult>(fun () -> Results.Ok({| status = "ok"; utc = DateTime.UtcNow |})))
        |> ignore

        app.MapGet("/api/boot", Func<BrainGamesStore, BootResponse>(fun store -> store.Boot()))
        |> ignore

        app.MapPost(
            "/api/players/register",
            Func<RegisterPlayerRequest, BrainGamesStore, IResult>(fun request store ->
                if String.IsNullOrWhiteSpace request.DisplayName || String.IsNullOrWhiteSpace request.Email then
                    Results.BadRequest({| error = "Display name and email are required." |})
                else
                    store.RegisterPlayer(request) |> Results.Ok)
        )
        |> ignore

        app.MapGet(
            "/api/leaderboard/{gameId}",
            Func<string, BrainGamesStore, LeaderboardEntry array>(fun gameId store -> store.Leaderboard(gameId))
        )
        |> ignore

        app.MapGet(
            "/api/profile/{playerId}",
            Func<string, BrainGamesStore, IResult>(fun playerId store ->
                match store.Profile(playerId) with
                | Some profile -> Results.Ok(profile)
                | None -> Results.NotFound({| error = "Player was not found." |})
            )
        )
        |> ignore

        app.MapPost(
            "/api/scores",
            Func<ScoreSubmissionRequest, BrainGamesStore, IResult>(fun request store ->
                match store.SubmitScore(request) with
                | Ok result -> Results.Ok(result)
                | Error message -> Results.BadRequest({| error = message |})
            )
        )
        |> ignore
