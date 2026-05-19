module SpaceInvaders.Game

open System
open System.IO
open SpaceInvaders.Types
open SpaceInvaders.Utils

// ==================== MENU ====================

let drawPauseScreen () =
    drawGameAt 2 1 ConsoleColor.Yellow "=== PAUSA ==="
    drawCentered 13 ConsoleColor.White "Presiona P para continuar"
    drawCentered 14 ConsoleColor.Gray "Presiona M para menu principal"
    drawCentered 15 ConsoleColor.Gray "Presiona S para guardar"

let drawGameOver score level =
    Console.Clear()
    Console.BackgroundColor <- ConsoleColor.Black
    drawCentered 8 ConsoleColor.Red "========================================"
    drawCentered 10 ConsoleColor.Red "GAME OVER"
    drawCentered 12 ConsoleColor.Red "========================================"
    drawCentered 14 ConsoleColor.White (sprintf "Score final: %d" score)
    drawCentered 15 ConsoleColor.White (sprintf "Nivel alcanzado: %d" level)
    drawCentered 17 ConsoleColor.Cyan "Presiona R para reiniciar"
    drawCentered 18 ConsoleColor.Cyan "Presiona M para menu principal"

let drawVictory score level =
    Console.Clear()
    Console.BackgroundColor <- ConsoleColor.Black
    drawCentered 6 ConsoleColor.Green "========================================"
    drawCentered 8 ConsoleColor.Yellow "       TIERRA SALVADA!"
    drawCentered 9 ConsoleColor.Cyan  "    Los invasores han sido derrotados."
    drawCentered 10 ConsoleColor.White "    La humanidad esta a salvo."
    drawCentered 12 ConsoleColor.Green "========================================"
    drawCentered 14 ConsoleColor.White (sprintf "Score: %d" score)
    drawCentered 15 ConsoleColor.White (sprintf "Nivel alcanzado: %d" level)
    drawCentered 17 ConsoleColor.Cyan "Presiona R para seguir jugando"
    drawCentered 18 ConsoleColor.Cyan "Presiona M para menu principal"
    drawCentered 19 ConsoleColor.Cyan "Presiona ESC para salir"

let showMenu () =
    Console.CursorVisible <- false
    Console.Clear()
    let mutable selected = 0
    let mutable running = true
    while running do
        Console.Clear()
        drawCentered 4 ConsoleColor.Yellow "SPACE INVADERS F#"
        drawCentered 6 ConsoleColor.White "La Tierra esta siendo invadida por aliens!"
        drawCentered 8 ConsoleColor.Gray "Eres un piloto de la fuerza espacial."

        let options = [| "Nueva Partida"; "Continuar Partida"; "Salir" |]
        for i in 0 .. 2 do
            let y = 12 + i * 2
            if i = selected then
                drawCentered y ConsoleColor.Green (sprintf ">> %s <<" options.[i])
            else
                drawCentered y ConsoleColor.Cyan options.[i]

        drawCentered 20 ConsoleColor.DarkGray "WASD / Flechas: mover    Espacio: disparar    P: pausa"

        let key = Console.ReadKey(true).Key
        if key = ConsoleKey.UpArrow || key = ConsoleKey.W then
            if selected > 0 then playBeep 600 120
            selected <- max 0 (selected - 1)
        elif key = ConsoleKey.DownArrow || key = ConsoleKey.S then
            if selected < 2 then playBeep 600 120
            selected <- min 2 (selected + 1)
        elif key = ConsoleKey.Enter then
            playBeep 800 150
            running <- false
    Console.Clear()
    selected

// ==================== JUGADOR ====================

let moveLeft state =
    if state.PlayerX > 2 then { state with PlayerX = state.PlayerX - 2 }
    else state

let moveRight state =
    if state.PlayerX < GAME_WIDTH - 4 then { state with PlayerX = state.PlayerX + 2 }
    else state

let moveUp state =
    if state.PlayerY > GAME_TOP + 1 then { state with PlayerY = state.PlayerY - 1 }
    else state

