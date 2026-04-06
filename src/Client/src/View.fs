module BrainGames.Client.View

open System
open Browser.Dom
open BrainGames.Shared
open BrainGames.Client.State

let private escapeHtml (value: string) =
    value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&#39;")

let private textOrFallback fallback value =
    if String.IsNullOrWhiteSpace value then
        fallback
    else
        value

let private bestScore model =
    model.Leaderboard
    |> Array.tryHead
    |> Option.map (fun entry -> string entry.Score)
    |> Option.defaultValue (
        if model.IsLeaderboardLoading then
            "..."
        else
            "-"
    )

let private currentRank model =
    match model.Player with
    | None ->
        if model.IsRegistering then
            "Joining..."
        else
            "Guest"
    | Some player ->
        model.Leaderboard
        |> Array.tryFindIndex (fun entry -> entry.PlayerId = player.PlayerId)
        |> Option.map (fun index -> $"#{index + 1}")
        |> Option.defaultValue "Unranked"

let private activeGame model =
    model.Boot
    |> Option.bind (fun boot ->
        boot.Games
        |> Array.tryFind (fun game -> game.Id = model.SelectedGameId))

let private appName model =
    model.Boot
    |> Option.map (fun boot -> boot.AppName)
    |> Option.defaultValue "Brain Games"

let private gameName model =
    activeGame model
    |> Option.map (fun game -> game.Name)
    |> Option.defaultValue "Brain Games"

let private tagline model =
    model.Boot
    |> Option.map (fun boot -> boot.Tagline)
    |> Option.defaultValue "Train recall, keep the board central, and let the rest of the interface support the run."

let private authMode model =
    model.Boot
    |> Option.map (fun boot -> boot.AuthMode)
    |> Option.defaultValue "local-dev"

let private playerName model =
    model.Player
    |> Option.map (fun player -> player.DisplayName)
    |> Option.defaultValue "Guest Player"

let private profileStat fallback projection model =
    model.Profile
    |> Option.map projection
    |> Option.defaultValue fallback

let private formatElapsedSeconds model =
    max
        1
        (int
            (DateTime.UtcNow - currentSessionStartedAtUtc model)
                .TotalSeconds)

let private renderCards model =
    model.MemoryMatch.Cards
    |> List.map (fun card ->
        let label =
            if card.Revealed || card.Matched then
                card.Value
            else
                "?"

        let classes =
            if card.Matched then
                "memory-card matched"
            elif card.Revealed then
                "memory-card revealed"
            else
                "memory-card"

        $"""<button id="card-{card.Index}" class="{classes}" type="button" aria-label="Memory card {card.Index + 1}">{escapeHtml label}</button>""")
    |> String.concat ""

let private renderRecallTiles model =
    let recall = model.PatternRecall

    recall.Tiles
    |> List.map (fun tile ->
        let classes =
            if tile.IsWrong then
                "recall-tile miss"
            elif tile.IsCorrect then
                "recall-tile hit"
            elif recall.Phase = Memorize && tile.IsTarget then
                "recall-tile target-visible"
            else
                "recall-tile"

        let ariaLabel = $"Recall tile {tile.Index + 1}"
        $"""<button id="recall-{tile.Index}" class="{classes}" type="button" aria-label="{ariaLabel}"></button>""")
    |> String.concat ""

let private gameSwitcher model =
    let renderGamePill gameId label =
        let classes =
            if model.SelectedGameId = gameId then
                "game-pill active"
            else
                "game-pill"

        $"""<button id="game-{gameId}" class="{classes}" type="button">{escapeHtml label}</button>"""

    $"""
    <div class="game-switcher" role="tablist" aria-label="Choose a game mode">
      {renderGamePill "memory-match" "Memory Match Sprint"}
      {renderGamePill "pattern-recall" "Pattern Recall Rush"}
    </div>
    """

let private renderBanner model =
    match model.LastError, model.ServerMessage with
    | Some error, _ -> $"""<div class="banner error-banner" role="status">{escapeHtml error}</div>"""
    | None, Some message -> $"""<div class="banner" role="status">{escapeHtml message}</div>"""
    | None, None -> ""

