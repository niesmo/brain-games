open System
open System.Collections.Concurrent
open BrainGames.Shared
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

type AppSettings =
    { Name: string
      Tagline: string }

type SupabaseSettings =
    { Enabled: bool
      ProjectUrl: string
      AnonKey: string
      ServiceRoleKey: string
      StorageBucket: string }

type Attempt =
    { AttemptId: string
      PlayerId: string
      PlayerName: string
      GameId: string
      Score: int
      Moves: int
      DurationSeconds: int
      CompletedAtUtc: DateTime }

type BrainGamesStore(appSettings: AppSettings, supabase: SupabaseSettings) =
    let players = ConcurrentDictionary<string, PlayerProfile>()
    let attempts = ConcurrentDictionary<string, ResizeArray<Attempt>>()
    let playerAttempts = ConcurrentDictionary<string, ResizeArray<Attempt>>()

    let games =
        [| { Id = "memory-match"
             Name = "Memory Match Sprint"
             Category = "Memory"
             Description = "Flip, remember, and clear the board before your focus fades."
             Difficulty = "Easy"
             LessonsCount = 3 } |]

    let lessons =
        [| { Id = "memory-lesson-1"
             GameId = "memory-match"
             Locale = "en"
             Title = "Chunk the board"
             Summary = "Divide the grid into rows and solve one zone at a time."
             Takeaway = "Consistent scanning reduces wasted flips." }
           { Id = "memory-lesson-2"
             GameId = "memory-match"
             Locale = "en"
             Title = "Say the pattern silently"
             Summary = "Use a short mental label for each card pair you discover."
             Takeaway = "Verbal coding strengthens recall." }
           { Id = "memory-lesson-3"
             GameId = "memory-match"
             Locale = "en"
             Title = "Pause before the second flip"
             Summary = "A half-second pause is usually faster than random guessing."
             Takeaway = "Intentional play beats frantic play." } |]

    let normalizeEmail (email: string) = email.Trim().ToLowerInvariant()

    let addSeedPlayer
        (displayName: string)
        (email: string)
        (countryCode: string)
        (favoriteCategory: string)
        (score: int)
        (moves: int)
        (seconds: int)
        (completedAtUtc: DateTime) =
        let playerId = Guid.NewGuid().ToString("N")

        let player =
            { PlayerId = playerId
              DisplayName = displayName
              Email = email
              CountryCode = countryCode
              JoinedAtUtc = completedAtUtc.AddDays(-14.0)
              FavoriteCategory = favoriteCategory }

        players[playerId] <- player

        let seededAttempt =
            { AttemptId = Guid.NewGuid().ToString("N")
              PlayerId = playerId
              PlayerName = displayName
              GameId = "memory-match"
              Score = score
              Moves = moves
              DurationSeconds = seconds
              CompletedAtUtc = completedAtUtc }

        attempts.AddOrUpdate(
            "memory-match",
            (fun _ -> ResizeArray([ seededAttempt ])),
            (fun _ existing ->
                existing.Add seededAttempt
                existing)
        )
        |> ignore

        playerAttempts.AddOrUpdate(
            playerId,
            (fun _ -> ResizeArray([ seededAttempt ])),
            (fun _ existing ->
                existing.Add seededAttempt
                existing)
        )
        |> ignore

    do
        let now = DateTime.UtcNow
        addSeedPlayer "Maya" "maya@example.com" "US" "Memory" 2110 14 49 (now.AddHours(-8.0))
        addSeedPlayer "Tariq" "tariq@example.com" "NG" "Memory" 1980 15 57 (now.AddHours(-18.0))
        addSeedPlayer "Lucia" "lucia@example.com" "BR" "Memory" 1845 16 64 (now.AddHours(-30.0))

    member _.AuthMode =
        if supabase.Enabled && not (String.IsNullOrWhiteSpace supabase.ProjectUrl) then
            "supabase"
        else
            "local-dev"

    member this.Boot() =
        { AppName = appSettings.Name
          Tagline = appSettings.Tagline
          AuthMode = this.AuthMode
          Games = games
          Lessons = lessons }

    member this.RegisterPlayer(request: RegisterPlayerRequest) =
        let email = normalizeEmail request.Email

        let existingPlayer =
            players.Values
            |> Seq.tryFind (fun player -> normalizeEmail player.Email = email)

        match existingPlayer with
        | Some player ->
            { Player = player
              AuthMode = this.AuthMode
              Message = "Welcome back. Your local dev profile is ready." }
        | None ->
            let player =
                { PlayerId = Guid.NewGuid().ToString("N")
                  DisplayName = request.DisplayName.Trim()
                  Email = email
                  CountryCode =
                    if String.IsNullOrWhiteSpace request.CountryCode then "US" else request.CountryCode.Trim().ToUpperInvariant()
                  JoinedAtUtc = DateTime.UtcNow
                  FavoriteCategory = "Memory" }

            players[player.PlayerId] <- player

            { Player = player
              AuthMode = this.AuthMode
              Message =
                if this.AuthMode = "supabase" then
                    "Profile created. Wire this request to Supabase auth next."
                else
                    "Local dev profile created. Configure Supabase keys to switch auth modes." }

    member _.Leaderboard(gameId: string) =
        let entries =
            match attempts.TryGetValue gameId with
            | true, values -> values |> Seq.toArray
            | _ -> [||]

        entries
        |> Array.sortBy (fun attempt -> -attempt.Score, attempt.DurationSeconds, attempt.Moves, attempt.CompletedAtUtc)
        |> Array.mapi (fun index attempt ->
            { Rank = index + 1
              PlayerId = attempt.PlayerId
              DisplayName = attempt.PlayerName
              Score = attempt.Score
              Moves = attempt.Moves
              DurationSeconds = attempt.DurationSeconds
              CompletedAtUtc = attempt.CompletedAtUtc })

    member this.Profile(playerId: string) =
        match players.TryGetValue playerId with
        | true, player ->
            let playerGames =
                match playerAttempts.TryGetValue playerId with
                | true, values -> values |> Seq.toArray
                | _ -> [||]

            let bestScores =
                playerGames
                |> Array.groupBy (fun attempt -> attempt.GameId)
                |> Array.choose (fun (_, runs) ->
                    runs
                    |> Array.sortBy (fun attempt -> -attempt.Score, attempt.DurationSeconds, attempt.Moves)
                    |> Array.tryHead
                    |> Option.map (fun best ->
                        { Rank = 1
                          PlayerId = best.PlayerId
                          DisplayName = best.PlayerName
                          Score = best.Score
                          Moves = best.Moves
                          DurationSeconds = best.DurationSeconds
                          CompletedAtUtc = best.CompletedAtUtc }))

            let orderedRuns = playerGames |> Array.sortByDescending (fun attempt -> attempt.CompletedAtUtc)
            let lastPlayed = orderedRuns |> Array.tryHead |> Option.map (fun attempt -> attempt.CompletedAtUtc) |> Option.defaultValue player.JoinedAtUtc

            let streakDays =
                orderedRuns
                |> Array.map (fun attempt -> attempt.CompletedAtUtc.Date)
                |> Array.distinct
                |> Array.sortDescending
                |> Array.fold (fun state currentDay ->
                    match state with
                    | None -> Some(1, currentDay)
                    | Some(count, previousDay) when previousDay.AddDays(-1.0) = currentDay -> Some(count + 1, currentDay)
                    | Some(count, _) -> Some(count, currentDay)) None
                |> Option.map fst
                |> Option.defaultValue 0

            Some
                { Player = player
                  BestScores = bestScores
                  StreakDays = streakDays
                  TotalGamesPlayed = playerGames.Length
                  LastPlayedUtc = lastPlayed
                  FavoriteCategory = player.FavoriteCategory }
        | _ -> None

    member this.SubmitScore(request: ScoreSubmissionRequest) =
        match ScoreRules.validateSubmission request with
        | Error message -> Error message
        | Ok validRequest ->
            match players.TryGetValue validRequest.PlayerId with
            | false, _ -> Error "Unknown player. Register before posting scores."
            | true, player ->
                let score = ScoreRules.calculateScore validRequest

                let attempt =
                    { AttemptId = Guid.NewGuid().ToString("N")
                      PlayerId = player.PlayerId
                      PlayerName = player.DisplayName
                      GameId = validRequest.GameId
                      Score = score
                      Moves = validRequest.Moves
                      DurationSeconds = validRequest.DurationSeconds
                      CompletedAtUtc = validRequest.CompletedAtUtc }

                attempts.AddOrUpdate(
                    validRequest.GameId,
                    (fun _ -> ResizeArray([ attempt ])),
                    (fun _ existing ->
                        existing.Add attempt
                        existing)
                )
                |> ignore

                let playerRuns =
                    playerAttempts.AddOrUpdate(
                        player.PlayerId,
                        (fun _ -> ResizeArray([ attempt ])),
                        (fun _ existing ->
                            existing.Add attempt
                            existing)
                    )

                let orderedLeaderboard = this.Leaderboard(validRequest.GameId)

                let rank =
                    orderedLeaderboard
                    |> Array.tryFindIndex (fun entry ->
                        entry.PlayerId = player.PlayerId
                        && entry.Score = score
                        && entry.CompletedAtUtc = validRequest.CompletedAtUtc)
                    |> Option.map ((+) 1)
                    |> Option.defaultValue orderedLeaderboard.Length

                let previousBest =
                    playerRuns
                    |> Seq.filter (fun run -> run.AttemptId <> attempt.AttemptId)
                    |> Seq.map (fun run -> run.Score)
                    |> Seq.sortDescending
                    |> Seq.tryHead

                let streakDays =
                    match this.Profile player.PlayerId with
                    | Some profile -> profile.StreakDays
                    | None -> 1

                Ok
                    { AttemptId = attempt.AttemptId
                      Score = score
                      Rank = rank
                      PersonalBest = previousBest |> Option.map (fun best -> score > best) |> Option.defaultValue true
                      StreakDays = streakDays
                      Message = "Score accepted and leaderboard updated." }

