module App.Save

open System.IO
open System.Text.Json

type SaveData = {
    Vidas: int
    Kills: int
}

let savePath = "savegame.json"

let saveGame vidas kills =
    let data = { Vidas = vidas; Kills = kills }
    let json = JsonSerializer.Serialize data
    File.WriteAllText (savePath, json)

let loadGame () =
    if File.Exists savePath then
        try
            let json = File.ReadAllText savePath
            let data = JsonSerializer.Deserialize<SaveData> json
            Some (data.Vidas, data.Kills)
        with
        | _ -> None
    else
        None

let deleteSave () =
    if File.Exists savePath then
        File.Delete savePath
