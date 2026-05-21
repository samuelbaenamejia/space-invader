/// Módulo de utilidades - Funciones auxiliares de pantalla y consola
module App.Utils

open System

/// Escribe texto en una posición específica de la consola
let displayMessage x y color (msg:string) =
    Console.SetCursorPosition(x,y)
    Console.ForegroundColor <- color
    msg |> Console.Write

/// Escribe texto alineado a la derecha en una fila
let displayMessageRight y color (msg:string) =
    let start = Console.BufferWidth - msg.Length
    displayMessage start y color msg

/// Limpia la pantalla de forma segura (no lanza error si falla)
let safeClear () =
    try Console.Clear () with _ -> ()

/// Muestra u oculta el cursor de forma segura
let safeCursorVisible v =
    try Console.CursorVisible <- v with _ -> ()
