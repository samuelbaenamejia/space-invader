module App.Types

type ProgramState =
| Running
| GameOver
| Victory
| Paused
| Terminated

type Bullet = { X: int; Y: int }
type Enemy = { X: int; Y: int; Dir: int }
type Explosion = { X: int; Y: int; Timer: int }

type State = {
    ProgramState: ProgramState
    PlayerX: int; PlayerY: int
    Lives: int; Score: int
    PlayerBullets: Bullet list
    EnemyBullets: Bullet list
    Enemies: Enemy list
    Explosions: Explosion list
    Tick: int
    RedrawScreen: bool
    Kills: int
    Invulnerable: int
}
