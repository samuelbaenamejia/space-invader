# PROYECTO SPACE INVADERS F# — CONTEXTO COMPLETO

## UBICACIÓN
C:\Users\Usuario\OneDrive\Escritorio\spaceinvaders

## CÓMO EJECUTAR
cd C:\Users\Usuario\OneDrive\Escritorio\spaceinvaders
dotnet run

## ESTRUCTURA
spaceinvaders/
├── Program.fs       # Entry point, bucle principal menú→juego
├── Game.fs          # Lógica central: update, colisiones, game loop
├── Types.fs         # Todos los tipos: GameState, Bullet, Enemy, Boss, etc.
├── Utils.fs         # Dibujo (drawGameAt, drawCentered), beep, constantes
├── Menu.fs          # Menú principal, pausa, game over, victoria, level select
├── Player.fs        # Movimiento y dibujo del jugador 🚀
├── Bullet.fs        # Disparo, movimiento de balas, cooldown
├── Enemy.fs         # Spawn, caída de enemigos 👾
├── Boss.fs          # Jefe 👹, movimiento, disparo, barra HP
├── PowerUp.fs       # Powerups ⭐⚡❤️, spawn, recolección
├── SaveSystem.fs    # Guardado/carga en savegame.txt
├── spaceinvaders.fsproj
└── README.md

## CONTROLES
- WASD / Flechas: mover
- Espacio: disparar
- P: pausa
- M: menú principal
- K: nivel secreto (1-5, flechas, Enter)
- S: guardar (en pausa)
- R: reiniciar (game over / victoria)
- ESC: salir (victoria)

## ARQUITECTURA
- Consola 80x24, GAME_WIDTH=80, PLAYER_ROW=20
- Offset automático GAME_OFFSET_X para centrar en terminales grandes
- GameState inmutable, se pasa por todas las funciones
- Game loop en runGame(): processInput → update → drawFrame → wait
- drawGameAt(x, y, color, texto) para dibujar dentro del área del juego
- drawCentered(y, color, texto) para texto centrado en toda la terminal
- Sonido vía kernel32.dll Beep (playBeep freq dur)

## MECÁNICAS
- 3 vidas ❤️, máx 5
- Enemigos caen desde arriba, velocidad según nivel
- Jefe cada 5 niveles (más vida, se mueve, dispara)
- Powerups: ⭐ doble disparo, ⚡ disparo rápido, ❤️ vida extra
- Asteroides 🪨 no destructibles (quitan vida al chocar)
- Niveles: matar X enemigos para avanzar
- Dificultad progresiva: más enemigos, más rápidos
- Jefe: no spawnean aliens durante boss fight
- Invulnerabilidad 60 frames (~2s) tras recibir golpe

## MELODÍAS (ELIMINADAS)
Se eliminaron todas las melodías de fondo porque el usuario las odió.
Solo quedan beeps cortos para acciones:
- Disparo: 880hz 180ms
- Enemigo muerto: 660hz 150ms
- Hit jugador: 260hz 300ms
- Game over: 500→400→300→200hz
- Subir nivel: 500+700hz
- Boss aparece: 400+600hz
- Boss muerto: 200hz 400ms
- Menú navegación: 600hz 120ms (con cooldown 120ms)
- Menú Enter: 800hz 150ms

## ESTADOS DEL JUEGO (GameScreen)
- Menu: menú principal
- Playing: jugando
- Paused: pausa
- GameOver: muerte
- Victory: boss derrotado (pantalla "Tierra Salvada!")

## MENÚ PRINCIPAL (Menu.show)
- Nueva Partida → StartAtLevel → game
- Continuar → loadGame → game
- K → levelSelect → StartAtLevel
- Salir → exit

## NIVEL SECRETO (K)
- Flechas arriba/abajo para seleccionar nivel 1-5
- Nivel 5 = "BOSS FIGHT" (spawnea boss inmediatamente)
- Enter para empezar, ESC para cancelar
- No se muestra en ningún lado de la UI (secreto)

## PANTALLA VICTORIA
- Aparece inmediatamente al matar al jefe
- Muestra "TIERRA SALVADA!", score, nivel
- R: seguir jugando
- M: menú principal
- ESC: salir

## NÚMERO DE MÓDULOS
11 archivos .fs, ~400 líneas Game.fs, ~130 Menu.fs, ~95 Utils.fs

## TECNOLOGÍAS
- .NET 10.0, F#, Consola Windows
- kernel32.dll Beep para sonido
- Sin dependencias externas
- Sin async/await
- Sin ECS
- Sin patrones avanzados

## CAMBIOS PENDIENTES (no implementados)
- Ninguno conocido. El usuario cerró sesión.

## NOTAS PARA PRÓXIMA SESIÓN
- Melodías: el usuario las pidió específicas pero luego dijo "quita toda musica".
- Sonido: playBeep usa kernel32 primero, fallbacks a Console.Beep y \x07.
- K key: no tiene pistas visuales. Solo el usuario sabe que existe.
- Nivel 5 = Boss. Al seleccionarlo con K, el boss aparece inmediatamente.
- El juego NO tiene música de fondo. Solo beeps de acciones.
