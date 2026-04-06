module BrainGames.Client.Api

open System
open Browser.Dom
open BrainGames.Shared
open Fable.SimpleHttp
open Thoth.Json

type private ApiError = { error: string }

let private registrationStorageKey = "brain-games.registration-request"

let private apiBaseUrl () =
    let host =
        if String.IsNullOrWhiteSpace window.location.hostname then
            "localhost"
        else
            window.location.hostname

    $"http://{host}:5189/api"

let private buildUrl path = $"{apiBaseUrl ()}{path}"

let private decode<'T> (body: string) =
    Decode.Auto.fromString<'T>(body)
    |> Result.mapError (fun error -> $"Unable to decode server response: {error}")

let private errorMessage statusCode body =
    match Decode.Auto.fromString<ApiError>(body) with
    | Ok apiError when not (String.IsNullOrWhiteSpace apiError.error) -> apiError.error
    | _ when String.IsNullOrWhiteSpace body -> $"The request failed with status {statusCode}."
    | _ -> $"The request failed with status {statusCode}: {body}"

let private get<'T> path =
    async {
        try
            let! statusCode, body = Http.get (buildUrl path)

            return
                if statusCode >= 200 && statusCode < 300 then
                    decode<'T> body
                else
                    Error(errorMessage statusCode body)
        with error ->
            return Error error.Message
    }

let private post<'TRequest, 'TResponse> path (payload: 'TRequest) =
    async {
        try
            let requestBody = Encode.Auto.toString(0, payload)

            let! response =
                Http.request (buildUrl path)
                |> Http.method POST
                |> Http.headers
                    [ Headers.contentType "application/json"
                      Headers.accept "application/json" ]
                |> Http.content (BodyContent.Text requestBody)
                |> Http.send

            let statusCode = response.statusCode
            let body = response.responseText

            return
                if statusCode >= 200 && statusCode < 300 then
                    decode<'TResponse> body
                else
                    Error(errorMessage statusCode body)
        with error ->
            return Error error.Message
    }

let loadBoot () = get<BootResponse> "/boot"

let loadLeaderboard gameId = get<LeaderboardEntry array> $"/leaderboard/{gameId}"

let loadProfile playerId = get<ProfileSnapshot> $"/profile/{playerId}"

let registerPlayer request = post<RegisterPlayerRequest, RegisterPlayerResponse> "/players/register" request

let submitScore request = post<ScoreSubmissionRequest, ScoreSubmissionResult> "/scores" request

let tryLoadStoredRegistration () =
    match window.localStorage.getItem registrationStorageKey with
    | null -> None
    | raw ->
        match Decode.Auto.fromString<RegisterPlayerRequest>(raw) with
        | Ok registration -> Some registration
        | Error _ -> None

let saveRegistration (request: RegisterPlayerRequest) =
    let raw = Encode.Auto.toString(0, request)
    window.localStorage.setItem(registrationStorageKey, raw)

let ensureGuestRegistration () =
    match tryLoadStoredRegistration () with
    | Some registration -> registration
    | None ->
        let suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss")

        let registration =
            { DisplayName = $"Guest {suffix.Substring(suffix.Length - 4)}"
              Email = $"guest-{suffix}@local.brain-games.dev"
              CountryCode = "US" }

        saveRegistration registration
        registration
