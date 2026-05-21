module App.Menu

open System
open App.Utils
open App.Types

let titleArt = [|
    @"██╗  ██╗██╗██╗     ██╗         ███████╗   ████████╗ "
    @"██║ ██╔╝██║██║     ██║         ██╔════╝   ╚══██╔══╝ "
    @"█████╔╝ ██║██║     ██║         █████╗        ██║    "
    @"██╔═██╗ ██║██║     ██║         ██╔══╝        ██║    "
    @"██║  ██╗██║███████╗███████╗    ███████╗██╗   ██║   ██╗"
    @"╚═╝  ╚═╝╚═╝╚══════╝╚══════╝    ╚══════╝╚═╝   ╚═╝   ╚═╝"
|]

let gameOverArt = [|
    "██████╗  █████╗ ███╗   ███╗███████╗     ██████╗ ██╗   ██╗███████╗██████╗ "
    "██╔════╝ ██╔══██╗████╗ ████║██╔════╝    ██╔═══██╗██║   ██║██╔════╝██╔══██╗"
    "██║  ███╗███████║██╔████╔██║█████╗      ██║   ██║██║   ██║█████╗  ██████╔╝"
    "██║   ██║██╔══██║██║╚██╔╝██║██╔══╝      ██║   ██║╚██╗ ██╔╝██╔══╝  ██╔══██╗"
    "╚██████╔╝██║  ██║██║ ╚═╝ ██║███████╗    ╚██████╔╝ ╚████╔╝ ███████╗██║  ██║"
    " ╚═════╝ ╚═╝  ╚═╝╚═╝     ╚═╝╚══════╝     ╚═════╝   ╚═══╝  ╚══════╝╚═╝  ╚═╝"
|]

let victoryArt = [|
    "██╗   ██╗██╗ ██████╗████████╗ ██████╗ ██████╗ ██╗ █████╗ "
    "██║   ██║██║██╔════╝╚══██╔══╝██╔═══██╗██╔══██╗██║██╔══██╗"
    "██║   ██║██║██║        ██║   ██║   ██║██████╔╝██║███████║"
    "╚██╗ ██╔╝██║██║        ██║   ██║   ██║██╔══██╗██║██╔══██║"
    " ╚████╔╝ ██║╚██████╗   ██║   ╚██████╔╝██║  ██║██║██║  ██║"
    "  ╚═══╝  ╚═╝ ╚═════╝   ╚═╝    ╚═════╝ ╚═╝  ╚═╝╚═╝╚═╝  ╚═╝"
|]

let coffinArt = [|
    @"              _____     "
    @"             /     \    "
    @"            | () () |   "
    @"             \  ^  /    "
    @"              |||_|     "
    @"              |   |     "
    @"              |RIP|     "
    @"              |E.T|     "
    @"             /_____\    "
|]

let drawBorder () =
    let w = Console.BufferWidth - 1
    let h = Console.BufferHeight - 1
    Console.ForegroundColor <- ConsoleColor.DarkBlue
    let top = "╔" + String.replicate (w-1) "═" + "╗"
    let bot = "╚" + String.replicate (w-1) "═" + "╝"
    displayMessage 0 0 ConsoleColor.DarkBlue top
    for y in 1 .. h-1 do
        displayMessage 0 y ConsoleColor.DarkBlue "║"
        displayMessage w y ConsoleColor.DarkBlue "║"
    displayMessage 0 h ConsoleColor.DarkBlue bot

let drawStars () =
    let rng = Random 42
    for _ in 1 .. 40 do
        let x = rng.Next(1, Console.BufferWidth - 2)
        let y = rng.Next(1, Console.BufferHeight - 2)
        let star = if rng.Next(2) = 0 then "." else "·"
        displayMessage x y ConsoleColor.DarkGray star

let drawInstructions () =
    let y = Console.BufferHeight - 4
    let text1 = "WASD/FLECHAS -> MOVER"
    let text2 = "ESPACIO -> DISPARAR"
    let text3 = "P -> PAUSA"
    let cx = Console.BufferWidth / 2
    displayMessage (cx - text1.Length / 2) y ConsoleColor.DarkGray text1
    displayMessage (cx - text2.Length / 2) (y + 1) ConsoleColor.DarkGray text2
    displayMessage (cx - text3.Length / 2) (y + 2) ConsoleColor.DarkGray text3

let mostrarConMenu (titleLines : string[]) artY drawExtra (commands : (Command * string) array) =
    safeCursorVisible false
    let cx = Console.BufferWidth / 2
    let optionsX = cx - 7
    let optionsY = artY + titleLines.Length + 2

    let mutable seleccion = 0

    let rec loop () =
        safeClear ()
        drawBorder ()
        drawStars ()
        titleLines |> Array.iteri (fun i (line : string) ->
            displayMessage (cx - line.Length / 2) (artY + i) ConsoleColor.Yellow line
        )
        drawExtra ()

        commands |> Array.iteri (fun i (_, text) ->
            let color = if i = seleccion then ConsoleColor.Yellow else ConsoleColor.Cyan
            displayMessage optionsX (optionsY + i) color text
        )
        displayMessage (optionsX - 2) (optionsY + seleccion) ConsoleColor.Yellow ">"

        let k = Console.ReadKey true
        match k.Key with
        | ConsoleKey.UpArrow -> seleccion <- max 0 (seleccion - 1); loop ()
        | ConsoleKey.DownArrow -> seleccion <- min (commands.Length - 1) (seleccion + 1); loop ()
        | ConsoleKey.Enter -> fst commands.[seleccion]
        | _ -> loop ()

    let resultado = loop ()
    safeCursorVisible true
    safeClear ()
    resultado

let mostrarPrincipal () =
    mostrarConMenu titleArt 3 drawInstructions [|
        NuevaPartida, "NUEVA PARTIDA"
        Continuar, "CONTINUAR"
        Salir, "SALIR"
    |]

let mostrarGameOver kills =
    let artY = Console.BufferHeight / 2 - 10
    let cx = Console.BufferWidth / 2
    let score = kills * 100
    let drawExtra () =
        displayMessage (cx - 12) (artY + 7) ConsoleColor.Cyan (sprintf "KILLS: %d     SCORE: %d" kills score)
    mostrarConMenu gameOverArt artY drawExtra [|
        Reintentar, "REINICIAR"
        Salir, "SALIR"
    |]

let mostrarVictoria kills =
    let artY = Console.BufferHeight / 2 - 8
    let cx = Console.BufferWidth / 2
    let score = kills * 100
    let drawExtra () =
        displayMessage (cx - 12) (artY + 7) ConsoleColor.Cyan (sprintf "KILLS: %d     SCORE: %d" kills score)
    mostrarConMenu victoryArt artY drawExtra [|
        Reintentar, "REINICIAR"
        Salir, "SALIR"
    |]
