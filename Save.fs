/// Módulo de guardado - Guarda y carga la partida en un archivo JSON
module App.Save

open System.IO
open System.Text.Json

/// Estructura de datos que se guarda en el archivo
type SaveData = {
    Vidas: int
    Kills: int
}

let savePath = "savegame.json"

/// Guarda las vidas y kills actuales en un archivo JSON
let saveGame vidas kills =
    let data = { Vidas = vidas; Kills = kills }
    let json = JsonSerializer.Serialize data
    File.WriteAllText (savePath, json)

/// Carga los datos guardados desde el archivo JSON
/// Devuelve None si no existe el archivo o hay error
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

/// Elimina el archivo de guardado (al empezar partida nueva o al terminar)
let deleteSave () =
    if File.Exists savePath then
        File.Delete savePath
