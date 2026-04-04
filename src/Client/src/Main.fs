module BrainGames.Client.Main

open System
open Browser.Dom
open Browser.Types
open Elmish

type Card =
    { Index: int
      Value: string
      Revealed: bool
      Matched: bool }

type Model =
    { Cards: Card list
      FirstSelection: int option
      SecondSelection: int option
      Moves: int
      MatchedPairs: int
      Status: string }

type Msg =
    | NewGame
    | FlipCard of int
    | ResolveTurn

let values = [ "A"; "B"; "C"; "D"; "E"; "F" ]

let shuffle items =
    let rng = Random()
    items |> List.sortBy (fun _ -> rng.Next())

let createDeck () =
    values
    |> List.collect (fun value -> [ value; value ])
    |> shuffle
    |> List.mapi (fun index value ->
        { Index = index
          Value = value
          Revealed = false
          Matched = false })

let init () =
    { Cards = createDeck ()
      FirstSelection = None
      SecondSelection = None
      Moves = 0
      MatchedPairs = 0
      Status = "Flip two cards at a time. Efficient runs will matter once leaderboard wiring lands." }

let update msg model =
    match msg with
    | NewGame ->
        init ()
    | FlipCard index ->
        let selectedCard = model.Cards |> List.tryFind (fun card -> card.Index = index)

        match selectedCard with
        | Some card when not card.Matched && not card.Revealed && model.SecondSelection.IsNone ->
            let reveal cards =
                cards
                |> List.map (fun current ->
                    if current.Index = index then { current with Revealed = true } else current)

            match model.FirstSelection with
            | None ->
                { model with
                    Cards = reveal model.Cards
                    FirstSelection = Some index
                    Status = "Good. Now find its pair." }
            | Some first when first <> index ->
                let revealedCards = reveal model.Cards
                let firstCard = revealedCards |> List.find (fun current -> current.Index = first)
                let secondCard = revealedCards |> List.find (fun current -> current.Index = index)

                if firstCard.Value = secondCard.Value then
                    let nextPairs = model.MatchedPairs + 1
                    let nextStatus =
                        if nextPairs = values.Length then
                            $"Board cleared in {model.Moves + 1} moves. Next: wire this run to the API leaderboard."
                        else
                            "Match found. Keep going."

                    { model with
                        Cards =
                            revealedCards
                            |> List.map (fun current ->
                                if current.Index = first || current.Index = index then
                                    { current with Matched = true }
                                else
                                    current)
                        FirstSelection = None
                        SecondSelection = None
                        Moves = model.Moves + 1
                        MatchedPairs = nextPairs
                        Status = nextStatus }
                else
                    { model with
                        Cards =
                            model.Cards
                            |> List.map (fun current ->
                                if current.Index = first || current.Index = index then
                                    { current with Revealed = false }
                                else
                                    current)
                        FirstSelection = None
                        SecondSelection = None
                        Moves = model.Moves + 1
                        Status = "Miss. Try to remember where those cards were." }
            | _ -> model
        | _ -> model
    | ResolveTurn ->
        model

let cardLabel card =
    if card.Revealed || card.Matched then card.Value else "?"

let cardClass card =
    if card.Matched then "memory-card matched"
    elif card.Revealed then "memory-card revealed"
    else "memory-card"

let render model dispatch =
    let cards =
        model.Cards
        |> List.map (fun card ->
            $"""<button id="card-{card.Index}" class="{cardClass card}">{cardLabel card}</button>""")
        |> String.concat ""

    document.getElementById("app").innerHTML <-
        $"""
        <main class="shell">
          <section class="hero">
            <div class="hero-top">
              <div class="brand">
                <span class="eyebrow">Brain training over doomscrolling</span>
                <h1>Brain Games</h1>
                <p class="subhead">The foundation is live: a playable memory loop in F#, a compiled API, shared score contracts, and docs that future sessions can resume from.</p>
              </div>
              <button id="new-game-button" class="cta">Shuffle board</button>
            </div>
            <div class="hero-stats">
              <div class="stat"><strong>{model.Moves}</strong><span>Moves</span></div>
              <div class="stat"><strong>{model.MatchedPairs}/{values.Length}</strong><span>Pairs found</span></div>
              <div class="stat"><strong>API ready</strong><span>Leaderboard and profile endpoints compile</span></div>
            </div>
          </section>
          <section class="grid">
            <div class="panel">
              <div class="panel-title">
                <div>
                  <h2>Memory Match Sprint</h2>
                  <p class="muted">{model.Status}</p>
                </div>
              </div>
              <div class="memory-grid">{cards}</div>
            </div>
            <div class="stack">
              <div class="panel">
                <div class="panel-title">
                  <div>
                    <h2>Current Build</h2>
                    <p class="muted">What is implemented now.</p>
                  </div>
                </div>
                <div class="lesson"><strong>Backend</strong><span class="muted">Boot, profile, leaderboard, and score endpoints compile.</span></div>
                <div class="lesson"><strong>Frontend</strong><span class="muted">Playable local memory loop built with F# and Elmish.</span></div>
                <div class="lesson"><strong>Next</strong><span class="muted">Wire score submission and registration into the SPA.</span></div>
              </div>
            </div>
          </section>
        </main>
        """

    match document.getElementById("new-game-button") with
    | null -> ()
    | element -> element.addEventListener("click", fun _ -> dispatch NewGame)

    model.Cards
    |> List.iter (fun card ->
        match document.getElementById($"card-{card.Index}") with
        | null -> ()
        | element -> element.addEventListener("click", fun _ -> dispatch (FlipCard card.Index)))

Program.mkSimple init update render
|> Program.withConsoleTrace
|> Program.run
