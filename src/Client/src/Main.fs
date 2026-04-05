module BrainGames.Client.Main

open System
open Browser.Dom

type View =
    | Arena
    | Progress
    | Learn

type Card =
    { Index: int
      Value: string
      Revealed: bool
      Matched: bool }

type Model =
    { Cards: Card list
      FirstSelection: int option
      Moves: int
      MatchedPairs: int
      Status: string
      ActiveView: View
      SessionName: string
      BestRun: int option }

type Msg =
    | NewGame
    | FlipCard of int
    | SetView of View

let values = [ "A"; "B"; "C"; "D"; "E"; "F" ]

let sessionNames =
    [| "Focus Sprint"
       "Recall Run"
       "Pattern Push"
       "Sharpness Loop" |]

let shuffle items =
    let rng = Random()
    items |> List.sortBy (fun _ -> rng.Next())

let pickSessionName () =
    let rng = Random()
    sessionNames[rng.Next(sessionNames.Length)]

let createDeck () =
    values
    |> List.collect (fun value -> [ value; value ])
    |> shuffle
    |> List.mapi (fun index value ->
        { Index = index
          Value = value
          Revealed = false
          Matched = false })

let initialModel () =
    { Cards = createDeck ()
      FirstSelection = None
      Moves = 0
      MatchedPairs = 0
      Status = "Flip two cards at a time. Clean runs will eventually feed your leaderboard profile."
      ActiveView = Arena
      SessionName = pickSessionName ()
      BestRun = None }

let init () = initialModel ()

let efficiencyLabel model =
    match model.MatchedPairs, model.Moves with
    | pairs, moves when pairs = values.Length -> "Board cleared"
    | 0, _ -> "Warming up"
    | _, moves when moves <= model.MatchedPairs * 2 -> "Locked in"
    | _, moves when moves <= model.MatchedPairs * 3 -> "Steady"
    | _ -> "Recovering"

let completionPercent model =
    (float model.MatchedPairs / float values.Length) * 100.0 |> int

let remainingPairs model = values.Length - model.MatchedPairs

let targetMoves = (values.Length * 2) + 2

let momentumCopy model =
    match model.MatchedPairs, model.Moves with
    | pairs, _ when pairs = values.Length -> "Full clear secured. This run is ready for leaderboard wiring."
    | 0, 0 -> "Fresh board. Start with the corners and build a clean mental map."
    | pairs, moves when moves <= (pairs * 2) + 1 -> "Momentum is high. Keep the rhythm and avoid panic flips."
    | _, _ -> "The board is still recoverable. Slow down and re-anchor on known pairs."

let focusBand model =
    match completionPercent model with
    | percent when percent >= 100 -> "Launch-ready"
    | percent when percent >= 67 -> "Dialed in"
    | percent when percent >= 34 -> "Building pace"
    | _ -> "Warm-up"

let progressCopy view =
    match view with
    | Arena -> "Play the live board and sharpen pattern recall."
    | Progress -> "Track what a better run should optimize next."
    | Learn -> "Translate each round into short retention habits."

let cardLabel card =
    if card.Revealed || card.Matched then card.Value else "?"

let cardClass card =
    if card.Matched then "memory-card matched"
    elif card.Revealed then "memory-card revealed"
    else "memory-card"

let navClass current target =
    if current = target then "nav-button active" else "nav-button"

let viewName view =
    match view with
    | Arena -> "Arena"
    | Progress -> "Progress"
    | Learn -> "Lessons"

let tryFindCard index cards =
    cards |> List.tryFind (fun card -> card.Index = index)

let revealCard index cards =
    cards
    |> List.map (fun current ->
        if current.Index = index then
            { current with Revealed = true }
        else
            current)

let markPair first second cards =
    cards
    |> List.map (fun current ->
        if current.Index = first || current.Index = second then
            { current with Matched = true }
        else
            current)

let hidePair first second cards =
    cards
    |> List.map (fun current ->
        if current.Index = first || current.Index = second then
            { current with Revealed = false }
        else
            current)

let update msg model =
    match msg with
    | NewGame ->
        { initialModel () with
            ActiveView = model.ActiveView
            BestRun = model.BestRun }
    | SetView view ->
        { model with
            ActiveView = view
            Status = progressCopy view }
    | FlipCard index ->
        match tryFindCard index model.Cards with
        | Some card when not card.Matched && not card.Revealed ->
            match model.FirstSelection with
            | None ->
                { model with
                    Cards = revealCard index model.Cards
                    FirstSelection = Some index
                    Status = "Good first read. Find the matching card before the board gets noisy." }
            | Some first when first <> index ->
                let revealedCards = revealCard index model.Cards
                let firstCard = revealedCards |> List.find (fun current -> current.Index = first)
                let secondCard = revealedCards |> List.find (fun current -> current.Index = index)
                let nextMoveCount = model.Moves + 1

                if firstCard.Value = secondCard.Value then
                    let nextPairs = model.MatchedPairs + 1
                    let nextBest =
                        if nextPairs = values.Length then
                            match model.BestRun with
                            | Some best -> Some(min best nextMoveCount)
                            | None -> Some nextMoveCount
                        else
                            model.BestRun

                    let nextStatus =
                        if nextPairs = values.Length then
                            $"Board cleared in {nextMoveCount} moves. Next step: wire this run into the live leaderboard."
                        else
                            "Match found. Keep chaining clean reads."

                    { model with
                        Cards = markPair first index revealedCards
                        FirstSelection = None
                        Moves = nextMoveCount
                        MatchedPairs = nextPairs
                        BestRun = nextBest
                        Status = nextStatus }
                else
                    { model with
                        Cards = hidePair first index revealedCards
                        FirstSelection = None
                        Moves = nextMoveCount
                        Status = "Miss. Reset your mental map and scan the outer ring first." }
            | _ ->
                model
        | _ ->
            model

