module App.Game

open System
open System.IO
open System.Threading
open App.Types
open App.Utils

let TOTAL_KILLS = 15
let SAVE_FILE = "savegame.txt"

let estadoInicial = {
    ProgramState = Running
    PlayerX = START_X; PlayerY = START_Y
    Lives = MAX_LIVES; Score = 0
    PlayerBullets = []; EnemyBullets = []; Enemies = []
    Explosions = []
    Tick = -1; RedrawScreen = true
    Kills = 0; Invulnerable = 0
}

// ==================== TICK ====================

let actualizarTick state =
    { state with
        Tick = state.Tick + 1
        Invulnerable = max 0 (state.Invulnerable - 1) }

// ==================== TECLADO ====================

let moverJugador key state =
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
    |> fun ns -> if ns <> state then { ns with RedrawScreen = true } else state

let dispararJugador key state =
    if key = ConsoleKey.Spacebar && state.PlayerBullets.Length < 3 then
        { state with
            PlayerBullets = { X = state.PlayerX + 2; Y = state.PlayerY } :: state.PlayerBullets
            RedrawScreen = true }
    else state

let procesarTeclado state =
    if state.ProgramState <> Running then state
    elif Console.KeyAvailable then
        let k = Console.ReadKey(true)
        match k.Key with
        | ConsoleKey.P -> { state with ProgramState = Paused }
        | _ -> state |> moverJugador k.Key |> dispararJugador k.Key
    else state

// ==================== ENEMIGOS ====================

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

let moverEnemigos state =
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
        { state with Enemies = moved |> List.filter (fun e -> e.X > 0); RedrawScreen = true }

let disparoEnemigo state =
    if state.Tick > 0 && state.Tick % 25 = 0 && state.Enemies.Length > 0 then
        let r = Random()
        let e = state.Enemies.[r.Next(state.Enemies.Length)]
        { state with
            EnemyBullets = { X = e.X - 1; Y = e.Y } :: state.EnemyBullets
            RedrawScreen = true }
    else state

// ==================== BALAS ====================

let moverBalas state =
    let pb = state.PlayerBullets |> List.map (fun b -> { b with X = b.X + 2 }) |> List.filter (fun b -> b.X < GAME_WIDTH)
    let eb = state.EnemyBullets |> List.map (fun b -> { b with X = b.X - 2 }) |> List.filter (fun b -> b.X > 0)
    let changed = pb.Length <> state.PlayerBullets.Length || eb.Length <> state.EnemyBullets.Length
    { state with PlayerBullets = pb; EnemyBullets = eb; RedrawScreen = state.RedrawScreen || changed }

// ==================== COLISIONES ====================

let detectarColisiones state =
    let (balasRestantes, enemigosRestantes, kills, nuevasExplosiones) =
        state.PlayerBullets
        |> List.fold (fun (bullets: Bullet list, enemies: Enemy list, k: int, exps: Explosion list) b ->
            match enemies |> List.tryFind (fun e -> abs(b.X - e.X) <= 2 && b.Y = e.Y) with
            | Some e ->
                (bullets, enemies |> List.filter (fun e2 -> e2 <> e), k + 1,
                 { X = e.X; Y = e.Y; Timer = 15 } :: exps)
            | None -> (b :: bullets, enemies, k, exps)
        ) ([], state.Enemies, 0, [])

    let jugadorGolpeado =
        state.Invulnerable <= 0 && (
            state.EnemyBullets |> List.exists (fun b -> abs(b.Y - state.PlayerY) <= 1 && abs(b.X - state.PlayerX) <= 2) ||
            enemigosRestantes |> List.exists (fun e -> abs(e.Y - state.PlayerY) <= 1 && abs(e.X - state.PlayerX) <= 3)
        )

    let s =
        { state with
            PlayerBullets = balasRestantes
            Enemies = enemigosRestantes
            Score = state.Score + kills * 10
            Kills = state.Kills + kills
            Explosions = state.Explosions @ nuevasExplosiones
            RedrawScreen = state.RedrawScreen || kills > 0 || jugadorGolpeado }

    if jugadorGolpeado then
        { s with
            Lives = s.Lives - 1
            Invulnerable = 60
            ProgramState = if s.Lives - 1 <= 0 then GameOver else Running
            Explosions = { X = state.PlayerX; Y = state.PlayerY; Timer = 15 } :: s.Explosions
            RedrawScreen = true }
    else s

