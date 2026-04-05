module BrainGames.Client.Main

open System
open Browser.Dom

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
      SessionName: string
      BestRun: int option }

type Msg =
    | NewGame
    | FlipCard of int

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
      Status = "Flip two cards at a time. Match every pair with as few moves as possible."
      SessionName = pickSessionName ()
      BestRun = None }

let init () = initialModel ()

let completionPercent model =
    (float model.MatchedPairs / float values.Length)
    * 100.0
    |> int

let remainingPairs model = values.Length - model.MatchedPairs

let focusScore model =
    max 0 (100 - ((model.Moves - model.MatchedPairs) * 8))

let pulseLabel model =
    match model.MatchedPairs, model.Moves with
    | pairs, _ when pairs = values.Length -> "Cleared"
    | 0, 0 -> "Idle"
    | _, moves when moves <= model.MatchedPairs * 2 -> "Sharp"
    | _, moves when moves <= model.MatchedPairs * 3 -> "Steady"
    | _ -> "Recovering"

let cardLabel card =
    if card.Revealed || card.Matched then
        card.Value
    else
        "?"

let cardClass card =
    if card.Matched then
        "memory-card matched"
    elif card.Revealed then
        "memory-card revealed"
    else
        "memory-card"

let tryFindCard index cards =
    cards
    |> List.tryFind (fun card -> card.Index = index)

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
    | NewGame -> { initialModel () with BestRun = model.BestRun }
    | FlipCard index ->
        match tryFindCard index model.Cards with
        | Some card when not card.Matched && not card.Revealed ->
            match model.FirstSelection with
            | None ->
                { model with
                    Cards = revealCard index model.Cards
                    FirstSelection = Some index
                    Status = "Good start. Find the matching card." }
            | Some first when first <> index ->
                let revealedCards = revealCard index model.Cards

                let firstCard =
                    revealedCards
                    |> List.find (fun current -> current.Index = first)

                let secondCard =
                    revealedCards
                    |> List.find (fun current -> current.Index = index)

                let nextMoves = model.Moves + 1

                if firstCard.Value = secondCard.Value then
                    let nextPairs = model.MatchedPairs + 1

                    let nextBest =
                        if nextPairs = values.Length then
                            match model.BestRun with
                            | Some best -> Some(min best nextMoves)
                            | None -> Some nextMoves
                        else
                            model.BestRun

                    let nextStatus =
                        if nextPairs = values.Length then
                            $"Board cleared in {nextMoves} moves."
                        else
                            "Match found. Keep going."

                    { model with
                        Cards = markPair first index revealedCards
                        FirstSelection = None
                        Moves = nextMoves
                        MatchedPairs = nextPairs
                        BestRun = nextBest
                        Status = nextStatus }
                else
                    { model with
                        Cards = hidePair first index revealedCards
                        FirstSelection = None
                        Moves = nextMoves
                        Status = "Miss. Remember those positions and try again." }
            | _ -> model
        | _ -> model

let renderCards model =
    model.Cards
    |> List.map (fun card ->
        $"""<button id="card-{card.Index}" class="{cardClass card}" type="button" aria-label="Memory card {card.Index + 1}">{cardLabel card}</button>""")
    |> String.concat ""

let renderFocusBars model =
    [ 18; 32; 28; 44; 38; 52; 41; 57; 48; 64; 51; 46 ]
    |> List.mapi (fun index height ->
        let activeClass =
            if index <= model.MatchedPairs then
                "focus-bar active"
            else
                "focus-bar"

        $"""<span class="{activeClass}" style="height: {height}px"></span>""")
    |> String.concat ""

let renderCountryRows model =
    let completion = completionPercent model

    [ ("United States", "US", 142410, completion + 8)
      ("Germany", "DE", 175133, max 28 (focusScore model))
      ("Italy", "IT", 58173, max 20 (completion - 6))
      ("England", "EN", 138110, max 24 (completion + 2))
      ("United Kingdom", "UK", 182503, max 34 (focusScore model + 6)) ]
    |> List.map (fun (name, flag, score, barValue) ->
        let width = min 100 barValue
        let widthLabel = $"{width}%%"

        $"""
        <div class="country-row">
          <div class="country-meta">
            <span class="flag-pill">{flag}</span>
            <span>{name}</span>
          </div>
          <div class="country-track">
            <span class="country-fill" style="width: {widthLabel}"></span>
          </div>
          <strong>{score}</strong>
        </div>
        """)
    |> String.concat ""