let moveDown state =
    if state.PlayerY < PLAYER_ROW then { state with PlayerY = state.PlayerY + 1 }
    else state

// ==================== BALAS ====================

let createPlayerBullet state =
    if state.ShotCooldown > 0 then state
    else
        let playerBullets = state.Bullets |> List.filter (fun b -> b.IsPlayerBullet)
        let maxBullets =
            match state.ActivePowerUp with
            | Some DoubleShot -> 6
            | _ -> 3
        if playerBullets.Length >= maxBullets then state
        else
            playBeep 880 50
            let cooldown =
                match state.ActivePowerUp with
                | Some FastShot -> 4
                | _ -> 8
            match state.ActivePowerUp with
            | Some DoubleShot ->
                let b1 = { X = state.PlayerX; Y = state.PlayerY - 1; IsPlayerBullet = true }
                let b2 = { X = state.PlayerX + 2; Y = state.PlayerY - 1; IsPlayerBullet = true }
                { state with Bullets = b1 :: b2 :: state.Bullets; ShotCooldown = cooldown }
            | _ ->
                let b = { X = state.PlayerX + 1; Y = state.PlayerY - 1; IsPlayerBullet = true }
                { state with Bullets = b :: state.Bullets; ShotCooldown = cooldown }

let moveBullets state =
    let moved =
        state.Bullets
        |> List.map (fun b ->
            if b.IsPlayerBullet then { b with Y = b.Y - 1 }
            else { b with Y = b.Y + 1 })
    let alive = moved |> List.filter (fun b -> b.Y >= GAME_TOP && b.Y <= PLAYER_ROW)
    { state with Bullets = alive }

// ==================== ENEMIGOS ====================

let spawnEnemy state =
    let rand = Random()
    let x = rand.Next(2, GAME_WIDTH - 4)
    let xEven = if x % 2 <> 0 then x + 1 else x
    { state with Enemies = { X = xEven; Y = GAME_TOP; HP = 1 } :: state.Enemies }

let moveEnemies state =
    let moved = state.Enemies |> List.map (fun e -> { e with Y = e.Y + 1 })
    { state with Enemies = moved }

// ==================== BOSS ====================

let spawnBoss state =
    let hp = state.Level * 5
    { state with
        Boss = Some {
            X = GAME_WIDTH / 2 - 2; Y = GAME_TOP
            HP = hp; MaxHP = hp; Direction = 1; MoveCounter = 0
        }
    }

let moveBoss state =
    match state.Boss with
    | Some boss ->
        let counter = boss.MoveCounter + 1
        if counter % 2 = 0 then
            let newX = boss.X + boss.Direction * 3
            let newDir =
                if newX <= 2 then 1
                elif newX >= GAME_WIDTH - 6 then -1
                else boss.Direction
            let newY =
                if counter % 30 = 0 && boss.Y < GAME_TOP + 8 then boss.Y + 1
                else boss.Y
            { state with
                Boss = Some {
                    boss with
                        X = boss.X + boss.Direction * 2
                        Direction = newDir; Y = newY; MoveCounter = counter
                }
            }
        else
            { state with Boss = Some { boss with MoveCounter = counter } }
    | None -> state

// ==================== POWERUPS ====================

let trySpawnPowerUp state =
    let rand = Random()
    if rand.Next(0, 100) < 30 then
        let x = rand.Next(2, GAME_WIDTH - 4)
        let xEven = if x % 2 <> 0 then x + 1 else x
        let powerType =
            match rand.Next(0, 6) with
            | 0 -> DoubleShot
            | 1 -> FastShot
            | _ -> ExtraLife
        { state with PowerUps = { X = xEven; Y = GAME_TOP; Type = powerType } :: state.PowerUps }
    else state

let movePowerUps state =
    if state.FrameCount % 4 <> 0 then state
    else
        let moved = state.PowerUps |> List.map (fun p -> { p with Y = p.Y + 1 })
        let alive = moved |> List.filter (fun p -> p.Y <= PLAYER_ROW)
        { state with PowerUps = alive }

