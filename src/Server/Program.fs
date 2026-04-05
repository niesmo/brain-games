open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open BrainGames.Server

let builder = WebApplication.CreateBuilder()

builder.Services.AddCors(fun options ->
    options.AddDefaultPolicy(fun policy ->
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore))
|> ignore

let appSettings = Configuration.loadAppSettings builder.Configuration
let supabaseSettings = Configuration.loadSupabaseSettings builder.Configuration

builder.Services.AddSingleton(BrainGamesStore(appSettings, supabaseSettings))
|> ignore

let app = builder.Build()

app.UseCors()
|> ignore
app.UseHttpsRedirection()
|> ignore

Endpoints.map app

app.Run()
