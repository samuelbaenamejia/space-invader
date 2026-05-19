module SpaceInvaders.Utils

open System

let GAME_WIDTH = 80
let GAME_HEIGHT = 24
let PLAYER_ROW = 20
let GAME_TOP = 2
let GAME_BOTTOM = PLAYER_ROW + 2

let GAME_OFFSET_X =
    try max 0 ((Console.WindowWidth - GAME_WIDTH) / 2)
    with _ -> 0

let drawGameAt (x: int) (y: int) (color: ConsoleColor) (text: string) =
    try
        Console.SetCursorPosition(x + GAME_OFFSET_X, y)
        Console.ForegroundColor <- color
        Console.Write(text)
    with _ -> ()

let drawCentered (y: int) (color: ConsoleColor) (text: string) =
    let x = max 0 ((Console.WindowWidth - text.Length) / 2)
    try
        Console.SetCursorPosition(x, y)
        Console.ForegroundColor <- color
        Console.Write(text)
    with _ -> ()

let clearGameArea () =
    for y in GAME_TOP .. GAME_BOTTOM do
        drawGameAt 1 y ConsoleColor.Black (String(' ', GAME_WIDTH - 2))

let playBeep (freq: int) (dur: int) =
    try Console.Beep(freq, dur) with _ -> ()

let wait (ms: int) =
    Threading.Thread.Sleep(ms)