let checkPowerUpCollision state =
    let collected, remaining =
        state.PowerUps |> List.partition (fun p ->
            p.Y >= state.PlayerY - 1 && p.Y <= state.PlayerY + 1 &&
            abs(p.X - state.PlayerX) < 4)
    if collected.IsEmpty then state
    else
        playBeep 1200 200
        let state = { state with PowerUps = remaining }
        match collected.Head.Type with
        | DoubleShot -> { state with ActivePowerUp = Some DoubleShot; PowerUpTimer = 600 }
        | FastShot -> { state with ActivePowerUp = Some FastShot; PowerUpTimer = 600 }
        | ExtraLife -> { state with Lives = min 5 (state.Lives + 1) }

let updatePowerUpTimer state =
    match state.ActivePowerUp with
    | Some _ ->
        if state.PowerUpTimer <= 0 then
            { state with ActivePowerUp = None; PowerUpTimer = 0 }
        else { state with PowerUpTimer = state.PowerUpTimer - 1 }
    | None -> state

// ==================== GUARDAR ====================

let saveFile = "savegame.txt"

let saveGame state =
    try
        File.WriteAllLines(saveFile, [| string state.Score; string state.Level |])
        true
    with _ -> false

let loadGame () =
    try
        if File.Exists(saveFile) then
            let lines = File.ReadAllLines(saveFile)
            if lines.Length >= 2 then Some (int lines.[0], int lines.[1])
            else None
        else None
    with _ -> None

// ==================== ESTADO INICIAL ====================

let createInitialState () =
    {
        PlayerX = GAME_WIDTH / 2 - 1; PlayerY = PLAYER_ROW
        Lives = 3; Score = 0; Level = 1; Bullets = []
        Enemies = []; Boss = None; PowerUps = []; Screen = Menu
        ActivePowerUp = None; PowerUpTimer = 0; FrameCount = 0
        SpawnInterval = 90; EnemiesKilled = 0; EnemiesToNextLevel = 8
        Explosions = []; BossLevel = false; ShotCooldown = 0
        Invulnerable = 0; LevelUpTimer = 0
    }

let createStateFromSave score level =
    { createInitialState() with
        Score = score; Level = level
        SpawnInterval = max 30 (90 - (level - 1) * 5)
        EnemiesToNextLevel = 5 + level * 3
    }

// ==================== DIBUJO ====================

let drawHUD state =
    drawGameAt 0 0 ConsoleColor.Black (String(' ', GAME_WIDTH))
    drawGameAt 1 0 ConsoleColor.White (sprintf "SCORE: %d" state.Score)
    match state.ActivePowerUp with
    | Some DoubleShot -> drawGameAt 22 0 ConsoleColor.Cyan "DOBLE DISPARO"
    | Some FastShot -> drawGameAt 22 0 ConsoleColor.Cyan "DISPARO RAPIDO"
    | Some ExtraLife -> drawGameAt 22 0 ConsoleColor.Red "VIDA EXTRA!"
    | None -> drawGameAt 22 0 ConsoleColor.DarkGray "------------"
    if state.BossLevel then
        drawGameAt 35 0 ConsoleColor.Red "  BOSS FIGHT!  "
    else
        drawGameAt 38 0 ConsoleColor.Cyan (sprintf " NIVEL %d " state.Level)
    let hearts = String.replicate state.Lives "❤️ " + "        "
    drawGameAt (GAME_WIDTH - 14) 0 ConsoleColor.Red hearts

let drawGameArea () =
    drawGameAt 0 1 ConsoleColor.DarkGray (String('═', GAME_WIDTH))
    drawGameAt 0 GAME_BOTTOM ConsoleColor.DarkGray (String('═', GAME_WIDTH))
    for y in 1 .. GAME_BOTTOM do
        drawGameAt 0 y ConsoleColor.DarkGray "║"
        drawGameAt (GAME_WIDTH - 1) y ConsoleColor.DarkGray "║"

