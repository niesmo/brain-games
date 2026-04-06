module BrainGames.Client.State

open System
open BrainGames.Shared
open BrainGames.Client.Api

type Page =
    | Play
    | Leaderboard
    | Progress

type Card =
    { Index: int
      Value: string
      Revealed: bool
      Matched: bool }

type MemoryMatchState =
    { Cards: Card list
      FirstSelection: int option
      Moves: int
      MatchedPairs: int
      Status: string
      SessionName: string
      BestRun: int option
      SessionStartedAtUtc: DateTime
      SessionSeed: int
      Difficulty: string }

type RecallTile =
    { Index: int
      IsTarget: bool
      IsCorrect: bool
      IsWrong: bool }

type RecallPhase =
    | Ready
    | Memorize
    | Input
    | Finished

type PatternRecallState =
    { Tiles: RecallTile list
      GridSize: int
      TargetCount: int
      Countdown: int
      RevealCountdown: int
      Phase: RecallPhase
      RoundsCleared: int
      CurrentRound: int
      Hits: int
      Misses: int
      TimeLimitSeconds: int
      SessionStartedAtUtc: DateTime
      SessionSeed: int
      Difficulty: string
      Status: string
      BestRounds: int option }

type Cmd<'msg> = (('msg -> unit) -> unit) list