let private renderNavItem currentPage targetPage label id =
    let classes =
        if currentPage = targetPage then
            "nav-item active"
        else
            "nav-item"

    $"""<button id="{id}" class="{classes}" type="button">{label}</button>"""

let private renderShell model pageTitle pageSummary body =
    let banner = renderBanner model
    let totalGames = profileStat 0 (fun profile -> profile.TotalGamesPlayed) model

    $"""
    <main class="shell app-shell">
      <a class="skip-link" href="#page-content">Skip to page content</a>
      <aside class="sidebar app-sidebar">
        <div class="brand-block">
          <div class="brand-mark">
            <span class="brand-mark-a"></span>
            <span class="brand-mark-b"></span>
            <span class="brand-mark-c"></span>
          </div>
          <div>
            <strong>{escapeHtml (appName model)}</strong>
            <small>Focused Play</small>
          </div>
        </div>

        <nav class="side-nav" aria-label="Primary">
          {renderNavItem model.CurrentPage Play "Play" "nav-play"}
          {renderNavItem model.CurrentPage Leaderboard "Leaderboard" "nav-leaderboard"}
          {renderNavItem model.CurrentPage Progress "Progress" "nav-progress"}
        </nav>

        <div class="side-section">
          <span class="side-label">Current player</span>
          <div class="side-chip">{escapeHtml (playerName model)}</div>
        </div>

        <div class="side-footer">
          <div class="session-chip dark">
            <span>Auth mode</span>
            <strong>{escapeHtml (authMode model)}</strong>
          </div>
          <div class="sidebar-note">
            {totalGames} recorded plays across the current guest profile.
          </div>
        </div>
      </aside>

      <section class="workspace app-workspace" id="page-content">
        <header class="topbar compact-topbar">
          <div class="page-intro">
            <span class="eyebrow">{escapeHtml (gameName model)}</span>
            <h1>{escapeHtml pageTitle}</h1>
            <p class="subhead">{escapeHtml pageSummary}</p>
          </div>
          <div class="account-chip">
            <span class="avatar-pill">BG</span>
            <div>
              <strong>{escapeHtml (playerName model)}</strong>
              <small>{escapeHtml (authMode model)}</small>
            </div>
          </div>
        </header>

        {banner}
        {body}
      </section>
    </main>
    """