let drawFrame state =
    clearGameArea ()
    drawGameArea ()
    drawHUD state

    for e in state.Enemies do
        let color = if e.HP > 1 then ConsoleColor.Magenta else ConsoleColor.Red
        drawGameAt e.X e.Y color "👾"

    match state.Boss with
    | Some boss ->
        drawGameAt boss.X boss.Y ConsoleColor.DarkRed "👹"
        let hpText = sprintf "HP:%d/%d" boss.HP boss.MaxHP
        let hx = min boss.X (GAME_WIDTH - 4 - hpText.Length)
        drawGameAt (max 2 hx) (boss.Y + 1) ConsoleColor.Red hpText
    | None -> ()

    for b in state.Bullets do
        if b.IsPlayerBullet then drawGameAt b.X b.Y ConsoleColor.Yellow "|"
        else drawGameAt b.X b.Y ConsoleColor.Red "|"

    for p in state.PowerUps do
        let symbol, color =
            match p.Type with
            | DoubleShot -> "⭐", ConsoleColor.Cyan
            | FastShot -> "⚡", ConsoleColor.Yellow
            | ExtraLife -> "❤️", ConsoleColor.Red
        drawGameAt p.X p.Y color symbol

    for e in state.Explosions do
        drawGameAt e.X e.Y ConsoleColor.Red "💥"

    if state.Lives > 0 && (state.Invulnerable <= 0 || state.FrameCount % 4 < 2) then
        drawGameAt state.PlayerX state.PlayerY ConsoleColor.Green "🚀 "

    if state.LevelUpTimer > 0 then
        let msg = sprintf "  NIVEL %d!  " state.Level
        let mx = (GAME_WIDTH - msg.Length) / 2
        drawGameAt mx 10 ConsoleColor.Yellow msg

    if state.Screen = Paused then drawPauseScreen ()

// ==================== COLISIONES ====================

let checkCollisions state =
    let playerBullets = state.Bullets |> List.filter (fun b -> b.IsPlayerBullet)
    let enemyBullets = state.Bullets |> List.filter (fun b -> not b.IsPlayerBullet)

    let mutable keptBullets = []
    let mutable livingEnemies = state.Enemies
    let mutable newExplosions = []
    let mutable enemiesKilled = 0

    for b in playerBullets do
        let mutable hitEnemy = None
        for e in livingEnemies do
            if abs(b.X - e.X) <= 2 && b.Y = e.Y then
                hitEnemy <- Some e
        match hitEnemy with
        | Some e ->
            enemiesKilled <- enemiesKilled + 1
            newExplosions <- { X = e.X; Y = e.Y; Timer = 10 } :: newExplosions
            livingEnemies <- livingEnemies |> List.filter (fun e2 -> e2 <> e)
        | None ->
            keptBullets <- b :: keptBullets

    if enemiesKilled > 0 then playBeep 660 150

    let oldBoss = state.Boss
    let mutable finalBoss = state.Boss
    let mutable playerBulletsAfterBoss = []

    for b in keptBullets do
        match finalBoss with
        | Some bs when abs(b.X - bs.X) <= 2 && b.Y = bs.Y ->
            if bs.HP <= 1 then finalBoss <- None
            else finalBoss <- Some { bs with HP = bs.HP - 1 }
        | _ ->
            playerBulletsAfterBoss <- b :: playerBulletsAfterBoss

    let bossKilled = state.Boss.IsSome && finalBoss.IsNone
    if bossKilled then playBeep 200 400

    let scoreGain = enemiesKilled * 10 + (if bossKilled then 50 else 0)
    let newKills = enemiesKilled + (if bossKilled then 1 else 0)

    let bossExplosions =
        if bossKilled then
            match oldBoss with
            | Some bs -> [{ X = bs.X; Y = bs.Y; Timer = 20 }]
            | None -> []
        else []

    let state =
        { state with
            Bullets = playerBulletsAfterBoss @ enemyBullets
            Enemies = livingEnemies
            Score = state.Score + scoreGain
            EnemiesKilled = state.EnemiesKilled + newKills
            Boss = finalBoss
            Screen = if bossKilled then Victory else state.Screen
            Explosions = state.Explosions @ newExplosions @ bossExplosions
        }

    let state = if enemiesKilled > 0 then trySpawnPowerUp state else state

    let playerHit =
        state.Invulnerable <= 0 && (
            enemyBullets |> List.exists (fun b ->
                b.Y >= state.PlayerY - 1 && b.Y <= state.PlayerY + 1 &&
                abs(b.X - state.PlayerX) <= 2) ||
            livingEnemies |> List.exists (fun e ->
                e.Y >= PLAYER_ROW - 1 ||
                (abs(e.Y - state.PlayerY) <= 1 && abs(e.X - state.PlayerX) <= 1))
        )

    state, playerHit

