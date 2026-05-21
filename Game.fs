/// Módulo del juego - Lógica principal: movimiento, colisiones, dibujo y pausa
module App.Game

open System
open System.Threading
open App.Utils
open App.Types
open App.Save

/// Estado de la pantalla actual del juego
type GameScreen =
| Playing   /// El juego se está ejecutando
| Paused    /// El juego está en pausa
| GameOver  /// El jugador perdió todas las vidas
| Victory   /// El jugador ganó (10 kills)
| Quit      /// Volver al menú principal

/// Estado del jugador (vivo o explotando)
type PlayerState =
| Alive
| Exploding

/// Enemigo: posición, dirección vertical y si está vivo
type Enemy = {
    X: int
    Y: int
    Dir: int       /// Dirección vertical: 1 = abajo, -1 = arriba
    Alive: bool
    HitTick: int   /// Tick en que fue golpeado (para animación de explosión)
}

/// Misil del jugador o del enemigo
type Missile = {
    X: int
    Y: int
}

/// Estado completo del juego en un momento dado
type GameState = {
    Screen: GameScreen
    PlayerX: int
    PlayerY: int
    PlayerState: PlayerState
    PlayerHitTick: int
    Vidas: int
    Kills: int
    Enemies: Enemy list
    PlayerMissiles: Missile list
    EnemyMissiles: Missile list
    Tick: int          /// Contador de ciclos del juego (aumenta en cada frame)
    RedrawScreen: bool /// Si es true, se redibuja la pantalla en el siguiente frame
    ShootCooldown: int /// Tiempo de espera entre disparos del jugador
}

/// Obtiene el ancho útil de la pantalla
let screenW () = Console.BufferWidth - 1
let screenH () = Console.BufferHeight - 1

/// Crea un nuevo enemigo en una posición aleatoria en el lado derecho
let createEnemy () =
    let sw = screenW ()
    let rng = Random ()
    { X = sw - 5
      Y = rng.Next(2, Console.BufferHeight - 2)
      Dir = if rng.Next(2) = 0 then 1 else -1
      Alive = true
      HitTick = -1 }

/// Crea el estado inicial del juego con las vidas y kills indicados
let initialState vidas kills =
    let sh = screenH ()
    {
        Screen = Playing
        PlayerX = 4            /// Jugador siempre empieza a la izquierda
        PlayerY = sh / 2
        PlayerState = Alive
        PlayerHitTick = -1
        Vidas = vidas
        Kills = kills
        Enemies = [createEnemy ()]
        PlayerMissiles = []
        EnemyMissiles = []
        Tick = -1
        RedrawScreen = true
        ShootCooldown = 0
    }

/// Incrementa el contador de ticks en cada ciclo del juego
let updateTick state =
    { state with Tick = state.Tick + 1 }

/// Reduce el cooldown de disparo del jugador
let updateShootCooldown state =
    if state.ShootCooldown > 0 then
        { state with ShootCooldown = state.ShootCooldown - 1 }
    else
        state

/// Mueve los misiles del jugador hacia la derecha y elimina los que salen de la pantalla
let updatePlayerMissiles state =
    let sw = screenW ()
    if state.PlayerMissiles <> [] then
        let nuevos =
            state.PlayerMissiles
            |> List.map (fun m -> { m with X = m.X + 1 })
            |> List.filter (fun m -> m.X < sw - 1)
        if nuevos <> state.PlayerMissiles then
            { state with PlayerMissiles = nuevos; RedrawScreen = true }
        else
            state
    else
        state

/// Mueve los misiles enemigos hacia la izquierda y elimina los que salen de la pantalla
let updateEnemyMissiles state =
    if state.EnemyMissiles <> [] then
        let nuevos =
            state.EnemyMissiles
            |> List.map (fun m -> { m with X = m.X - 1 })
            |> List.filter (fun m -> m.X > 0)
        if nuevos <> state.EnemyMissiles then
            { state with EnemyMissiles = nuevos; RedrawScreen = true }
        else
            state
    else
        state