[<RequireQualifiedAccess>]
module Cmd =
    let none: Cmd<'msg> = []

    let batch (commands: Cmd<'msg> list) : Cmd<'msg> = List.concat commands

    let ofAsync (operation: Async<'msg>) : Cmd<'msg> =
        [ fun dispatch ->
              Async.StartImmediate(
                  async {
                      let! message = operation
                      dispatch message
                  }
              ) ]

    let after (delayMs: int) message =
        ofAsync (
            async {
                do! Async.Sleep delayMs
                return message
            }
        )

    let run dispatch (commands: Cmd<'msg>) =
        commands
        |> List.iter (fun command -> command dispatch)

type Model =
    { CurrentPage: Page
      SelectedGameId: string
      MemoryMatch: MemoryMatchState
      PatternRecall: PatternRecallState
      Boot: BootResponse option
      Leaderboard: LeaderboardEntry array
      Player: PlayerProfile option
      Profile: ProfileSnapshot option
      LastScore: ScoreSubmissionResult option
      ServerMessage: string option
      LastError: string option
      IsBootLoading: bool
      IsLeaderboardLoading: bool
      IsProfileLoading: bool
      IsRegistering: bool
      IsSubmittingScore: bool }

type Msg =
    | BootLoaded of Result<BootResponse, string>
    | LeaderboardLoaded of Result<LeaderboardEntry array, string>
    | PlayerRegistered of Result<RegisterPlayerResponse, string>
    | ProfileLoaded of Result<ProfileSnapshot, string>
    | ScoreSubmitted of Result<ScoreSubmissionResult, string>
    | GoToPlay
    | GoToLeaderboard
    | GoToProgress
    | SelectGame of string
    | NewGame
    | FlipCard of int
    | RecallCellClicked of int
    | RecallReadyTick of sessionSeed: int * roundNumber: int * remaining: int
    | RecallRevealTick of sessionSeed: int * roundNumber: int * remaining: int
    | RecallAdvanceRound of sessionSeed: int * nextRound: int
    | RecallSessionExpired of sessionSeed: int

let private values = [ "A"; "B"; "C"; "D"; "E"; "F" ]
let private memoryMatchId = "memory-match"
let private patternRecallId = "pattern-recall"

let private rng = Random()

let private sessionNames =
    [| "Focus Sprint"
       "Recall Run"
       "Pattern Push"
       "Sharpness Loop" |]

let private recallSessionNames =
    [| "Flash Grid"
       "Signal Chase"
       "Recall Rush"
       "Pattern Ladder" |]

let private shuffle seed items =
    let random = Random(seed)
    items |> List.sortBy (fun _ -> random.Next())

let private pickSessionName (names: string array) = names[rng.Next(names.Length)]

let private createSessionSeed () = rng.Next(100000, 999999)

let private createDeck sessionSeed =
    values
    |> List.collect (fun value -> [ value; value ])
    |> shuffle sessionSeed
    |> List.mapi (fun index value ->
        { Index = index
          Value = value
          Revealed = false
          Matched = false })

let private freshMemoryBoard () =
    let sessionSeed = createSessionSeed ()

    { Cards = createDeck sessionSeed
      FirstSelection = None
      Moves = 0
      MatchedPairs = 0
      Status = "Flip two cards at a time. Match every pair with as few moves as possible."
      SessionName = pickSessionName sessionNames
      BestRun = None
      SessionStartedAtUtc = DateTime.UtcNow
      SessionSeed = sessionSeed
      Difficulty = "easy" }

let private recallConfig roundNumber =
    match roundNumber with
    | round when round <= 2 -> 3, 3, "easy"
    | round when round <= 4 -> 4, 4, "easy"
    | round when round <= 6 -> 4, 5, "medium"
    | round when round <= 8 -> 5, 6, "medium"
    | round ->
        let targets = min 10 (6 + ((round - 8) / 2))
        5, targets, "hard"

let private createRecallTiles sessionSeed roundNumber =
    let gridSize, targetCount, difficulty = recallConfig roundNumber
    let totalTiles = gridSize * gridSize

    let targets =
        [ 0 .. totalTiles - 1 ]
        |> shuffle (sessionSeed + (roundNumber * 37))
        |> List.truncate targetCount
        |> Set.ofList

    gridSize,
    targetCount,
    difficulty,
    [ 0 .. totalTiles - 1 ]
    |> List.map (fun index ->
        { Index = index
          IsTarget = targets.Contains index
          IsCorrect = false
          IsWrong = false })

let private freshPatternRecall roundNumber bestRounds =
    let sessionSeed = createSessionSeed ()

    let gridSize, targetCount, difficulty, tiles =
        createRecallTiles sessionSeed roundNumber

    { Tiles = tiles
      GridSize = gridSize
      TargetCount = targetCount
      Countdown = 3
      RevealCountdown = 0
      Phase = Ready
      RoundsCleared = 0
      CurrentRound = roundNumber
      Hits = 0
      Misses = 0
      TimeLimitSeconds = 60
      SessionStartedAtUtc = DateTime.UtcNow
      SessionSeed = sessionSeed
      Difficulty = difficulty
      Status = "Get ready. The pattern will flash after the countdown."
      BestRounds = bestRounds }

let private initialModel () =
    { CurrentPage = Play
      SelectedGameId = memoryMatchId
      MemoryMatch = freshMemoryBoard ()
      PatternRecall = freshPatternRecall 1 None
      Boot = None
      Leaderboard = [||]
      Player = None
      Profile = None
      LastScore = None
      ServerMessage = None
      LastError = None
      IsBootLoading = true
      IsLeaderboardLoading = true
      IsProfileLoading = true
      IsRegistering = true
      IsSubmittingScore = false }

let init () =
    let registration = ensureGuestRegistration ()

    let commands =
        Cmd.batch [ Cmd.ofAsync (
                        async {
                            let! result = loadBoot ()
                            return BootLoaded result
                        }
                    )
                    Cmd.ofAsync (
                        async {
                            let! result = loadLeaderboard memoryMatchId
                            return LeaderboardLoaded result
                        }
                    )
                    Cmd.ofAsync (
                        async {
                            let! result = registerPlayer registration
                            return PlayerRegistered result
                        }
                    ) ]

    initialModel (), commands

let selectedGame model = model.SelectedGameId

let completionPercent model =
    (float model.MemoryMatch.MatchedPairs
     / float values.Length)
    * 100.0
    |> int

let focusScore model =
    match selectedGame model with
    | id when id = patternRecallId -> max 0 (100 - (model.PatternRecall.Misses * 10))
    | _ ->
        max
            0
            (100
             - ((model.MemoryMatch.Moves
                 - model.MemoryMatch.MatchedPairs)
                * 8))

let activeDifficulty model =
    match selectedGame model with
    | id when id = patternRecallId -> model.PatternRecall.Difficulty
    | _ -> model.MemoryMatch.Difficulty

let currentSessionStartedAtUtc model =
    match selectedGame model with
    | id when id = patternRecallId -> model.PatternRecall.SessionStartedAtUtc
    | _ -> model.MemoryMatch.SessionStartedAtUtc

let currentSessionName model =
    match selectedGame model with
    | id when id = patternRecallId -> "Pattern Recall Rush"
    | _ -> model.MemoryMatch.SessionName

let tryFindCard index (cards: Card list) =
    cards
    |> List.tryFind (fun card -> card.Index = index)

let revealCard index (cards: Card list) =
    cards
    |> List.map (fun current ->
        if current.Index = index then
            { current with Revealed = true }
        else
            current)

let markPair first second (cards: Card list) =
    cards
    |> List.map (fun current ->
        if current.Index = first || current.Index = second then
            { current with Matched = true }
        else
            current)

let hidePair first second (cards: Card list) =
    cards
    |> List.map (fun current ->
        if current.Index = first || current.Index = second then
            { current with Revealed = false }
        else
            current)

let private profileRefresh playerId =
    Cmd.ofAsync (
        async {
            let! result = loadProfile playerId
            return ProfileLoaded result
        }
    )

let private loadLeaderboardCommand gameId =
    Cmd.ofAsync (
        async {
            let! result = loadLeaderboard gameId
            return LeaderboardLoaded result
        }
    )

let private recallReadyTick sessionSeed roundNumber remaining =
    Cmd.after 1000 (RecallReadyTick(sessionSeed, roundNumber, remaining))

let private recallRevealTick sessionSeed roundNumber remaining =
    Cmd.after 1000 (RecallRevealTick(sessionSeed, roundNumber, remaining))

let private recallAdvanceRound sessionSeed nextRound =
    Cmd.after 700 (RecallAdvanceRound(sessionSeed, nextRound))

let private recallExpireSession sessionSeed =
    Cmd.after 60000 (RecallSessionExpired sessionSeed)

let private queueRecallSession recall =
    Cmd.batch [ recallReadyTick recall.SessionSeed recall.CurrentRound 2
                recallExpireSession recall.SessionSeed ]

let private queueRecallRound recall =
    recallReadyTick recall.SessionSeed recall.CurrentRound 2

let private patternRecallRound sessionSeed roundNumber existingState =
    let gridSize, targetCount, difficulty, tiles =
        createRecallTiles sessionSeed roundNumber

    { existingState with
        Tiles = tiles
        GridSize = gridSize
        TargetCount = targetCount
        Countdown = 3
        RevealCountdown = 0
        Phase = Ready
        CurrentRound = roundNumber
        Difficulty = difficulty
        Status = "New grid incoming. Lock in the pattern before it disappears." }

let private leaderboardRefresh model =
    loadLeaderboardCommand model.SelectedGameId

let private submitMemoryScore model (player: PlayerProfile) =
    let memory = model.MemoryMatch

    let durationSeconds =
        max
            1
            (int
                (DateTime.UtcNow - memory.SessionStartedAtUtc)
                    .TotalSeconds)

    let request =
        { PlayerId = player.PlayerId
          GameId = memoryMatchId
          Difficulty = memory.Difficulty
          Moves = memory.Moves
          DurationSeconds = durationSeconds
          MatchedPairs = memory.MatchedPairs
          MaxPairs = values.Length
          SessionSeed = memory.SessionSeed
          CompletedAtUtc = DateTime.UtcNow }

    Cmd.ofAsync (
        async {
            let! result = submitScore request
            return ScoreSubmitted result
        }
    )

let private submitRecallScore model (player: PlayerProfile) =
    let recall = model.PatternRecall

    let durationSeconds =
        max
            1
            (int
                (DateTime.UtcNow - recall.SessionStartedAtUtc)
                    .TotalSeconds)

    let completedRounds = max 1 recall.RoundsCleared

    let request =
        { PlayerId = player.PlayerId
          GameId = patternRecallId
          Difficulty = recall.Difficulty
          Moves = max completedRounds (recall.Hits + recall.Misses)
          DurationSeconds = durationSeconds
          MatchedPairs = completedRounds
          MaxPairs = completedRounds
          SessionSeed = recall.SessionSeed
          CompletedAtUtc = DateTime.UtcNow }

    Cmd.ofAsync (
        async {
            let! result = submitScore request
            return ScoreSubmitted result
        }
    )

let private switchGame model gameId =
    if gameId = patternRecallId then
        let recall = freshPatternRecall 1 model.PatternRecall.BestRounds

        { model with
            SelectedGameId = patternRecallId
            CurrentPage = Play
            PatternRecall = recall
            LastScore = None
            IsLeaderboardLoading = true
            ServerMessage =
                Some
                    "Pattern Recall Rush loaded. Memorize the flash and clear as many grids as you can in sixty seconds." },
        Cmd.batch [ loadLeaderboardCommand patternRecallId
                    queueRecallSession recall ]
    else
        let memory = freshMemoryBoard ()

        { model with
            SelectedGameId = memoryMatchId
            CurrentPage = Play
            MemoryMatch = memory
            LastScore = None
            IsLeaderboardLoading = true
            ServerMessage = Some "Memory Match Sprint loaded. Clear the board with as few moves as possible." },
        loadLeaderboardCommand memoryMatchId

let private applyRecallSelection index state =
    match state.Tiles
          |> List.tryFind (fun tile -> tile.Index = index)
        with
    | None -> state, false, false
    | Some tile when tile.IsCorrect || tile.IsWrong -> state, false, false
    | Some tile ->
        let updatedTiles =
            state.Tiles
            |> List.map (fun current ->
                if current.Index = index then
                    if current.IsTarget then
                        { current with IsCorrect = true }
                    else
                        { current with IsWrong = true }
                else
                    current)

        let nextState =
            if tile.IsTarget then
                { state with
                    Tiles = updatedTiles
                    Hits = state.Hits + 1
                    Status = "Pattern confirmed. Keep clearing the lit cells." }
            else
                { state with
                    Tiles = updatedTiles
                    Misses = state.Misses + 1
                    Status = "Missed tile. A new grid is loading." }

        let clearedRound =
            tile.IsTarget
            && (updatedTiles
                |> List.filter (fun current -> current.IsTarget)
                |> List.forall (fun current -> current.IsCorrect))

        nextState, clearedRound, not tile.IsTarget

let update msg model =
    match msg with
    | BootLoaded result ->
        match result with
        | Ok boot ->
            { model with
                Boot = Some boot
                IsBootLoading = false
                LastError = None },
            Cmd.none
        | Error error ->
            { model with
                IsBootLoading = false
                LastError = Some error },
            Cmd.none
    | LeaderboardLoaded result ->
        match result with
        | Ok leaderboard ->
            { model with
                Leaderboard = leaderboard
                IsLeaderboardLoading = false
                LastError = None },
            Cmd.none
        | Error error ->
            { model with
                IsLeaderboardLoading = false
                LastError = Some error },
            Cmd.none
    | PlayerRegistered result ->
        match result with
        | Ok registration ->
            { model with
                Player = Some registration.Player
                IsRegistering = false
                ServerMessage = Some registration.Message
                LastError = None },
            profileRefresh registration.Player.PlayerId
        | Error error ->
            { model with
                IsRegistering = false
                IsProfileLoading = false
                LastError = Some error },
            Cmd.none
    | ProfileLoaded result ->
        match result with
        | Ok profile ->
            { model with
                Profile = Some profile
                IsProfileLoading = false
                LastError = None },
            Cmd.none
        | Error error ->
            { model with
                Profile = None
                IsProfileLoading = false
                LastError = Some error },
            Cmd.none
    | ScoreSubmitted result ->
        match result with
        | Ok score ->
            let commands =
                match model.Player with
                | Some player ->
                    Cmd.batch [ leaderboardRefresh model
                                profileRefresh player.PlayerId ]
                | None -> leaderboardRefresh model

            { model with
                LastScore = Some score
                ServerMessage = Some score.Message
                LastError = None
                IsSubmittingScore = false
                CurrentPage = Play },
            commands
        | Error error ->
            { model with
                LastError = Some error
                IsSubmittingScore = false },
            Cmd.none
    | GoToPlay -> { model with CurrentPage = Play }, Cmd.none
    | GoToLeaderboard -> { model with CurrentPage = Leaderboard }, Cmd.none
    | GoToProgress -> { model with CurrentPage = Progress }, Cmd.none
    | SelectGame gameId when gameId = model.SelectedGameId -> model, Cmd.none
    | SelectGame gameId -> switchGame model gameId
    | NewGame ->
        match model.SelectedGameId with
        | id when id = patternRecallId ->
            let recall = freshPatternRecall 1 model.PatternRecall.BestRounds

            { model with
                CurrentPage = Play
                PatternRecall = recall
                LastScore = None
                IsSubmittingScore = false
                ServerMessage = Some "New recall run started. Use the countdown to get set." },
            queueRecallSession recall
        | _ ->
            let memory = freshMemoryBoard ()

            { model with
                CurrentPage = Play
                MemoryMatch = memory
                LastScore = None
                IsSubmittingScore = false
                ServerMessage = Some "Fresh board ready. Build a clean run." },
            Cmd.none
    | FlipCard index when model.SelectedGameId <> memoryMatchId -> model, Cmd.none
    | FlipCard index ->
        let memory = model.MemoryMatch

        match tryFindCard index memory.Cards with
        | Some card when not card.Matched && not card.Revealed ->
            match memory.FirstSelection with
            | None ->
                { model with
                    MemoryMatch =
                        { memory with
                            Cards = revealCard index memory.Cards
                            FirstSelection = Some index
                            Status = "Good start. Find the matching card." } },
                Cmd.none
            | Some first when first <> index ->
                let revealedCards = revealCard index memory.Cards

                let firstCard =
                    revealedCards
                    |> List.find (fun current -> current.Index = first)

                let secondCard =
                    revealedCards
                    |> List.find (fun current -> current.Index = index)

                let nextMoves = memory.Moves + 1

                if firstCard.Value = secondCard.Value then
                    let nextPairs = memory.MatchedPairs + 1

                    let nextBest =
                        if nextPairs = values.Length then
                            match memory.BestRun with
                            | Some best -> Some(min best nextMoves)
                            | None -> Some nextMoves
                        else
                            memory.BestRun

                    let nextStatus =
                        if nextPairs = values.Length then
                            $"Board cleared in {nextMoves} moves."
                        else
                            "Match found. Keep going."

                    let nextMemory =
                        { memory with
                            Cards = markPair first index revealedCards
                            FirstSelection = None
                            Moves = nextMoves
                            MatchedPairs = nextPairs
                            BestRun = nextBest
                            Status = nextStatus }

                    let nextModel = { model with MemoryMatch = nextMemory }

                    if nextPairs = values.Length then
                        match model.Player with
                        | Some player ->
                            { nextModel with
                                IsSubmittingScore = true
                                CurrentPage = Play },
                            submitMemoryScore nextModel player
                        | None -> nextModel, Cmd.none
                    else
                        nextModel, Cmd.none
                else
                    { model with
                        MemoryMatch =
                            { memory with
                                Cards = hidePair first index revealedCards
                                FirstSelection = None
                                Moves = nextMoves
                                Status = "Miss. Remember those positions and try again." } },
                    Cmd.none
            | _ -> model, Cmd.none
        | _ -> model, Cmd.none
    | RecallReadyTick (sessionSeed, roundNumber, remaining) ->
        let recall = model.PatternRecall

        if model.SelectedGameId <> patternRecallId
           || recall.SessionSeed <> sessionSeed
           || recall.CurrentRound <> roundNumber
           || recall.Phase <> Ready then
            model, Cmd.none
        elif remaining > 0 then
            { model with
                PatternRecall =
                    { recall with
                        Countdown = remaining
                        Status = $"Get ready. Grid flashes in {remaining}." } },
            recallReadyTick sessionSeed roundNumber (remaining - 1)
        else
            { model with
                PatternRecall =
                    { recall with
                        Phase = Memorize
                        Countdown = 0
                        RevealCountdown = 3
                        Status = "Memorize the lit cells now." } },
            recallRevealTick sessionSeed roundNumber 2
    | RecallRevealTick (sessionSeed, roundNumber, remaining) ->
        let recall = model.PatternRecall

        if model.SelectedGameId <> patternRecallId
           || recall.SessionSeed <> sessionSeed
           || recall.CurrentRound <> roundNumber
           || recall.Phase <> Memorize then
            model, Cmd.none
        elif remaining > 0 then
            { model with PatternRecall = { recall with RevealCountdown = remaining } },
            recallRevealTick sessionSeed roundNumber (remaining - 1)
        else
            { model with
                PatternRecall =
                    { recall with
                        Phase = Input
                        RevealCountdown = 0
                        Status = "The lights are gone. Rebuild the pattern from memory." } },
            Cmd.none
    | RecallCellClicked index when model.SelectedGameId <> patternRecallId -> model, Cmd.none
    | RecallCellClicked index ->
        let recall = model.PatternRecall

        match recall.Phase with
        | Finished
        | Ready -> model, Cmd.none
        | Memorize
        | Input ->
            let interactiveState =
                if recall.Phase = Memorize then
                    { recall with
                        Phase = Input
                        RevealCountdown = 0
                        Status = "The pattern is hidden now. Trust the snapshot and click." }
                else
                    recall

            let nextState, clearedRound, missedRound =
                applyRecallSelection index interactiveState

            let nextRound = interactiveState.CurrentRound + 1

            if clearedRound then
                let clearedCount = interactiveState.RoundsCleared + 1

                let advancedState =
                    { nextState with
                        RoundsCleared = clearedCount
                        BestRounds =
                            match interactiveState.BestRounds with
                            | Some best -> Some(max best clearedCount)
                            | None -> Some clearedCount
                        Status = "Grid cleared. Next pattern incoming." }

                { model with PatternRecall = advancedState }, recallAdvanceRound interactiveState.SessionSeed nextRound
            elif missedRound then
                { model with PatternRecall = nextState }, recallAdvanceRound interactiveState.SessionSeed nextRound
            else
                { model with PatternRecall = nextState }, Cmd.none
    | RecallAdvanceRound (sessionSeed, nextRound) ->
        let recall = model.PatternRecall

        if model.SelectedGameId <> patternRecallId
           || recall.SessionSeed <> sessionSeed
           || recall.Phase = Finished then
            model, Cmd.none
        else
            let nextState = patternRecallRound sessionSeed nextRound recall
            { model with PatternRecall = nextState }, queueRecallRound nextState
    | RecallSessionExpired sessionSeed ->
        let recall = model.PatternRecall

        if model.SelectedGameId <> patternRecallId
           || recall.SessionSeed <> sessionSeed
           || recall.Phase = Finished then
            model, Cmd.none
        else
            let finishedState =
                { recall with
                    Phase = Finished
                    Countdown = 0
                    RevealCountdown = 0
                    Status = $"Time. You cleared {recall.RoundsCleared} grids in sixty seconds." }

            let nextModel =
                { model with
                    PatternRecall = finishedState
                    CurrentPage = Play }

            if recall.RoundsCleared > 0 then
                match model.Player with
                | Some player -> { nextModel with IsSubmittingScore = true }, submitRecallScore nextModel player
                | None -> nextModel, Cmd.none
            else
                nextModel, Cmd.none
