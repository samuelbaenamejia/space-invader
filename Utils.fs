module App.Utils

open System

let GAME_WIDTH = 80
let GAME_HEIGHT = 24
let GAME_TOP = 2
let GAME_BOTTOM = 22
let START_X = 10
let START_Y = 12
let MAX_LIVES = 3
let KILLS_TO_WIN = 15

let drawAt x y color (msg: string) =
    try
        Console.SetCursorPosition(x, y)
        Console.ForegroundColor <- color
        Console.Write(msg)
    with _ -> ()

let drawCentered y color (msg: string) =
    let x = max 0 ((Console.WindowWidth - msg.Length) / 2)
    drawAt x y color msg

let wait (ms: int) =
    Threading.Thread.Sleep(ms)
