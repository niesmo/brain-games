module BrainGames.Server.Tests.ScoreRulesTests

open System
open BrainGames.Shared
open Xunit

let private completedRequest difficulty moves seconds =
    { PlayerId = "player-1"
      GameId = "memory-match"
      Difficulty = difficulty
      Moves = moves
      DurationSeconds = seconds
      MatchedPairs = 6
      MaxPairs = 6
      SessionSeed = 42
      CompletedAtUtc = DateTime.UtcNow }

[<Fact>]
let ``completed hard runs earn more score than easy runs`` () =
    let easyScore = completedRequest "easy" 12 58 |> ScoreRules.calculateScore
    let hardScore = completedRequest "hard" 12 58 |> ScoreRules.calculateScore

    Assert.True(hardScore > easyScore)

[<Fact>]
let ``validation rejects incomplete boards`` () =
    let invalidRequest =
        { completedRequest "easy" 14 70 with
            MatchedPairs = 4 }

    match ScoreRules.validateSubmission invalidRequest with
    | Ok _ -> failwith "Expected validation to fail for incomplete boards."
    | Error message -> Assert.Contains("completed memory runs", message)

[<Fact>]
let ``faster and cleaner runs score higher`` () =
    let efficient = completedRequest "medium" 12 45 |> ScoreRules.calculateScore
    let inefficient = completedRequest "medium" 18 75 |> ScoreRules.calculateScore

    Assert.True(efficient > inefficient)
