module BrainGames.Client.Main

open BrainGames.Client.State
open BrainGames.Client.View

let initialModel, initialCommands = init ()

let mutable currentModel = initialModel

let rec dispatch msg =
    let nextModel, commands = update msg currentModel
    currentModel <- nextModel
    render currentModel dispatch
    Cmd.run dispatch commands

render currentModel dispatch
Cmd.run dispatch initialCommands
