module App.Types

type Bullet = { X: int; Y: int }
type Enemy = { X: int; Y: int; Dir: int }

type State = {
    PlayerX: int; PlayerY: int
    Lives: int; Score: int
    PlayerBullets: Bullet list
    EnemyBullets: Bullet list
    Enemies: Enemy list
    Tick: int
    RedrawScreen: bool
    Kills: int
    TotalKills: int
    Invulnerable: int
    GameOver: bool
    Victory: bool
    Paused: bool
}
