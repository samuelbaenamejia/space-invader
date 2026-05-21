/// Módulo de tipos - Define los tipos de datos que usa todo el programa
module App.Types

/// Comandos que puede elegir el usuario en los menús
type Command =
| NuevaPartida  /// Empezar juego desde cero
| Continuar     /// Cargar partida guardada
| Salir         /// Salir del juego
| Reintentar    /// Volver a jugar después de perder/ganar
