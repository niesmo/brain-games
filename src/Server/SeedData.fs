namespace BrainGames.Server

open System
open BrainGames.Shared

type SeedPlayer =
    { DisplayName: string
      Email: string
      CountryCode: string
      FavoriteCategory: string
      Score: int
      Moves: int
      DurationSeconds: int
      CompletedAtUtc: DateTime }

[<RequireQualifiedAccess>]
module SeedData =
    let games =
        [| { Id = "memory-match"
             Name = "Memory Match Sprint"
             Category = "Memory"
             Description = "Flip, remember, and clear the board before your focus fades."
             Difficulty = "Easy"
             LessonsCount = 3 }
           { Id = "pattern-recall"
             Name = "Pattern Recall Rush"
             Category = "Memory"
             Description = "Memorize the lit cells, then clear as many evolving grids as you can in sixty seconds."
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
             Takeaway = "Intentional play beats frantic play." }
           { Id = "recall-lesson-1"
             GameId = "pattern-recall"
             Locale = "en"
             Title = "Map the corners first"
             Summary = "Anchor the outer edge of the grid before you memorize the center."
             Takeaway = "Strong anchors make the pattern easier to replay." }
           { Id = "recall-lesson-2"
             GameId = "pattern-recall"
             Locale = "en"
             Title = "Count before you click"
             Summary = "Know how many lit cells you expect before the grid goes dark."
             Takeaway = "A target count reduces panic taps." }
           { Id = "recall-lesson-3"
             GameId = "pattern-recall"
             Locale = "en"
             Title = "Reset after a miss"
             Summary = "A missed grid should not speed up your next one. Re-center during the new countdown."
             Takeaway = "Fast recovery preserves the long run." } |]

    let players now =
        [| { DisplayName = "Maya"
             Email = "maya@example.com"
             CountryCode = "US"
             FavoriteCategory = "Memory"
             Score = 2110
             Moves = 14
             DurationSeconds = 49
             CompletedAtUtc = (now: DateTime).AddHours(-8.0) }
           { DisplayName = "Tariq"
             Email = "tariq@example.com"
             CountryCode = "NG"
             FavoriteCategory = "Memory"
             Score = 1980
             Moves = 15
             DurationSeconds = 57
             CompletedAtUtc = (now: DateTime).AddHours(-18.0) }
           { DisplayName = "Lucia"
             Email = "lucia@example.com"
             CountryCode = "BR"
             FavoriteCategory = "Memory"
             Score = 1845
             Moves = 16
             DurationSeconds = 64
             CompletedAtUtc = (now: DateTime).AddHours(-30.0) } |]