/// Actualiza la posición de los enemigos:
/// - Cada 4 ticks: rebote vertical (cambia Y, rebota en los bordes)
/// - Cada 12 ticks: avance horizontal hacia la izquierda (mínimo X=10)
let updateEnemies state =
    let maxY = Console.BufferHeight - 1
    state.Enemies
    |> List.map (fun e ->
        if e.Alive then
            let (newY, newDir) =
                if state.Tick % 4 = 0 then
                    let ny = e.Y + e.Dir
                    if ny > maxY then (maxY, -1)
                    elif ny < 0 then (0, 1)
                    else (ny, e.Dir)
                else
                    (e.Y, e.Dir)
            let newX = if state.Tick % 12 = 0 then max 10 (e.X - 1) else e.X
            { e with X = newX; Y = newY; Dir = newDir }
        else
            e
    )
    |> fun enemies ->
        if enemies <> state.Enemies then
            { state with Enemies = enemies; RedrawScreen = true }
        else
            state

/// El enemigo dispara un misil hacia la izquierda cada 15 ticks
let enemyShoot state =
    let aliveEnemies =
        state.Enemies |> List.filter (fun e -> e.Alive)
    if aliveEnemies |> List.isEmpty |> not && state.Tick % 15 = 0 then
        let rng = Random ()
        let shooter = aliveEnemies.[rng.Next(aliveEnemies.Length)]
        let misil = { X = shooter.X - 1; Y = shooter.Y }
        { state with
            EnemyMissiles = misil :: state.EnemyMissiles
            RedrawScreen = true }
    else
        state

/// Detecta si un misil del jugador golpea a un enemigo (distancia <= 2 en X y Y)
let checkMissileCollisions state =
    let hitEnemy =
        state.PlayerMissiles
        |> List.tryPick (fun m ->
            state.Enemies
            |> List.tryFind (fun e ->
                e.Alive && abs (e.X - m.X) <= 2 && abs (e.Y - m.Y) <= 2
            )
            |> Option.map (fun e -> (e, m))
        )
    match hitEnemy with
    | Some (enemy, misil) ->
        let remainingMissiles =
            state.PlayerMissiles
            |> List.filter (fun m -> not (m.X = misil.X && m.Y = misil.Y))
        let updatedEnemies =
            state.Enemies
            |> List.map (fun e ->
                if e.X = enemy.X && e.Y = enemy.Y then
                    { e with Alive = false; HitTick = state.Tick }
                else
                    e
            )
        let newKills = state.Kills + 1
        { state with
            Enemies = updatedEnemies
            PlayerMissiles = remainingMissiles
            Kills = newKills
            Screen = if newKills >= 10 then Victory else Playing
            RedrawScreen = true }
    | None -> state

/// Detecta si un misil enemigo golpea al jugador (distancia <= 1)
let checkPlayerHit state =
    if state.PlayerState = Alive then
        let hit =
            state.EnemyMissiles
            |> List.tryFind (fun m ->
                abs (m.X - state.PlayerX) <= 1 && abs (m.Y - state.PlayerY) <= 1
            )
        match hit with
        | Some _ ->
            let remainingMissiles =
                state.EnemyMissiles
                |> List.filter (fun m ->
                    abs (m.X - state.PlayerX) > 1 || abs (m.Y - state.PlayerY) > 1
                )
            { state with
                PlayerState = Exploding
                PlayerHitTick = state.Tick
                Vidas = state.Vidas - 1
                EnemyMissiles = remainingMissiles
                RedrawScreen = true }
        | None -> state
    else
        state

/// Después de 40 ticks de estar explotando:
/// - Si vidas <= 0: Game Over
/// - Si aún tiene vidas: vuelve a estado Alive (el jugador sigue en la misma posición)
let updateExplosions state =
    match state.PlayerState with
    | Exploding ->
        if state.Tick - state.PlayerHitTick >= 40 then
            if state.Vidas <= 0 then
                { state with Screen = GameOver; RedrawScreen = true }
            else
                { state with
                    PlayerState = Alive
                    PlayerHitTick = -1
                    RedrawScreen = true }
        else
            state
    | Alive -> state

/// Si el enemigo actual murió y pasaron 20 ticks, reaparece uno nuevo
let checkEnemyRespawn state =
    let hasAlive = state.Enemies |> List.exists (fun e -> e.Alive)
    if not hasAlive && state.Kills < 10 then
        let allExploded =
            state.Enemies
            |> List.forall (fun e -> state.Tick - e.HitTick >= 20)
        if allExploded && state.Enemies.Length > 0 then
            { state with Enemies = [createEnemy ()]; RedrawScreen = true }
        else
            state
    else
        state

