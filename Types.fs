module SpaceInvaders.Types

type PowerUpType = DoubleShot | FastShot | ExtraLife

type GameScreen = Menu | Playing | Paused | GameOver | Victory

type Bullet = { X: int; Y: int; IsPlayerBullet: bool }

type Enemy = { X: int; Y: int; HP: int }

type Boss = { X: int; Y: int; HP: int; MaxHP: int; Direction: int; MoveCounter: int }

type PowerUp = { X: int; Y: int; Type: PowerUpType }

type Explosion = { X: int; Y: int; Timer: int }

type GameState = {
    PlayerX: int; PlayerY: int; Lives: int; Score: int; Level: int
    Bullets: Bullet list; Enemies: Enemy list; Boss: Boss option
    PowerUps: PowerUp list; Screen: GameScreen
    ActivePowerUp: PowerUpType option; PowerUpTimer: int
    FrameCount: int; SpawnInterval: int; EnemiesKilled: int
    EnemiesToNextLevel: int; Explosions: Explosion list
    BossLevel: bool; ShotCooldown: int; Invulnerable: int; LevelUpTimer: int
}