// ==================== EXPLOSIONES ====================

let actualizarExplosiones state =
    { state with
        Explosions =
            state.Explosions
            |> List.map (fun e -> { e with Timer = e.Timer - 1 })
            |> List.filter (fun e -> e.Timer > 0) }

// ==================== VICTORIA ====================

let verificarVictoria state =
    if state.Kills >= TOTAL_KILLS then
        { state with ProgramState = Victory; RedrawScreen = true }
    else state

// ==================== GUARDAR ====================

let guardarPartida state =
    try File.WriteAllLines(SAVE_FILE, [| string state.Score; string state.Kills; string state.Lives |]); true
    with _ -> false

let cargarPartida () =
    try
        if File.Exists(SAVE_FILE) then
            let lines = File.ReadAllLines(SAVE_FILE)
            if lines.Length >= 2 then
                let score = int lines.[0]
                let kills = int lines.[1]
                let lives = if lines.Length >= 3 then int lines.[2] else MAX_LIVES
                Some { estadoInicial with Score = score; Kills = kills; Lives = lives }
            else None
        else None
    with _ -> None

// ==================== DIBUJO ====================

let dibujarBordes () =
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

let dibujarHUD state =
    drawAt 2 0 ConsoleColor.White (sprintf "SCORE: %d" state.Score)
    drawAt 25 0 ConsoleColor.Cyan (sprintf "KILLS: %d/%d" state.Kills TOTAL_KILLS)
    drawAt (GAME_WIDTH - 10) 0 ConsoleColor.Black "        "
    for i in 0 .. state.Lives - 1 do
        drawAt (GAME_WIDTH - 10 + i * 2) 0 ConsoleColor.Red "❤️"

let redibujarPantalla state =
    if state.RedrawScreen then
        clearGameArea ()
        dibujarHUD state
        for e in state.Enemies do drawAt e.X e.Y ConsoleColor.Red "👾"
        for b in state.PlayerBullets do drawAt b.X b.Y ConsoleColor.Yellow "⇒"
        for b in state.EnemyBullets do drawAt b.X b.Y ConsoleColor.Red "⇐"
        for e in state.Explosions do drawAt e.X e.Y ConsoleColor.Red "💥"
        if state.Invulnerable <= 0 || state.Tick % 4 < 2 then
            drawAt state.PlayerX state.PlayerY ConsoleColor.Green "🚀"
        { state with RedrawScreen = false }
    else state

// ==================== PANTALLAS ====================

let dibujarGameOver state =
    Console.Clear()
    drawCentered 8 ConsoleColor.Red "GAME OVER"
    drawCentered 12 ConsoleColor.White (sprintf "Score final: %d" state.Score)
    drawCentered 14 ConsoleColor.White (sprintf "Enemigos eliminados: %d" state.Kills)
    drawCentered 17 ConsoleColor.Cyan "R = Reiniciar    M = Menu"

let dibujarVictoria state =
    Console.Clear()
    drawCentered 6 ConsoleColor.Green "VICTORIA!"
    drawCentered 8 ConsoleColor.Yellow "Tierra salvada!"
    drawCentered 10 ConsoleColor.White "Los invasores han sido derrotados."
    drawCentered 12 ConsoleColor.White (sprintf "Score final: %d" state.Score)
    drawCentered 14 ConsoleColor.White (sprintf "Enemigos eliminados: %d" state.Kills)
    drawCentered 17 ConsoleColor.Cyan "R = Reiniciar    M = Menu"