/// Escribe un carácter en pantalla (con protección de bordes)
let renderChar x y color (c: char) =
    if x >= 0 && x < Console.BufferWidth && y >= 0 && y < Console.BufferHeight then
        Console.SetCursorPosition(x, y)
        Console.ForegroundColor <- color
        Console.Write c

/// Escribe un string en pantalla (con protección de bordes)
let renderStr x y color (s: string) =
    if y >= 0 && y < Console.BufferHeight then
        Console.SetCursorPosition(x, y)
        Console.ForegroundColor <- color
        Console.Write s

/// Dibuja la interfaz superior: vidas, kills y puntaje
let drawUI state =
    let vidaStr = String.replicate state.Vidas "❤️"
    let killsStr = sprintf "KILLS: %d / 10" state.Kills
    let scoreStr = sprintf "SCORE: %d" (state.Kills * 100)
    renderStr 2 0 ConsoleColor.Red vidaStr
    renderStr (Console.BufferWidth - 2 - killsStr.Length) 0 ConsoleColor.Cyan killsStr
    renderStr (Console.BufferWidth / 2 - scoreStr.Length / 2) 1 ConsoleColor.Yellow scoreStr

/// Dibuja los enemigos (👾 si viven, 💥 si explotan)
let drawEnemies state =
    state.Enemies
    |> List.iter (fun e ->
        if e.Alive then
            renderStr e.X e.Y ConsoleColor.Magenta "👾"
        elif state.Tick - e.HitTick < 10 then
            renderStr e.X e.Y ConsoleColor.Red "💥"
    )

let drawPlayerMissiles state =
    state.PlayerMissiles
    |> List.iter (fun m -> renderStr m.X m.Y ConsoleColor.Yellow "⇒")

let drawEnemyMissiles state =
    state.EnemyMissiles
    |> List.iter (fun m -> renderStr m.X m.Y ConsoleColor.Red "⇐")

/// Dibuja al jugador (🚀 si vive, 💥/⚰️ si explota)
let drawPlayer state =
    match state.PlayerState with
    | Alive ->
        renderStr state.PlayerX state.PlayerY ConsoleColor.Green "🚀"
    | Exploding ->
        if state.Vidas <= 0 then
            renderStr state.PlayerX state.PlayerY ConsoleColor.DarkYellow "⚰️"
        elif state.Tick - state.PlayerHitTick < 10 then
            renderStr state.PlayerX state.PlayerY ConsoleColor.Red "💥"

/// Limpia el área de juego pintando toda la pantalla de negro
let drawBackground () =
    let sw = Console.BufferWidth
    let sh = Console.BufferHeight
    for y in 0 .. sh - 1 do
        renderStr 0 y ConsoleColor.Black (String.replicate (sw - 1) " ")
    let rng = Random 123
    for _ in 1 .. 15 do
        let x = rng.Next(1, sw - 2)
        let y = rng.Next(1, sh - 2)
        renderChar x y ConsoleColor.DarkGray (if rng.Next(2) = 0 then '.' else '·')

/// Redibuja la pantalla completa si RedrawScreen está activo
let redrawScreen state =
    if state.RedrawScreen then
        drawBackground ()
        drawUI state
        drawEnemies state
        drawPlayerMissiles state
        drawEnemyMissiles state
        drawPlayer state
        { state with RedrawScreen = false }
    else
        state

/// Procesa la tecla presionada: movimiento, disparo, pausa
let processGameKeyboard (key: ConsoleKey) state =
    let sw = screenW ()
    let sh = screenH ()
    match state.PlayerState with
    | Alive ->
        match key with
        | ConsoleKey.UpArrow | ConsoleKey.W ->
            { state with PlayerY = max 2 (state.PlayerY - 1); RedrawScreen = true }
        | ConsoleKey.DownArrow | ConsoleKey.S ->
            { state with PlayerY = min (sh - 2) (state.PlayerY + 1); RedrawScreen = true }
        | ConsoleKey.LeftArrow | ConsoleKey.A ->
            { state with PlayerX = max 2 (state.PlayerX - 2); RedrawScreen = true }
        | ConsoleKey.RightArrow | ConsoleKey.D ->
            { state with PlayerX = min (sw - 2) (state.PlayerX + 2); RedrawScreen = true }
        | ConsoleKey.Spacebar ->
            if state.ShootCooldown = 0 then
                let misil = { X = state.PlayerX + 2; Y = state.PlayerY }
                { state with
                    PlayerMissiles = misil :: state.PlayerMissiles
                    ShootCooldown = 8
                    RedrawScreen = true }
            else
                state
        | ConsoleKey.P -> { state with Screen = Paused }
        | _ -> state
    | Exploding -> state

