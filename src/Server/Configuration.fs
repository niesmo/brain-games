namespace BrainGames.Server

open System
open Microsoft.Extensions.Configuration

type AppSettings =
    { Name: string
      Tagline: string }

type SupabaseSettings =
    { Enabled: bool
      ProjectUrl: string
      AnonKey: string
      ServiceRoleKey: string
      StorageBucket: string }

[<RequireQualifiedAccess>]
module Configuration =
    let loadAppSettings (configuration: IConfiguration) =
        let appConfig = configuration.GetSection("App")

        { Name = appConfig["Name"] |> Option.ofObj |> Option.defaultValue "Brain Games"
          Tagline =
            appConfig["Tagline"]
            |> Option.ofObj
            |> Option.defaultValue "Playful practice for sharper thinking." }

    let loadSupabaseSettings (configuration: IConfiguration) =
        let supabaseConfig = configuration.GetSection("Supabase")

        { Enabled =
            supabaseConfig["Enabled"]
            |> Option.ofObj
            |> Option.map Boolean.Parse
            |> Option.defaultValue false
          ProjectUrl = supabaseConfig["ProjectUrl"] |> Option.ofObj |> Option.defaultValue ""
          AnonKey = supabaseConfig["AnonKey"] |> Option.ofObj |> Option.defaultValue ""
          ServiceRoleKey = supabaseConfig["ServiceRoleKey"] |> Option.ofObj |> Option.defaultValue ""
          StorageBucket =
            supabaseConfig["StorageBucket"]
            |> Option.ofObj
            |> Option.defaultValue "lesson-assets" }