let private renderPlayView model =
    let elapsedLabel = $"{formatElapsedSeconds model}s"

    let activeStatus =
        if model.IsSubmittingScore then
            "Submitting score..."
        elif model.IsProfileLoading then
            "Refreshing player profile..."
        elif model.SelectedGameId = "pattern-recall" then
            model.PatternRecall.Status
        else
            model.MemoryMatch.Status

    let memoryPlayBody =
        let memory = model.MemoryMatch
        let progressLabel = $"{completionPercent model}%%"

        $"""
        <div class="play-grid">
          <div class="board-shell board-shell-lg">
            <div class="board-grid-wrap">
              <div class="memory-grid featured-grid">{renderCards model}</div>
            </div>
          </div>

          <aside class="play-session-rail">
            <div class="snapshot-card spotlight-card">
              <strong>{memory.Moves}</strong>
              <span>Moves taken</span>
            </div>
            <div class="snapshot-card">
              <strong>{memory.MatchedPairs}/6</strong>
              <span>Pairs cleared</span>
            </div>
            <div class="snapshot-card">
              <strong>{progressLabel}</strong>
              <span>Board completion</span>
            </div>
            <div class="snapshot-card">
              <strong>{elapsedLabel}</strong>
              <span>Elapsed time</span>
            </div>
            <div class="session-note-card">
              <span class="side-label">Session label</span>
              <strong>{escapeHtml memory.SessionName}</strong>
              <p class="status-copy">Classic card-matching run. Clear the full board with clean flips.</p>
            </div>
          </aside>
        </div>
        """

    let recallPlayBody =
        let recall = model.PatternRecall

        let timeLeft =
            max
                0
                (recall.TimeLimitSeconds
                 - int
                     (DateTime.UtcNow - recall.SessionStartedAtUtc)
                         .TotalSeconds)

        let countdownLabel =
            match recall.Phase with
            | Ready -> $"Ready {recall.Countdown}"
            | Memorize -> $"Memorize {recall.RevealCountdown}"
            | Input -> "Rebuild the pattern"
            | Finished -> "Run complete"

        let gridStyle = $"grid-template-columns: repeat({recall.GridSize}, 88px);"

        $"""
        <div class="play-grid">
          <div class="board-shell board-shell-lg">
            <div class="countdown-pill">{escapeHtml countdownLabel}</div>
            <div class="board-grid-wrap">
              <div class="recall-grid" style="{gridStyle}">{renderRecallTiles model}</div>
            </div>
          </div>

          <aside class="play-session-rail">
            <div class="snapshot-card spotlight-card">
              <strong>{recall.RoundsCleared}</strong>
              <span>Grids cleared</span>
            </div>
            <div class="snapshot-card">
              <strong>{recall.CurrentRound}</strong>
              <span>Current round</span>
            </div>
            <div class="snapshot-card">
              <strong>{timeLeft}s</strong>
              <span>Time left</span>
            </div>
            <div class="snapshot-card">
              <strong>{recall.GridSize}x{recall.GridSize}</strong>
              <span>{recall.TargetCount} lit boxes</span>
            </div>
            <div class="session-note-card">
              <span class="side-label">Run format</span>
              <strong>Sixty-second climb</strong>
              <p class="status-copy">Every couple of grids the board gets denser. One wrong tap moves you to the next pattern.</p>
            </div>
          </aside>
        </div>
        """

    let body =
        $"""
        <section class="play-layout">
          <section class="panel play-surface">
            <div class="play-surface-head">
              <div class="panel-head-copy">
                <h2>Live session</h2>
                <p class="status-copy">{escapeHtml activeStatus}</p>
              </div>
              <button id="new-game-button" class="cta" type="button">Start new run</button>
            </div>

            {gameSwitcher model}
            {if model.SelectedGameId = "pattern-recall" then
                 recallPlayBody
             else
                 memoryPlayBody}
          </section>
        </section>
        """

    renderShell model "The game board comes first." (tagline model) body

let private renderLeaderboardCards (entries: LeaderboardEntry array) =
    entries
    |> Array.map (fun entry ->
        let widthLabel = $"{min 100 (max 18 (entry.Score / 24))}%%"

        let completedLabel =
            entry
                .CompletedAtUtc
                .ToLocalTime()
                .ToString("MMM d · h:mm tt")

        $"""
        <div class="leaderboard-row-card">
          <div class="leaderboard-rank">#{entry.Rank}</div>
          <div class="leaderboard-player">
            <strong>{escapeHtml entry.DisplayName}</strong>
            <span>{entry.Score} pts</span>
          </div>
          <div class="leaderboard-track">
            <span class="country-fill" style="width: {widthLabel}"></span>
          </div>
          <div class="leaderboard-meta">
            <span>{entry.Moves} moves</span>
            <span>{entry.DurationSeconds}s</span>
            <span>{escapeHtml completedLabel}</span>
          </div>
        </div>
        """)
    |> String.concat ""