let renderHero model =
    let progressLabel = $"{completionPercent model}%%"

    $"""
    <section class="hero">
      <div class="hero-copy">
        <a class="skip-link" href="#main-content">Skip to main content</a>
        <span class="eyebrow">Focused play over empty scrolling</span>
        <h1>Brain Games turns quick sessions into repeatable momentum.</h1>
        <p class="subhead">A sharper F# client, a cleaner game arena, and a UI that feels more like a product than a placeholder.</p>
        <div class="hero-actions">
          <button id="new-game-button" class="cta" type="button">Shuffle a fresh board</button>
          <div class="session-chip">
            <span class="session-kicker">Current session</span>
            <strong>{model.SessionName}</strong>
            <small>Focus band: {focusBand model}</small>
          </div>
        </div>
      </div>
      <div class="hero-aside">
        <div class="hero-stat spotlight">
          <span>Board progress</span>
          <strong>{progressLabel}</strong>
          <small>{model.MatchedPairs}/{values.Length} pairs solved</small>
        </div>
        <div class="hero-stat">
          <span>Efficiency</span>
          <strong>{efficiencyLabel model}</strong>
          <small>{model.Moves} moves so far</small>
        </div>
        <div class="hero-stat">
          <span>Best local run</span>
          <strong>{model.BestRun |> Option.map string |> Option.defaultValue "None yet"}</strong>
          <small>Moves to clear the full board</small>
        </div>
      </div>
      <div class="hero-marquee" aria-label="Current session signals">
        <div class="marquee-pill">
          <span class="marquee-dot"></span>
          <strong>Live arena</strong>
          <small>Guest-friendly play remains part of the product flow.</small>
        </div>
        <div class="marquee-pill">
          <strong>{remainingPairs model}</strong>
          <small>pairs left to lock in</small>
        </div>
        <div class="marquee-pill">
          <strong>{targetMoves}</strong>
          <small>move target for a sharp clear</small>
        </div>
      </div>
    </section>
    """

let renderNav model =
    $"""
    <nav class="nav" aria-label="Primary views">
      <button id="nav-arena" class="{navClass model.ActiveView Arena}" type="button" aria-pressed="{(model.ActiveView = Arena).ToString().ToLowerInvariant()}">Arena</button>
      <button id="nav-progress" class="{navClass model.ActiveView Progress}" type="button" aria-pressed="{(model.ActiveView = Progress).ToString().ToLowerInvariant()}">Progress</button>
      <button id="nav-learn" class="{navClass model.ActiveView Learn}" type="button" aria-pressed="{(model.ActiveView = Learn).ToString().ToLowerInvariant()}">Lessons</button>
    </nav>
    """

let renderCards model =
    model.Cards
    |> List.map (fun card ->
        $"""<button id="card-{card.Index}" class="{cardClass card}" type="button" aria-label="Memory card {card.Index + 1}">{cardLabel card}</button>""")
    |> String.concat ""

let renderMainPanel model =
    let progressPercent = completionPercent model
    let progressWidth = string progressPercent + "%"
    let progressSummary = string progressPercent + "% complete with " + string (remainingPairs model) + " pairs left."
    let nextGoal =
        if model.MatchedPairs = values.Length then
            "Submit score"
        else
            "Keep the streak clean"

    $"""
    <section class="panel game-panel" id="main-content">
      <div class="panel-head">
        <div>
          <span class="panel-kicker">Live board</span>
          <h2>Memory Match Sprint</h2>
        </div>
        <div class="panel-pill">{viewName model.ActiveView}</div>
      </div>
      <p class="status-copy">{model.Status}</p>
      <div class="signal-banner">
        <strong>Momentum</strong>
        <span>{momentumCopy model}</span>
      </div>
      <div class="board-meta">
        <div class="meta-card">
          <span>Moves</span>
          <strong>{model.Moves}</strong>
        </div>
        <div class="meta-card">
          <span>Pairs matched</span>
          <strong>{model.MatchedPairs}/{values.Length}</strong>
        </div>
        <div class="meta-card">
          <span>Next goal</span>
          <strong>{nextGoal}</strong>
        </div>
      </div>
      <div class="progress-rail" aria-label="Session progress">
        <div class="progress-track">
          <span class="progress-fill" style="width: {progressWidth}"></span>
        </div>
        <small>{progressSummary}</small>
      </div>
      <div class="board-stage">
        <div class="board-shell">
          <div class="memory-grid">{renderCards model}</div>
        </div>
      </div>
    </section>
    """

