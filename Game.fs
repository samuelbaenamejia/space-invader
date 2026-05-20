module App.Game

open System
open System.IO
open System.Threading
open App.Types
open App.Utils

// ========== INICIAL ==========

let initialState = {
    PlayerX = START_X; PlayerY = START_Y
    Lives = MAX_LIVES; Score = 0
    PlayerBullets = []; EnemyBullets = []; Enemies = []
    Explosions = []
    Tick = -1; RedrawScreen = true
    Kills = 0; TotalKills = KILLS_TO_WIN
    Invulnerable = 0; GameOver = false; Victory = false; Paused = false
}

let saveFile = "savegame.txt"

// ========== TICK ==========

let updateTick state =
    { state with
        Tick = state.Tick + 1
        Invulnerable = max 0 (state.Invulnerable - 1) }

// ========== TECLADO ==========

let movePlayer key state =
    let ns =
        match key with
        | ConsoleKey.UpArrow | ConsoleKey.W ->
            { state with PlayerY = max GAME_TOP (state.PlayerY - 1) }
        | ConsoleKey.DownArrow | ConsoleKey.S ->
            { state with PlayerY = min (GAME_BOTTOM - 1) (state.PlayerY + 1) }
        | ConsoleKey.LeftArrow | ConsoleKey.A ->
            { state with PlayerX = max 1 (state.PlayerX - 2) }
        | ConsoleKey.RightArrow | ConsoleKey.D ->
            { state with PlayerX = min (GAME_WIDTH - 3) (state.PlayerX + 2) }
        | _ -> state
    if ns <> state then { ns with RedrawScreen = true } else state

let playerShoot key state =
    if key = ConsoleKey.Spacebar && state.PlayerBullets.Length < 3 then
        { state with
            PlayerBullets = { X = state.PlayerX + 2; Y = state.PlayerY } :: state.PlayerBullets
            RedrawScreen = true }
    else state

let processKeyboard state =
    if Console.KeyAvailable then
        let k = Console.ReadKey(true)
        match k.Key with
        | ConsoleKey.P -> { state with Paused = true }
        | _ -> state |> movePlayer k.Key |> playerShoot k.Key
    else state

// ========== ENEMIGOS ==========

let spawnEnemy state =
    if state.Tick > 0 && state.Tick % 55 = 0 && state.Enemies.Length < 6 then
        let r = Random()
        { state with
            Enemies = {
                X = GAME_WIDTH - 3
                Y = r.Next(GAME_TOP + 1, GAME_BOTTOM - 2)
                Dir = if r.Next(2) = 0 then 1 else -1
            } :: state.Enemies
            RedrawScreen = true }
    else state

let moveEnemies state =
    if state.Tick % 2 <> 0 then state
    else
        let moved =
            state.Enemies |> List.map (fun e ->
                let nx = e.X - 1
                if nx < 2 then { e with X = 0 }
                else
                    let ny, nd =
                        if state.Tick % 4 = 0 then
                            let ny = e.Y + e.Dir
                            if ny < GAME_TOP || ny > GAME_BOTTOM - 1 then e.Y, -e.Dir
                            else ny, e.Dir
                        else e.Y, e.Dir
                    { e with X = nx; Y = ny; Dir = nd })
        let alive = moved |> List.filter (fun e -> e.X > 0)
        { state with Enemies = alive; RedrawScreen = true }

let enemyShoot state =
    if state.Tick > 0 && state.Tick % 25 = 0 && state.Enemies.Length > 0 then
        let r = Random()
        let e = state.Enemies.[r.Next(state.Enemies.Length)]
        { state with
            EnemyBullets = { X = e.X - 1; Y = e.Y } :: state.EnemyBullets
            RedrawScreen = true }
    else state

// ========== BALAS ==========

let moveBullets state =
    let pb =
        state.PlayerBullets
        |> List.map (fun b -> { b with X = b.X + 2 })
        |> List.filter (fun b -> b.X < GAME_WIDTH)
    let eb =
        state.EnemyBullets
        |> List.map (fun b -> { b with X = b.X - 2 })
        |> List.filter (fun b -> b.X > 0)
    let changed = pb.Length <> state.PlayerBullets.Length || eb.Length <> state.EnemyBullets.Length
    { state with PlayerBullets = pb; EnemyBullets = eb; RedrawScreen = state.RedrawScreen || changed }

// ========== COLISIONES ==========