let dibujarPausa state =
    drawCentered 10 ConsoleColor.Yellow "=== PAUSA ==="
    drawCentered 12 ConsoleColor.White "P = Continuar"
    drawCentered 13 ConsoleColor.White "S = Guardar"
    drawCentered 14 ConsoleColor.White "M = Menu"

// ==================== MENU ====================

let mostrarMenu () =
    let hayGuardado = File.Exists(SAVE_FILE)
    Console.CursorVisible <- false
    let mutable selected = 0
    let opciones =
        if hayGuardado then [| "Nueva Partida"; "Continuar Partida"; "Salir" |]
        else [| "Nueva Partida"; "Salir" |]
    let mutable running = true
    while running do
        Console.Clear()
        drawCentered 4 ConsoleColor.Yellow "SPACE INVADERS"
        drawCentered 6 ConsoleColor.Gray "WASD/Flechas: mover   Espacio: disparar"
        for i in 0 .. opciones.Length - 1 do
            if i = selected then drawCentered (12 + i * 2) ConsoleColor.Green (sprintf ">> %s <<" opciones.[i])
            else drawCentered (12 + i * 2) ConsoleColor.Cyan opciones.[i]
        match Console.ReadKey(true).Key with
        | ConsoleKey.UpArrow | ConsoleKey.W -> selected <- max 0 (selected - 1)
        | ConsoleKey.DownArrow | ConsoleKey.S -> selected <- min (opciones.Length - 1) (selected + 1)
        | ConsoleKey.Enter -> running <- false
        | _ -> ()
    Console.Clear()
    selected, hayGuardado

// ==================== BUCLE PRINCIPAL ====================

let rec buclePrincipal state =
    match state.ProgramState with
    | Running ->
        let s =
            state
            |> actualizarTick
            |> procesarTeclado
            |> spawnEnemy
            |> moverEnemigos
            |> disparoEnemigo
            |> moverBalas
            |> detectarColisiones
            |> actualizarExplosiones
            |> verificarVictoria
            |> redibujarPantalla
        wait 33
        buclePrincipal s

    | Paused ->
        dibujarPausa state
        wait 33
        if Console.KeyAvailable then
            match Console.ReadKey(true).Key with
            | ConsoleKey.P -> buclePrincipal { state with ProgramState = Running; RedrawScreen = true }
            | ConsoleKey.S ->
                if guardarPartida state then drawCentered 16 ConsoleColor.Green "Guardado!"
                else drawCentered 16 ConsoleColor.Red "Error!"
                wait 500
                buclePrincipal state
            | ConsoleKey.M -> ()
            | _ -> buclePrincipal state
        else buclePrincipal state

    | GameOver ->
        dibujarGameOver state
        wait 33
        if Console.KeyAvailable then
            match Console.ReadKey(true).Key with
            | ConsoleKey.R -> buclePrincipal { estadoInicial with RedrawScreen = true }
            | ConsoleKey.M -> ()
            | _ -> buclePrincipal state
        else buclePrincipal state

    | Victory ->
        dibujarVictoria state
        wait 33
        if Console.KeyAvailable then
            match Console.ReadKey(true).Key with
            | ConsoleKey.R -> buclePrincipal { estadoInicial with RedrawScreen = true }
            | ConsoleKey.M -> ()
            | _ -> buclePrincipal state
        else buclePrincipal state

    | Terminated -> ()

// ==================== INICIO ====================

let start () =
    let mutable salir = false
    while not salir do
        match mostrarMenu () with
        | 0, _ ->
            Console.Clear(); dibujarBordes ()
            buclePrincipal estadoInicial
        | 1, true ->
            match cargarPartida () with
            | Some s -> Console.Clear(); dibujarBordes (); buclePrincipal s
            | None -> drawCentered 14 ConsoleColor.Red "Error al cargar"; wait 1000
        | _, false | _, true ->
            salir <- true
            Console.CursorVisible <- true
            printfn "Gracias por jugar!"; wait 1000