// ==================== ACTUALIZACION ====================

let updateExplosions state =
    let updated =
        state.Explosions
        |> List.map (fun e -> { e with Timer = e.Timer - 1 })
        |> List.filter (fun e -> e.Timer > 0)
    { state with Explosions = updated }

let handleLifeLoss state =
    let newLives = state.Lives - 1
    playBeep 260 50
    let playerExplosion = { X = state.PlayerX; Y = state.PlayerY; Timer = 12 }

    let enemiesCleaned =
        state.Enemies |> List.filter (fun e ->
            e.Y < PLAYER_ROW - 1 &&
            not (abs(e.Y - state.PlayerY) <= 1 && abs(e.X - state.PlayerX) <= 1))
    let bulletsCleaned =
        state.Bullets |> List.filter (fun b ->
            b.IsPlayerBullet ||
            not (b.Y >= state.PlayerY - 1 && abs(b.X - state.PlayerX) <= 3))

    if newLives <= 0 then
        { state with
            Lives = 0; Screen = GameOver
            Enemies = enemiesCleaned; Bullets = bulletsCleaned
            Explosions = state.Explosions @ [playerExplosion]
        }
    else
        { state with
            Lives = newLives; Invulnerable = 60
            Enemies = enemiesCleaned; Bullets = bulletsCleaned
            Explosions = state.Explosions @ [playerExplosion]
        }

let checkLevelProgression state =
    if state.Screen <> Playing then state
    elif state.EnemiesKilled >= state.EnemiesToNextLevel then
        let nl = state.Level + 1
        let isBoss = nl % 5 = 0
        let state =
            { state with
                Level = nl; EnemiesKilled = 0
                EnemiesToNextLevel = 5 + nl * 3
                SpawnInterval = max 30 (90 - (nl - 1) * 5)
                BossLevel = isBoss; LevelUpTimer = 90
            }
        if isBoss then
            playBeep 400 200; wait 100; playBeep 600 200
            spawnBoss state
        else
            playBeep 500 200; wait 100; playBeep 700 200
            state
    else state

let cleanOffscreen state =
    { state with
        Enemies = state.Enemies |> List.filter (fun e -> e.Y <= GAME_BOTTOM - 2)
        PowerUps = state.PowerUps |> List.filter (fun p -> p.Y <= GAME_BOTTOM - 2)
    }

let update state =
    let state =
        { state with
            FrameCount = state.FrameCount + 1
            ShotCooldown = max 0 (state.ShotCooldown - 1)
            Invulnerable = max 0 (state.Invulnerable - 1)
            LevelUpTimer = max 0 (state.LevelUpTimer - 1)
        }

    let state =
        if state.BossLevel && state.Boss.IsSome then state
        elif state.FrameCount % state.SpawnInterval = 0 then spawnEnemy state
        else state

    let mi = max 4 (8 - state.Level / 5)
    let state =
        if state.FrameCount % mi = 0 then moveEnemies state
        else state

    let state = moveBullets state
    let state = moveBoss state

    let rng = Random()
    let state =
        if state.Boss.IsSome && rng.Next(100) < 3 then
            match state.Boss with
            | Some boss ->
                { state with
                    Bullets = { X = boss.X + 2; Y = boss.Y + 1; IsPlayerBullet = false } :: state.Bullets }
            | None -> state
        else state

    let state = movePowerUps state
    let state, playerHit = checkCollisions state
    let state = if playerHit then handleLifeLoss state else state
    let state = checkPowerUpCollision state
    let state = updatePowerUpTimer state
    let state = updateExplosions state
    let state = checkLevelProgression state
    let state = cleanOffscreen state
    state