let checkCollisions state =
    let (remainingBullets, remainingEnemies, kills, newExplosions) =
        state.PlayerBullets
        |> List.fold (fun (bullets: Bullet list, enemies: Enemy list, k: int, exps: Explosion list) b ->
            match enemies |> List.tryFind (fun e -> abs(b.X - e.X) <= 2 && b.Y = e.Y) with
            | Some e ->
                (bullets, enemies |> List.filter (fun e2 -> e2 <> e), k + 1,
                 { X = e.X; Y = e.Y; Timer = 15 } :: exps)
            | None -> (b :: bullets, enemies, k, exps)
        ) ([], state.Enemies, 0, [])

    let bulletHit =
        state.Invulnerable <= 0 &&
        state.EnemyBullets |> List.exists (fun b ->
            abs(b.Y - state.PlayerY) <= 1 && abs(b.X - state.PlayerX) <= 2)

    let enemyTouch =
        state.Invulnerable <= 0 &&
        remainingEnemies |> List.exists (fun e ->
            abs(e.Y - state.PlayerY) <= 1 && abs(e.X - state.PlayerX) <= 3)

    let s =
        { state with
            PlayerBullets = remainingBullets
            Enemies = remainingEnemies
            Score = state.Score + kills * 10
            Kills = state.Kills + kills
            RedrawScreen = state.RedrawScreen || kills > 0 }

    if bulletHit || enemyTouch then
        let playerExp = { X = state.PlayerX; Y = state.PlayerY; Timer = 15 }
        { s with
            Lives = s.Lives - 1
            Invulnerable = 60
            GameOver = s.Lives - 1 <= 0
            RedrawScreen = true
            Explosions = s.Explosions @ playerExp :: newExplosions }
    else
        { s with Explosions = s.Explosions @ newExplosions }

// ========== EXPLOSIONES ==========

let updateExplosions state =
    let alive =
        state.Explosions
        |> List.map (fun e -> { e with Timer = e.Timer - 1 })
        |> List.filter (fun e -> e.Timer > 0)
    { state with Explosions = alive }

// ========== VICTORIA ==========

let checkWin state =
    if state.Kills >= state.TotalKills then
        { state with Victory = true; RedrawScreen = true }
    else state

// ========== GUARDAR ==========

let saveGame state =
    try
        File.WriteAllLines(saveFile, [| string state.Score; string state.Kills; string state.Lives |])
        true
    with _ -> false

let loadGame () =
    try
        if File.Exists(saveFile) then
            let lines = File.ReadAllLines(saveFile)
            if lines.Length >= 2 then
                let score = int lines.[0]
                let kills = int lines.[1]
                let lives = if lines.Length >= 3 then int lines.[2] else MAX_LIVES
                Some { initialState with Score = score; Kills = kills; Lives = lives }
            else None
        else None
    with _ -> None

// ========== DIBUJO ==========

let drawBorders () =
    for x in 1 .. GAME_WIDTH - 2 do
        drawAt x GAME_TOP ConsoleColor.DarkGray "═"
        drawAt x GAME_BOTTOM ConsoleColor.DarkGray "═"
    for y in GAME_TOP + 1 .. GAME_BOTTOM - 1 do
        drawAt 0 y ConsoleColor.DarkGray "║"
        drawAt (GAME_WIDTH - 1) y ConsoleColor.DarkGray "║"
    drawAt 0 GAME_TOP ConsoleColor.DarkGray "╔"
    drawAt (GAME_WIDTH - 1) GAME_TOP ConsoleColor.DarkGray "╗"
    drawAt 0 GAME_BOTTOM ConsoleColor.DarkGray "╚"
    drawAt (GAME_WIDTH - 1) GAME_BOTTOM ConsoleColor.DarkGray "╝"

let drawHUD state =
    drawAt 2 0 ConsoleColor.White (sprintf "SCORE: %d" state.Score)
    drawAt 25 0 ConsoleColor.Cyan (sprintf "KILLS: %d/%d" state.Kills state.TotalKills)
    drawAt (GAME_WIDTH - 10) 0 ConsoleColor.Black "        "
    for i in 0 .. state.Lives - 1 do
        drawAt (GAME_WIDTH - 10 + i * 2) 0 ConsoleColor.Red "❤️"

let drawFrame state =
    clearGameArea ()
    drawHUD state
    for e in state.Enemies do
        drawAt e.X e.Y ConsoleColor.Red "👾"
    for b in state.PlayerBullets do
        drawAt b.X b.Y ConsoleColor.Yellow "⇒"
    for b in state.EnemyBullets do
        drawAt b.X b.Y ConsoleColor.Red "⇐"
    for e in state.Explosions do
        drawAt e.X e.Y ConsoleColor.Red "💥"
    if state.Invulnerable <= 0 || state.Tick % 4 < 2 then
        drawAt state.PlayerX state.PlayerY ConsoleColor.Green "🚀"

// ========== PANTALLAS ==========