/// Muestra el menú de pausa con CONTINUAR, GUARDAR y MENU PRINCIPAL
/// Navegación con flechas, Enter para seleccionar, P para continuar rápido
let rec handlePause state =
    let cx = Console.BufferWidth / 2
    let pauseLines = [|
        "██████╗  █████╗ ██╗   ██╗███████╗ █████╗ "
        "██╔══██╗██╔══██╗██║   ██║██╔════╝██╔══██╗"
        "██████╔╝███████║██║   ██║███████╗███████║"
        "██╔═══╝ ██╔══██║██║   ██║╚════██║██╔══██║"
        "██║     ██║  ██║╚██████╔╝███████║██║  ██║"
        "╚═╝     ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═╝  ╚═╝"
    |]
    let options = [| "CONTINUAR"; "GUARDAR"; "MENU PRINCIPAL" |]
    let artY = Console.BufferHeight / 2 - 8
    let optY = artY + pauseLines.Length + 1

    pauseLines |> Array.iteri (fun i line ->
        renderStr (cx - line.Length / 2) (artY + i) ConsoleColor.Yellow line
    )
    options |> Array.iteri (fun i opt ->
        renderStr (cx - 6) (optY + i) ConsoleColor.Cyan opt
    )

    let rec pauseInput sel =
        for i in 0 .. options.Length - 1 do
            renderStr (cx - 8) (optY + i) ConsoleColor.Black ">"
        renderStr (cx - 8) (optY + sel) ConsoleColor.Yellow ">"
        Thread.Sleep 25
        if Console.KeyAvailable then
            let k = Console.ReadKey true
            match k.Key with
            | ConsoleKey.P -> Some 0
            | ConsoleKey.UpArrow -> pauseInput (max 0 (sel - 1))
            | ConsoleKey.DownArrow -> pauseInput (min (options.Length - 1) (sel + 1))
            | ConsoleKey.Enter -> Some sel
            | _ -> pauseInput sel
        else
            pauseInput sel

    let result = pauseInput 0
    safeClear ()
    match result with
    | Some 0 -> mainLoop { state with Screen = Playing; RedrawScreen = true }
    | Some 1 ->
        try saveGame state.Vidas state.Kills with _ -> ()
        handlePause { state with RedrawScreen = true }
    | _ ->
        { state with Screen = Quit }

/// Bucle principal del juego:
/// 1. Actualiza estado (tick, misiles, enemigos, colisiones)
/// 2. Procesa teclado
/// 3. Redibuja pantalla
/// 4. Espera 25ms y repite
and mainLoop state =
    if state.Screen = GameOver || state.Screen = Victory || state.Screen = Quit then
        state
    elif state.Screen = Paused then
        handlePause state
    else
        let pipelineState =
            state
            |> updateTick
            |> updateShootCooldown
            |> updatePlayerMissiles
            |> updateEnemyMissiles
            |> updateEnemies
            |> enemyShoot
            |> checkMissileCollisions
            |> checkPlayerHit
            |> updateExplosions
            |> checkEnemyRespawn

        let inputState =
            if Console.KeyAvailable then
                let k = Console.ReadKey true
                pipelineState |> processGameKeyboard k.Key
            else
                pipelineState

        let newState = inputState |> redrawScreen

        if newState.Screen = GameOver || newState.Screen = Victory || newState.Screen = Quit then
            newState
        elif newState.Screen = Paused then
            handlePause newState
        else
            Thread.Sleep 25
            mainLoop newState

/// Inicia una partida nueva (3 vidas, 0 kills)
let mostrar () =
    safeCursorVisible false
    safeClear ()
    let finalState = initialState 3 0 |> mainLoop
    safeCursorVisible true
    safeClear ()
    finalState.Screen, finalState.Vidas, finalState.Kills

/// Inicia una partida con datos cargados de una partida guardada
let mostrarConDatos vidas kills =
    safeCursorVisible false
    safeClear ()
    let finalState = initialState vidas kills |> mainLoop
    safeCursorVisible true
    safeClear ()
    finalState.Screen, finalState.Vidas, finalState.Kills
