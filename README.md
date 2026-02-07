# AVAGunner

A modern reimplementation of the classic TailGunner arcade game built with .NET 9 and Avalonia UI.

## Description

AVAGunner is a rear-view perspective space shooter featuring wireframe vector graphics with neon glow effects. Defend your ship from waves of approaching enemy fighters as they fly toward you from the distance.

## Screenshots

![Title Screen](Images/AVAGunner_20260203_001939.png)

![Gameplay - Wave Start](Images/AVAGunner_20260203_001959.png)

![Gameplay - Enemy Approach](Images/AVAGunner_20260203_002427.png)

![Gameplay - Explosion](Images/AVAGunner_20260203_002432.png)

![Gameplay - Multiple Enemies](Images/AVAGunner_20260203_002444.png)

![Gameplay - Combat](Images/AVAGunner_20260203_002446.png)

![Warp Transition](Images/AVAGunner_20260203_002449.png)

![Warp Effect](Images/AVAGunner_20260203_002451.png)

## Features

- Classic arcade-style gameplay
- 3D wireframe vector graphics with neon glow effects (cyan, magenta, yellow, green)
- 3D perspective rendering with enemies approaching from the distance
- Animated starfield background
- Multiple enemy types: Fighters, Bombers, Interceptors, Scouts, and Destroyers
- Curved enemy trajectories with 3D rotation and realistic flight patterns
- Defensive shield system (3 charges per wave) that bounces nearby enemies back with tumbling animation
- Explosion effects when enemies are destroyed
- Wave-based progression with increasing difficulty
- Capitol Ship mini-boss encounters between waves
- Cross-platform support (Windows, macOS, Linux)
- Mouse and keyboard input support
- Synthesized retro sound effects

## Capitol Ship Encounters

After every 3rd wave (following wave 3), a Capitol Ship mini-boss traverses the screen as an inter-wave challenge. The ship is a large dreadnought with sponson wings, a sensor mast, and a bridge superstructure rendered as a detailed 3D wireframe.

- **3 Turrets**: Each ship spawns with 3 randomly selected turrets from 5 possible hull positions, each with a distinct neon color (Cyan, Yellow, Green, Orange, Red)
- **Turret Missiles**: Turrets fire homing missiles at the player. The fire rate starts slow (1/4 normal speed) and increases with each subsequent encounter
- **Scoring**: Destroying a turret awards 500 points
- **Extra Life**: Destroying all 3 turrets awards the player a bonus life
- **Shield Defense**: The shield deflects incoming capitol missiles on contact
- **HUD**: A turret status display appears at the top center showing remaining turrets with colored indicators
- **No Regular Enemies**: Regular enemy spawning is paused during the encounter
- **Encounter Schedule**: Appears after waves 3, 6, 9, 12, and so on

## Controls

### Keyboard
- **Arrow Keys** or **WASD**: Move targeting reticle
- **Space**: Fire
- **Enter**: Start game / Resume from pause
- **V**: Activate shield
- **ESC**: Pause game (press twice while paused to exit)
- **Ctrl+S** or **Cmd+S**: Save screenshot to Documents folder

### Mouse
- **Move**: Aim targeting reticle
- **Left Click**: Fire
- **Right Click**: Activate shield

## Requirements

- .NET 9.0 SDK or later

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Lonnie Watson
