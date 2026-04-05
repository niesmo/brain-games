namespace BrainGames.Server

open System
open System.Collections.Concurrent
open BrainGames.Shared

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

    let normalizeEmail (email: string) = email.Trim().ToLowerInvariant()

    let addSeedPlayer (seedPlayer: SeedPlayer) =
        let playerId = Guid.NewGuid().ToString("N")

        let player =
            { PlayerId = playerId
              DisplayName = seedPlayer.DisplayName
              Email = seedPlayer.Email
              CountryCode = seedPlayer.CountryCode
              JoinedAtUtc = seedPlayer.CompletedAtUtc.AddDays(-14.0)
              FavoriteCategory = seedPlayer.FavoriteCategory }

        players[playerId] <- player

        let seededAttempt =
            { AttemptId = Guid.NewGuid().ToString("N")
              PlayerId = playerId
              PlayerName = seedPlayer.DisplayName
              GameId = "memory-match"
              Score = seedPlayer.Score
              Moves = seedPlayer.Moves
              DurationSeconds = seedPlayer.DurationSeconds
              CompletedAtUtc = seedPlayer.CompletedAtUtc }

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
        SeedData.players now |> Array.iter addSeedPlayer

    member _.AuthMode =
        if supabase.Enabled && not (String.IsNullOrWhiteSpace supabase.ProjectUrl) then
            "supabase"
        else
            "local-dev"

    member this.Boot() =
        { AppName = appSettings.Name
          Tagline = appSettings.Tagline
          AuthMode = this.AuthMode
          Games = SeedData.games
          Lessons = SeedData.lessons }

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
                    if String.IsNullOrWhiteSpace request.CountryCode then
                        "US"
                    else
                        request.CountryCode.Trim().ToUpperInvariant()
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

            let lastPlayed =
                orderedRuns
                |> Array.tryHead
                |> Option.map (fun attempt -> attempt.CompletedAtUtc)
                |> Option.defaultValue player.JoinedAtUtc

            let streakDays =
                orderedRuns
                |> Array.map (fun attempt -> attempt.CompletedAtUtc.Date)
                |> Array.distinct
                |> Array.sortDescending
                |> Array.fold
                    (fun state currentDay ->
                        match state with
                        | None -> Some(1, currentDay)
                        | Some(count, previousDay) when previousDay.AddDays(-1.0) = currentDay -> Some(count + 1, currentDay)
                        | Some(count, _) -> Some(count, currentDay))
                    None
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
