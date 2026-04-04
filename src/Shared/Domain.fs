namespace BrainGames.Shared

open System

type GameSummary =
    { Id: string
      Name: string
      Category: string
      Description: string
      Difficulty: string
      LessonsCount: int }

type LessonSummary =
    { Id: string
      GameId: string
      Locale: string
      Title: string
      Summary: string
      Takeaway: string }

type PlayerProfile =
    { PlayerId: string
      DisplayName: string
      Email: string
      CountryCode: string
      JoinedAtUtc: DateTime
      FavoriteCategory: string }

type RegisterPlayerRequest =
    { DisplayName: string
      Email: string
      CountryCode: string }

type RegisterPlayerResponse =
    { Player: PlayerProfile
      AuthMode: string
      Message: string }

type ScoreSubmissionRequest =
    { PlayerId: string
      GameId: string
      Difficulty: string
      Moves: int
      DurationSeconds: int
      MatchedPairs: int
      MaxPairs: int
      SessionSeed: int
      CompletedAtUtc: DateTime }

type ScoreSubmissionResult =
    { AttemptId: string
      Score: int
      Rank: int
      PersonalBest: bool
      StreakDays: int
      Message: string }

type LeaderboardEntry =
    { Rank: int
      PlayerId: string
      DisplayName: string
      Score: int
      Moves: int
      DurationSeconds: int
      CompletedAtUtc: DateTime }

type ProfileSnapshot =
    { Player: PlayerProfile
      BestScores: LeaderboardEntry array
      StreakDays: int
      TotalGamesPlayed: int
      LastPlayedUtc: DateTime
      FavoriteCategory: string }

type BootResponse =
    { AppName: string
      Tagline: string
      AuthMode: string
      Games: GameSummary array
      Lessons: LessonSummary array }

[<RequireQualifiedAccess>]
module ScoreRules =
    let private maxSeconds (scoreRequest: ScoreSubmissionRequest) =
        max 15 scoreRequest.DurationSeconds

    let validateSubmission (scoreRequest: ScoreSubmissionRequest) =
        if String.IsNullOrWhiteSpace scoreRequest.PlayerId then
            Error "A player id is required."
        elif String.IsNullOrWhiteSpace scoreRequest.GameId then
            Error "A game id is required."
        elif scoreRequest.Moves < scoreRequest.MaxPairs then
            Error "Moves cannot be lower than the number of pairs."
        elif scoreRequest.MaxPairs <= 0 then
            Error "Max pairs must be greater than zero."
        elif scoreRequest.MatchedPairs <> scoreRequest.MaxPairs then
            Error "Only completed memory runs can be submitted to the leaderboard."
        elif scoreRequest.DurationSeconds <= 0 then
            Error "Duration must be greater than zero."
        else
            Ok scoreRequest

    let calculateScore (scoreRequest: ScoreSubmissionRequest) =
        let basePoints = scoreRequest.MaxPairs * 120
        let speedBonus = max 0 (900 - (maxSeconds scoreRequest * 9))
        let efficiencyBonus = max 0 ((scoreRequest.MaxPairs * 12) - ((scoreRequest.Moves - scoreRequest.MaxPairs) * 6))
        let difficultyBonus =
            match scoreRequest.Difficulty.Trim().ToLowerInvariant() with
            | "hard" -> 250
            | "medium" -> 125
            | _ -> 0

        basePoints + speedBonus + efficiencyBonus + difficultyBonus