// ==================== PROCESAR TECLAS ====================

let processInput state =
    if not (Console.KeyAvailable) then state
    else
        let key = Console.ReadKey(true).Key

        match state.Screen with
        | Playing ->
            if key = ConsoleKey.LeftArrow || key = ConsoleKey.A then moveLeft state
            elif key = ConsoleKey.RightArrow || key = ConsoleKey.D then moveRight state
            elif key = ConsoleKey.UpArrow || key = ConsoleKey.W then moveUp state
            elif key = ConsoleKey.DownArrow || key = ConsoleKey.S then moveDown state
            elif key = ConsoleKey.Spacebar then createPlayerBullet state
            elif key = ConsoleKey.P then { state with Screen = Paused }
            elif key = ConsoleKey.M then { createInitialState() with Screen = Menu }
            else state

        | Paused ->
            if key = ConsoleKey.P then { state with Screen = Playing }
            elif key = ConsoleKey.M then { createInitialState() with Screen = Menu }
            elif key = ConsoleKey.S then
                if saveGame state then drawCentered 14 ConsoleColor.Green "Partida guardada!"
                else drawCentered 14 ConsoleColor.Red "Error al guardar!"
                drawCentered 15 ConsoleColor.Gray "Presiona cualquier tecla..."
                Console.ReadKey(true) |> ignore
                state
            else state

        | GameOver ->
            if key = ConsoleKey.R then { createInitialState() with Screen = Playing }
            elif key = ConsoleKey.M then { createInitialState() with Screen = Menu }
            else state

        | Victory ->
            if key = ConsoleKey.R then { createInitialState() with Screen = Playing }
            elif key = ConsoleKey.M then { createInitialState() with Screen = Menu }
            elif key = ConsoleKey.Escape then { createInitialState() with Screen = Menu }
            else state

        | Menu -> state

// ==================== BUCLE DEL JUEGO ====================

let playGameOverSound () =
    playBeep 500 180; wait 120
    playBeep 400 180; wait 120
    playBeep 300 180; wait 150
    playBeep 200 400

let runGame initialState =
    let mutable state = { initialState with Screen = Playing }
    let mutable running = true
    let mutable sonidoGameOver = false

    while running do
        state <- processInput state

        if state.Screen = Menu then
            running <- false
        elif state.Screen = GameOver then
            drawGameOver state.Score state.Level
            if not sonidoGameOver then
                playGameOverSound ()
                sonidoGameOver <- true
            wait 33
        elif state.Screen = Victory then
            drawVictory state.Score state.Level
            wait 33
        elif state.Screen = Paused then
            drawFrame state
            wait 33
        else
            sonidoGameOver <- false
            state <- update state
            drawFrame state
            if state.ActivePowerUp = Some FastShot then wait 16
            else wait 33

    state

// ==================== INICIO ====================

let start () =
    let mutable salir = false
    while not salir do
        let opcion = showMenu ()
        if opcion = 2 then
            salir <- true
            Console.CursorVisible <- true
            printfn "Gracias por jugar Space Invaders F#!"
            wait 1000
        elif opcion = 1 then
            match loadGame () with
            | Some (score, level) ->
                printfn $"Cargando partida - Score: {score}, Nivel: {level}..."
                wait 500
                runGame (createStateFromSave score level) |> ignore
            | None ->
                printfn "No hay partida guardada."
                wait 1000
        else
            printfn "Iniciando nueva partida..."
            wait 500
            runGame (createInitialState ()) |> ignore