let private renderLeaderboardView model =
    let rankLabel = currentRank model
    let topEntries = model.Leaderboard |> Array.truncate 5
    let currentGameLabel = gameName model

    let nearbyEntries =
        match model.Player with
        | None -> [||]
        | Some player ->
            match model.Leaderboard
                  |> Array.tryFindIndex (fun entry -> entry.PlayerId = player.PlayerId)
                with
            | None -> [||]
            | Some index ->
                let startIndex = max 0 (index - 1)
                let endIndex = min (model.Leaderboard.Length - 1) (index + 1)
                model.Leaderboard[startIndex..endIndex]

    let latestSummary =
        model.LastScore
        |> Option.map (fun score -> $"Latest result: {score.Score} points, rank #{score.Rank}.")
        |> Option.defaultValue "Complete a run to place your current session on the board."

    let topMarkup =
        if model.IsLeaderboardLoading then
            """<div class="empty-state">Loading leaderboard...</div>"""
        elif Array.isEmpty topEntries then
            """<div class="empty-state">No ranked sessions yet. Complete a run to start the board.</div>"""
        else
            renderLeaderboardCards topEntries

    let nearbyMarkup =
        if model.IsLeaderboardLoading then
            """<div class="empty-state">Finding your current position...</div>"""
        elif Array.isEmpty nearbyEntries then
            """<div class="empty-state">You are not ranked yet. Finish a session to see rivals near your position.</div>"""
        else
            renderLeaderboardCards nearbyEntries

    let body =
        $"""
        <section class="leaderboard-layout">
          <div class="stats-strip leaderboard-strip">
            <div class="stat-card spotlight-stat">
              <span>Your rank</span>
              <strong>{rankLabel}</strong>
              <small>{escapeHtml latestSummary}</small>
            </div>
            <div class="stat-card">
              <span>Top score</span>
              <strong>{bestScore model}</strong>
              <small>{escapeHtml currentGameLabel}</small>
            </div>
            <div class="stat-card">
              <span>Best session</span>
              <strong>{if model.SelectedGameId = "pattern-recall" then
                           model.PatternRecall.BestRounds
                           |> Option.map string
                           |> Option.defaultValue "-"
                       else
                           model.MemoryMatch.BestRun
                           |> Option.map string
                           |> Option.defaultValue "-"}</strong>
              <small>{if model.SelectedGameId = "pattern-recall" then
                          "Most grids cleared this session"
                      else
                          "Fewest moves this session"}</small>
            </div>
          </div>

          <section class="leaderboard-grid">
            <section class="panel">
              <div class="panel-head-copy">
                <h2>Top performers</h2>
                <p class="status-copy">The strongest recent runs for {escapeHtml currentGameLabel}.</p>
              </div>
              <div class="leaderboard-list">{topMarkup}</div>
            </section>

            <section class="panel">
              <div class="panel-head-copy">
                <h2>Near your position</h2>
                <p class="status-copy">See who is immediately ahead of you and who you are holding off.</p>
              </div>
              <div class="leaderboard-list">{nearbyMarkup}</div>
            </section>
          </section>
        </section>
        """

    renderShell
        model
        "Leaderboard and live ranking"
        "Track the top runs, see your standing, and watch the rivals closest to your position."
        body

let private trendCopy model =
    let bestSession =
        if model.SelectedGameId = "pattern-recall" then
            model.PatternRecall.BestRounds
        else
            model.MemoryMatch.BestRun

    match model.LastScore, bestSession with
    | Some score, Some _ when score.PersonalBest ->
        "You just posted a new personal best. Keep the cadence steady and protect the streak."
    | Some _, Some best ->
        if model.SelectedGameId = "pattern-recall" then
            $"Your next target is more than {best} cleared grids. Keep the mistakes down."
        else
            $"Your target is a cleaner run than {best} moves. Focus on reducing wasted flips."
    | Some _, None -> "Your first result is in. Set a cleaner baseline on the next run."
    | None, _ -> "Once you log a few finished sessions, this page will better reflect whether you are improving."