let drawGameOver state =
    Console.Clear()
    drawCentered 8 ConsoleColor.Red "GAME OVER"
    drawCentered 12 ConsoleColor.White (sprintf "Score final: %d" state.Score)
    drawCentered 14 ConsoleColor.White (sprintf "Enemigos eliminados: %d" state.Kills)
    drawCentered 17 ConsoleColor.Cyan "Presiona R para reiniciar"
    drawCentered 18 ConsoleColor.Cyan "Presiona M para menu principal"

let drawVictory state =
    Console.Clear()
    drawCentered 6 ConsoleColor.Green "VICTORIA!"
    drawCentered 8 ConsoleColor.Yellow "Tierra salvada!"
    drawCentered 10 ConsoleColor.White "Los invasores han sido derrotados."
    drawCentered 12 ConsoleColor.White (sprintf "Score final: %d" state.Score)
    drawCentered 14 ConsoleColor.White (sprintf "Enemigos eliminados: %d" state.Kills)
    drawCentered 17 ConsoleColor.Cyan "Presiona R para reiniciar"
    drawCentered 18 ConsoleColor.Cyan "Presiona M para menu principal"

// ========== MENU ==========

let showMenu () =
    let hasSave = File.Exists(saveFile)
    Console.CursorVisible <- false
    let mutable selected = 0
    let options =
        if hasSave then [| "Nueva Partida"; "Continuar Partida"; "Salir" |]
        else [| "Nueva Partida"; "Salir" |]
    let maxIdx = options.Length - 1
    let mutable running = true
    while running do
        Console.Clear()
        drawCentered 4 ConsoleColor.Yellow "SPACE INVADERS"
        drawCentered 6 ConsoleColor.Gray "Defiende la Tierra de los invasores!"
        drawCentered 7 ConsoleColor.DarkGray "WASD/Flechas: mover   Espacio: disparar"
        for i in 0 .. maxIdx do
            if i = selected then
                drawCentered (12 + i * 2) ConsoleColor.Green (sprintf ">> %s <<" options.[i])
            else
                drawCentered (12 + i * 2) ConsoleColor.Cyan options.[i]
        match Console.ReadKey(true).Key with
        | ConsoleKey.UpArrow | ConsoleKey.W -> selected <- max 0 (selected - 1)
        | ConsoleKey.DownArrow | ConsoleKey.S -> selected <- min maxIdx (selected + 1)
        | ConsoleKey.Enter -> running <- false
        | _ -> ()
    Console.Clear()
    selected, hasSave

// ========== BUCLE ==========

let rec mainLoop state =
    if state.Paused then
        drawCentered 10 ConsoleColor.Yellow "=== PAUSA ==="
        drawCentered 12 ConsoleColor.White "P = Continuar"
        drawCentered 13 ConsoleColor.White "S = Guardar partida"
        drawCentered 14 ConsoleColor.White "M = Menu principal"
        wait 33
        if Console.KeyAvailable then
            match Console.ReadKey(true).Key with
            | ConsoleKey.P ->
                clearGameArea ()
                mainLoop { state with Paused = false }
            | ConsoleKey.S ->
                if saveGame state then
                    drawCentered 16 ConsoleColor.Green "Partida guardada!"
                else
                    drawCentered 16 ConsoleColor.Red "Error al guardar!"
                wait 500
                mainLoop state
            | ConsoleKey.M -> ()
            | _ -> mainLoop state
        else mainLoop state
    else
        let s =
            state
            |> updateTick
            |> processKeyboard
            |> spawnEnemy
            |> moveEnemies
            |> enemyShoot
            |> moveBullets
            |> checkCollisions
            |> updateExplosions
            |> checkWin

        drawFrame s
        wait 33

        if s.GameOver then
            drawGameOver s
            wait 33
            if Console.KeyAvailable then
                match Console.ReadKey(true).Key with
                | ConsoleKey.R -> mainLoop initialState
                | ConsoleKey.M -> ()
                | _ -> mainLoop s
            else mainLoop s
        elif s.Victory then
            drawVictory s
            wait 33
            if Console.KeyAvailable then
                match Console.ReadKey(true).Key with
                | ConsoleKey.R -> mainLoop initialState
                | ConsoleKey.M -> ()
                | _ -> mainLoop s
            else mainLoop s
        else mainLoop s

// ========== INICIO ==========

let start () =
    let mutable salir = false
    while not salir do
        let opcion, hasSave = showMenu ()
        if opcion = 0 then
            Console.Clear ()
            drawBorders ()
            mainLoop initialState
        elif hasSave && opcion = 1 then
            match loadGame () with
            | Some s ->
                Console.Clear ()
                drawBorders ()
                mainLoop s
            | None ->
                drawCentered 14 ConsoleColor.Red "Error al cargar partida"
                wait 1000
        else
            salir <- true
            Console.CursorVisible <- true
            printfn "Gracias por jugar!"
            wait 1000
