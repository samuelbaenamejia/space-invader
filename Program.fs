open System
open App
open App.Utils

try
    Router.mostrar ()
with
| ex ->
    safeCursorVisible true
    safeClear ()
    Console.ForegroundColor <- ConsoleColor.Red
    Console.SetCursorPosition(0, 0)
    printfn "ERROR: %s" ex.Message
    printfn "%s" ex.StackTrace
    ignore (Console.ReadKey true)