let private renderProgressView model =
    let streakDays = profileStat 0 (fun profile -> profile.StreakDays) model
    let totalGames = profileStat 0 (fun profile -> profile.TotalGamesPlayed) model

    let favoriteCategory =
        profileStat "Memory" (fun profile -> profile.FavoriteCategory) model

    let focusLabel = $"{focusScore model}"

    let lastResultLabel =
        model.LastScore
        |> Option.map (fun score -> $"{score.Score} pts · rank #{score.Rank}")
        |> Option.defaultValue "No scored run yet"

    let bestMovesLabel =
        (if model.SelectedGameId = "pattern-recall" then
             model.PatternRecall.BestRounds
         else
             model.MemoryMatch.BestRun)
        |> Option.map string
        |> Option.defaultValue "-"

    let body =
        $"""
        <section class="progress-layout">
          <div class="stats-strip progress-strip">
            <div class="stat-card spotlight-stat">
              <span>Best score</span>
              <strong>{bestScore model}</strong>
              <small>Current high-water mark</small>
            </div>
            <div class="stat-card">
              <span>{if model.SelectedGameId = "pattern-recall" then
                         "Best climb"
                     else
                         "Best run"}</span>
              <strong>{bestMovesLabel}</strong>
              <small>{if model.SelectedGameId = "pattern-recall" then
                          "Most grids cleared"
                      else
                          "Fewest moves"}</small>
            </div>
            <div class="stat-card">
              <span>Total games</span>
              <strong>{totalGames}</strong>
              <small>Completed sessions</small>
            </div>
            <div class="stat-card">
              <span>Streak</span>
              <strong>{streakDays}</strong>
              <small>Consecutive active days</small>
            </div>
          </div>

          <section class="progress-grid">
            <section class="panel">
              <div class="panel-head-copy">
                <h2>Improvement snapshot</h2>
                <p class="status-copy">{escapeHtml (trendCopy model)}</p>
              </div>

              <div class="progress-meter-grid">
                <div class="progress-meter-card">
                  <span>Latest result</span>
                  <strong>{escapeHtml lastResultLabel}</strong>
                </div>
                <div class="progress-meter-card">
                  <span>Current focus score</span>
                  <strong>{focusLabel}</strong>
                </div>
                <div class="progress-meter-card">
                  <span>Favorite category</span>
                  <strong>{escapeHtml favoriteCategory}</strong>
                </div>
              </div>
            </section>

            <section class="panel">
              <div class="panel-head-copy">
                <h2>What the app can infer today</h2>
                <p class="status-copy">This v1 progress page uses your profile snapshot, latest score, and best run. Richer historical charts will need per-run history from the server.</p>
              </div>

              <div class="progress-notes">
                <article class="lesson-item">
                  <strong>Improvement signal</strong>
                  <span>{if model.SelectedGameId = "pattern-recall" then
                             "Compare your latest ranked score with your best known score and see whether you are clearing more grids before time expires."
                         else
                             "Compare your latest ranked score with your best known score and try to reduce total moves."}</span>
                </article>
                <article class="lesson-item">
                  <strong>Consistency signal</strong>
                  <span>Your streak shows whether you are maintaining repetition, which matters more than any single great run.</span>
                </article>
                <article class="lesson-item">
                  <strong>Next data gap</strong>
                  <span>To chart true progress over time, the client will need a history endpoint or attempt timeline instead of only the latest profile summary.</span>
                </article>
              </div>
            </section>
          </section>
        </section>
        """

    renderShell
        model
        "Progress over time"
        "See whether your play is improving, how consistent your habit is, and what your current profile says about your trajectory."
        body

let render model dispatch =
    document.title <- $"{appName model} | {gameName model}"

    match document.getElementById "app" with
    | null -> console.error ("Brain Games could not find the #app mount node.")
    | mountPoint ->
        mountPoint.innerHTML <-
            match model.CurrentPage with
            | Play -> renderPlayView model
            | Leaderboard -> renderLeaderboardView model
            | Progress -> renderProgressView model

        let bindClick id message =
            match document.getElementById id with
            | null -> ()
            | element -> element.addEventListener ("click", (fun _ -> dispatch message))

        bindClick "nav-play" GoToPlay
        bindClick "nav-leaderboard" GoToLeaderboard
        bindClick "nav-progress" GoToProgress
        bindClick "new-game-button" NewGame
        bindClick "game-memory-match" (SelectGame "memory-match")
        bindClick "game-pattern-recall" (SelectGame "pattern-recall")

        model.MemoryMatch.Cards
        |> List.iter (fun card ->
            match document.getElementById $"card-{card.Index}" with
            | null -> ()
            | element -> element.addEventListener ("click", (fun _ -> dispatch (FlipCard card.Index))))

        model.PatternRecall.Tiles
        |> List.iter (fun tile ->
            match document.getElementById $"recall-{tile.Index}" with
            | null -> ()
            | element -> element.addEventListener ("click", (fun _ -> dispatch (RecallCellClicked tile.Index))))
