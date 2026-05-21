module App.Router

open System
open App.Types
open App.Save
open App.Game
open App.Utils

type RouterState =
| ShowingMenu
| ShowingGame of int * int

let rec mainLoop state =
    match state with
    | ShowingMenu ->
        let cmd = Menu.mostrarPrincipal ()
        match cmd with
        | NuevaPartida ->
            deleteSave ()
            mainLoop (ShowingGame (3, 0))
        | Continuar ->
            match loadGame () with
            | Some (v, k) when v > 0 ->
                mainLoop (ShowingGame (v, k))
            | _ ->
                mainLoop ShowingMenu
        | Salir ->
            ()
        | _ ->
            mainLoop ShowingMenu
    | ShowingGame (vidas, kills) ->
        let (screen, _, killsFinal) = mostrarConDatos vidas kills
        match screen with
        | GameOver ->
            deleteSave ()
            match Menu.mostrarGameOver killsFinal with
            | Reintentar -> mainLoop (ShowingGame (3, 0))
            | _ -> mainLoop ShowingMenu
        | Victory ->
            deleteSave ()
            match Menu.mostrarVictoria killsFinal with
            | Reintentar -> mainLoop (ShowingGame (3, 0))
            | _ -> mainLoop ShowingMenu
        | Quit ->
            mainLoop ShowingMenu
        | _ ->
            mainLoop ShowingMenu

let mostrar () =
    safeCursorVisible false
    mainLoop ShowingMenu
    safeCursorVisible true
    safeClear ()
