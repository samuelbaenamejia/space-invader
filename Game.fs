module App.Game

open System
open System.Threading
open App.Types
open App.Utils

// ========== INICIAL ==========

let initialState = {
    PlayerX = START_X; PlayerY = START_Y
    Lives = MAX_LIVES; Score = 0
    PlayerBullets = []; EnemyBullets = []; Enemies = []
    Tick = -1; RedrawScreen = true
    Kills = 0; TotalKills = KILLS_TO_WIN
    Invulnerable = 0; GameOver = false; Victory = false
}

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
        state |> movePlayer k.Key |> playerShoot k.Key
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
                if nx < 1 then { e with X = 0 }
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
    let (remainingBullets, remainingEnemies, kills) =
        state.PlayerBullets
        |> List.fold (fun (bullets, enemies, k) b ->
            match enemies |> List.tryFind (fun e -> abs(b.X - e.X) <= 2 && b.Y = e.Y) with
            | Some e -> (bullets, enemies |> List.filter (fun e2 -> e2 <> e), k + 1)
            | None -> (b :: bullets, enemies, k)
        ) ([], state.Enemies, 0)

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
        { s with
            Lives = s.Lives - 1
            Invulnerable = 60
            GameOver = s.Lives - 1 <= 0
            RedrawScreen = true }
    else s

// ========== VICTORIA ==========

let checkWin state =
    if state.Kills >= state.TotalKills then
        { state with Victory = true; RedrawScreen = true }
    else state

// ========== DIBUJO ==========

let drawHUD state =
    drawAt 2 0 ConsoleColor.White (sprintf "SCORE: %d" state.Score)
    drawAt 25 0 ConsoleColor.Cyan (sprintf "KILLS: %d/%d" state.Kills state.TotalKills)
    let hearts = String.replicate state.Lives "❤️ "
    let empty = String.replicate (MAX_LIVES - state.Lives) "  "
    drawAt (GAME_WIDTH - 12) 0 ConsoleColor.Red (hearts + empty)

let drawFrame state =
    Console.Clear()
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
    drawHUD state
    for e in state.Enemies do
        drawAt e.X e.Y ConsoleColor.Red "👾"
    for b in state.PlayerBullets do
        drawAt b.X b.Y ConsoleColor.Yellow "|"
    for b in state.EnemyBullets do
        drawAt b.X b.Y ConsoleColor.Red "|"
    if state.Invulnerable <= 0 || state.Tick % 4 < 2 then
        drawAt state.PlayerX state.PlayerY ConsoleColor.Green "🚀"

// ========== PANTALLAS ==========

let drawGameOver state =
    drawCentered 8 ConsoleColor.Red "GAME OVER"
    drawCentered 12 ConsoleColor.White (sprintf "Score final: %d" state.Score)
    drawCentered 14 ConsoleColor.White (sprintf "Enemigos eliminados: %d" state.Kills)
    drawCentered 17 ConsoleColor.Cyan "Presiona R para reiniciar"
    drawCentered 18 ConsoleColor.Cyan "Presiona M para menu principal"

let drawVictory state =
    drawCentered 6 ConsoleColor.Green "VICTORIA!"
    drawCentered 8 ConsoleColor.Yellow "Tierra salvada!"
    drawCentered 10 ConsoleColor.White "Los invasores han sido derrotados."
    drawCentered 12 ConsoleColor.White (sprintf "Score final: %d" state.Score)
    drawCentered 14 ConsoleColor.White (sprintf "Enemigos eliminados: %d" state.Kills)
    drawCentered 17 ConsoleColor.Cyan "Presiona R para reiniciar"
    drawCentered 18 ConsoleColor.Cyan "Presiona M para menu principal"

// ========== MENU ==========

let showMenu () =
    Console.CursorVisible <- false
    let mutable selected = 0
    let mutable running = true
    while running do
        Console.Clear()
        drawCentered 4 ConsoleColor.Yellow "SPACE INVADERS"
        drawCentered 6 ConsoleColor.Gray "Defiende la Tierra de los invasores!"
        drawCentered 7 ConsoleColor.DarkGray "WASD/Flechas: mover   Espacio: disparar"
        for i in 0..1 do
            let opts = [| "Nueva Partida"; "Salir" |]
            if i = selected then
                drawCentered (12 + i * 2) ConsoleColor.Green (sprintf ">> %s <<" opts.[i])
            else
                drawCentered (12 + i * 2) ConsoleColor.Cyan opts.[i]
        match Console.ReadKey(true).Key with
        | ConsoleKey.UpArrow | ConsoleKey.W -> selected <- max 0 (selected - 1)
        | ConsoleKey.DownArrow | ConsoleKey.S -> selected <- min 1 (selected + 1)
        | ConsoleKey.Enter -> running <- false
        | _ -> ()
    Console.Clear()
    selected

// ========== BUCLE ==========

let rec mainLoop state =
    let s =
        state
        |> updateTick
        |> processKeyboard
        |> spawnEnemy
        |> moveEnemies
        |> enemyShoot
        |> moveBullets
        |> checkCollisions
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
        match showMenu () with
        | 1 ->
            salir <- true
            Console.CursorVisible <- true
            printfn "Gracias por jugar!"
            wait 1000
        | _ -> mainLoop initialState