let renderSidePanels model =
    let viewSpecificPanel =
        match model.ActiveView with
        | Arena ->
            """
            <div class="panel">
              <div class="panel-head">
                <div>
                  <span class="panel-kicker">Tactical cues</span>
                  <h3>Stay ahead of the board</h3>
                </div>
              </div>
              <div class="detail-card">
                <strong>Open with structure</strong>
                <p>Clear the corners first, then sweep inward. That reduces repeated misses.</p>
              </div>
              <div class="detail-card">
                <strong>Play for the API loop</strong>
                <p>The current backend already exposes registration, leaderboard, profile, and score endpoints.</p>
              </div>
            </div>
            """
        | Progress ->
            $"""
            <div class="panel">
              <div class="panel-head">
                <div>
                  <span class="panel-kicker">Run analysis</span>
                  <h3>What improves next</h3>
                </div>
              </div>
              <div class="detail-card">
                <strong>Current efficiency</strong>
                <p>{efficiencyLabel model} play usually means fewer panic flips once the board is half-solved.</p>
              </div>
              <div class="detail-card">
                <strong>Local best</strong>
                <p>{model.BestRun |> Option.map (fun best -> $"Beat {best} moves on the next clear.") |> Option.defaultValue "Finish one full board to establish a benchmark."}</p>
              </div>
            </div>
            """
        | Learn ->
            """
            <div class="panel">
              <div class="panel-head">
                <div>
                  <span class="panel-kicker">Lessons</span>
                  <h3>Retention prompts</h3>
                </div>
              </div>
              <div class="detail-card">
                <strong>Chunk the grid</strong>
                <p>Mentally divide the board into zones so each reveal reinforces a smaller map.</p>
              </div>
              <div class="detail-card">
                <strong>Name the pair</strong>
                <p>Give each symbol a short internal label. Retrieval gets faster when the cue is verbal.</p>
              </div>
            </div>
            """

    $"""
    <section class="stack">
      <div class="panel">
        <div class="panel-head">
          <div>
            <span class="panel-kicker">Build status</span>
            <h3>Current product slice</h3>
          </div>
        </div>
        <div class="detail-card">
          <strong>Frontend</strong>
          <p>Playable F# memory loop running through Fable, Elmish, and Vite.</p>
        </div>
        <div class="detail-card">
          <strong>Backend</strong>
          <p>Boot, leaderboard, profile, and score submission endpoints are already present and compiling.</p>
        </div>
        <div class="detail-card">
          <strong>Next product move</strong>
          <p>Connect registration, profile fetches, and score posting without forcing auth for every player.</p>
        </div>
      </div>
      <div class="panel accent-panel">
        <div class="panel-head">
          <div>
            <span class="panel-kicker">Session radar</span>
            <h3>What to optimize</h3>
          </div>
        </div>
        <div class="detail-card">
          <strong>Accuracy first</strong>
          <p>Perfect memory beats frantic clicking. The board rewards controlled reads more than raw speed.</p>
        </div>
        <div class="detail-card">
          <strong>Clear target</strong>
          <p>{if model.Moves <= targetMoves then "You are inside the target pace window." else "Trim extra flips and you move back into scoring range."}</p>
        </div>
      </div>
      {viewSpecificPanel}
    </section>
    """

let render model dispatch =
    document.title <- "Brain Games"

    match document.getElementById("app") with
    | null ->
        console.error("Brain Games could not find the #app mount node.")
    | mountPoint ->
        mountPoint.innerHTML <-
            $"""
            <main class="shell">
              {renderHero model}
              {renderNav model}
              <section class="content-grid">
                {renderMainPanel model}
                {renderSidePanels model}
              </section>
            </main>
            """

        match document.getElementById("new-game-button") with
        | null -> ()
        | element -> element.addEventListener("click", fun _ -> dispatch NewGame)

        [ ("nav-arena", Arena); ("nav-progress", Progress); ("nav-learn", Learn) ]
        |> List.iter (fun (elementId, view) ->
            match document.getElementById(elementId) with
            | null -> ()
            | element -> element.addEventListener("click", fun _ -> dispatch (SetView view)))

        model.Cards
        |> List.iter (fun card ->
            match document.getElementById($"card-{card.Index}") with
            | null -> ()
            | element -> element.addEventListener("click", fun _ -> dispatch (FlipCard card.Index)))

let mutable currentModel = init ()

let rec dispatch msg =
    currentModel <- update msg currentModel
    render currentModel dispatch

render currentModel dispatch