let view model dispatch =
    document.title <- "Brain Games"
    let progressLabel = $"{completionPercent model}%%"
    let focusScoreLabel = $"{focusScore model}"
    let focusBars = renderFocusBars model
    let countryRows = renderCountryRows model

    match document.getElementById ("app") with
    | null -> console.error ("Brain Games could not find the #app mount node.")
    | mountPoint ->
        mountPoint.innerHTML <-
            $"""
            <main class="shell">
              <a class="skip-link" href="#game-board">Skip to game board</a>
              <aside class="sidebar">
                <div class="brand-block">
                  <div class="brand-mark">
                    <span class="brand-mark-a"></span>
                    <span class="brand-mark-b"></span>
                    <span class="brand-mark-c"></span>
                  </div>
                  <div>
                    <strong>Brain Games</strong>
                    <small>Operator View</small>
                  </div>
                </div>

                <nav class="side-nav" aria-label="Primary">
                  <button class="nav-item active" type="button">Dashboard</button>
                  <button class="nav-item" type="button">Sessions</button>
                  <button class="nav-item" type="button">Leaderboard</button>
                  <button class="nav-item" type="button">Players</button>
                  <button class="nav-item" type="button">Reports</button>
                </nav>

                <div class="side-section">
                  <span class="side-label">Current mode</span>
                  <div class="side-chip">Memory Match Sprint</div>
                </div>

                <div class="side-footer">
                  <div class="session-chip dark">
                    <span>Session</span>
                    <strong>{model.SessionName}</strong>
                  </div>
                  <div class="sidebar-note">
                    Guest play remains active while the dashboard shell previews future analytics surfaces.
                  </div>
                </div>
              </aside>

              <section class="workspace">
                <header class="topbar">
                  <label class="search-shell" aria-label="Search">
                    <span class="search-icon">⌕</span>
                    <input class="search-input" type="text" value="search" readonly />
                  </label>
                  <div class="topbar-actions">
                    <button class="icon-button" type="button">◌</button>
                    <div class="account-chip">
                      <span class="avatar-pill">BG</span>
                      <div>
                        <strong>Brain Games</strong>
                        <small>Administrator</small>
                      </div>
                    </div>
                  </div>
                </header>

                <section class="page-header">
                  <div class="page-header-copy">
                    <span class="eyebrow">Memory Match Sprint</span>
                    <h1>The game board comes first.</h1>
                    <p class="subhead">Everything else supports the live run: the board stays central, progress stays close, and analytics move into the background.</p>
                  </div>
                  <div class="page-actions">
                    <div class="session-chip">
                      <span>Active session</span>
                      <strong>{model.SessionName}</strong>
                    </div>
                    <button id="new-game-button" class="cta" type="button">Start fresh board</button>
                  </div>
                </section>

                <section class="stats-strip" aria-label="Current run stats">
                  <div class="stat-card">
                    <span>Moves</span>
                    <strong>{model.Moves}</strong>
                    <small>{pulseLabel model}</small>
                  </div>
                  <div class="stat-card">
                    <span>Pairs</span>
                    <strong>{model.MatchedPairs}/{values.Length}</strong>
                    <small>{remainingPairs model} left</small>
                  </div>
                  <div class="stat-card">
                    <span>Best Run</span>
                    <strong>{model.BestRun
                             |> Option.map string
                             |> Option.defaultValue "-"}</strong>
                    <small>Personal baseline</small>
                  </div>
                  <div class="stat-card">
                    <span>Focus Score</span>
                    <strong>{focusScoreLabel}</strong>
                    <small>{progressLabel} complete</small>
                  </div>
                </section>

                <section class="panel board-hero" id="game-board">
                  <div class="board-hero-head">
                    <div class="panel-head-copy">
                      <h2>Live board</h2>
                      <p class="status-copy">{model.Status}</p>
                    </div>
                    <div class="panel-head-meta">
                      <span class="legend-pill"><span class="legend-dot purple"></span> Focus pulse</span>
                      <span class="legend-pill"><span class="legend-dot green"></span> Matched cards</span>
                    </div>
                  </div>

                  <div class="board-hero-grid">
                    <div class="board-shell">
                      <div class="board-grid-wrap">
                        <div class="memory-grid featured-grid">{renderCards model}</div>
                      </div>
                    </div>

                    <div class="board-side">
                      <div class="snapshot-card spotlight-card">
                        <strong>{progressLabel}</strong>
                        <span>Board completion</span>
                      </div>

                      <div class="snapshot-card">
                        <strong>{focusScoreLabel}</strong>
                        <span>Estimated control score</span>
                      </div>

                      <div class="snapshot-card">
                        <strong>{model.SessionName}</strong>
                        <span>Active session label</span>
                      </div>
                    </div>
                  </div>
                </section>

                <section class="support-grid">
                  <section class="panel support-panel compact-panel">
                    <div class="panel-head">
                      <div class="panel-head-copy">
                        <h2>Focus Trend</h2>
                        <p class="status-copy">Compact run telemetry that supports the board instead of competing with it.</p>
                      </div>
                    </div>

                    <div class="focus-chart compact-chart">
                      <div class="focus-axis">
                        <span>50k</span>
                        <span>30k</span>
                        <span>10k</span>
                      </div>
                      <div class="focus-bars">
                        {focusBars}
                      </div>
                    </div>
                  </section>

                  <section class="panel activity-panel compact-panel">
                    <div class="panel-head">
                      <div class="panel-head-copy">
                        <h2>Recent Country Activities</h2>
                        <p class="status-copy">Secondary activity context and ranked traffic signals.</p>
                      </div>
                      <div class="panel-head-meta">
                        <span class="legend-pill"><span class="legend-dot purple"></span> Activity pulse</span>
                        <span class="legend-pill"><span class="legend-dot blue"></span> Ranked traffic</span>
                      </div>
                    </div>

                    <div class="activity-grid">
                      <div class="map-card">
                        <div class="dot-map">
                          <span class="map-dot dot-a"></span>
                          <span class="map-dot dot-b"></span>
                          <span class="map-dot dot-c"></span>
                          <span class="map-dot dot-d"></span>
                          <span class="map-dot dot-e"></span>
                          <span class="map-tooltip">United States · 46.057</span>
                        </div>
                      </div>

                      <div class="country-list">
                        <div class="impression-block">
                          <strong>245.145</strong>
                          <span>Impressions</span>
                        </div>
                        {countryRows}
                      </div>
                    </div>
                  </section>
                </section>
              </section>
            </main>
            """

        match document.getElementById ("new-game-button") with
        | null -> ()
        | element -> element.addEventListener ("click", (fun _ -> dispatch NewGame))

        model.Cards
        |> List.iter (fun card ->
            match document.getElementById ($"card-{card.Index}") with
            | null -> ()
            | element -> element.addEventListener ("click", (fun _ -> dispatch (FlipCard card.Index))))

let mutable currentModel = init ()

let rec dispatch msg =
    currentModel <- update msg currentModel
    view currentModel dispatch

view currentModel dispatch