let builder = WebApplication.CreateBuilder()

builder.Services.AddCors(fun options ->
    options.AddDefaultPolicy(fun policy ->
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore))
|> ignore

let appConfig = builder.Configuration.GetSection("App")
let supabaseConfig = builder.Configuration.GetSection("Supabase")

let appSettings =
    { Name = appConfig["Name"] |> Option.ofObj |> Option.defaultValue "Brain Games"
      Tagline = appConfig["Tagline"] |> Option.ofObj |> Option.defaultValue "Playful practice for sharper thinking." }

let supabaseSettings =
    { Enabled = supabaseConfig["Enabled"] |> Option.ofObj |> Option.map Boolean.Parse |> Option.defaultValue false
      ProjectUrl = supabaseConfig["ProjectUrl"] |> Option.ofObj |> Option.defaultValue ""
      AnonKey = supabaseConfig["AnonKey"] |> Option.ofObj |> Option.defaultValue ""
      ServiceRoleKey = supabaseConfig["ServiceRoleKey"] |> Option.ofObj |> Option.defaultValue ""
      StorageBucket = supabaseConfig["StorageBucket"] |> Option.ofObj |> Option.defaultValue "lesson-assets" }

builder.Services.AddSingleton(BrainGamesStore(appSettings, supabaseSettings))
|> ignore

let app = builder.Build()

app.UseCors()
|> ignore
app.UseHttpsRedirection()
|> ignore

app.MapGet("/api/health", Func<IResult>(fun () -> Results.Ok({| status = "ok"; utc = DateTime.UtcNow |}))) |> ignore
app.MapGet("/api/boot", Func<BrainGamesStore, BootResponse>(fun store -> store.Boot())) |> ignore

app.MapPost(
    "/api/players/register",
    Func<RegisterPlayerRequest, BrainGamesStore, IResult>(fun request store ->
        if String.IsNullOrWhiteSpace request.DisplayName || String.IsNullOrWhiteSpace request.Email then
            Results.BadRequest({| error = "Display name and email are required." |})
        else
            store.RegisterPlayer(request) |> Results.Ok)
) |> ignore

app.MapGet(
    "/api/leaderboard/{gameId}",
    Func<string, BrainGamesStore, LeaderboardEntry array>(fun gameId store -> store.Leaderboard(gameId))
) |> ignore

app.MapGet(
    "/api/profile/{playerId}",
    Func<string, BrainGamesStore, IResult>(fun playerId store ->
        match store.Profile(playerId) with
        | Some profile -> Results.Ok(profile)
        | None -> Results.NotFound({| error = "Player was not found." |}))
) |> ignore

app.MapPost(
    "/api/scores",
    Func<ScoreSubmissionRequest, BrainGamesStore, IResult>(fun request store ->
        match store.SubmitScore(request) with
        | Ok result -> Results.Ok(result)
        | Error message -> Results.BadRequest({| error = message |}))
) |> ignore

app.Run()
