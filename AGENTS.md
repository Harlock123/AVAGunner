# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers the game, its controls and downloads;
this file covers how the code is put together and what to watch when changing it.

## What this is

A rear-view space shooter in the style of the TailGunner arcade game, in C# on .NET 9 and Avalonia 11.3. Everything
is drawn in code as 3D wireframe vector graphics with a neon glow. Sound effects are synthesised into WAV files at
start-up and played through `NetCoreAudio`. One self-contained executable per platform.

## Commands

```bash
dotnet build AVAGunner.sln                        # needs the .NET 9 SDK (no global.json)
dotnet run                                        # play (from the repository root)
```

There are no tests yet, and no run arguments.

## Layout

The project is a single `AVAGunner.csproj` at the repository root.

- `Game/`: the game itself.
  - `GameEngine.cs` runs the loop, the phases (title, playing, paused, warping, game over), waves, spawning,
    collisions and the Capitol Ship encounters.
  - `GameState.cs` holds score, lives, wave and shields; `InputManager.cs` tracks keys and the mouse.
  - `SoundManager.cs` synthesises the effects into a temp folder and plays them on a pool of players.
- `Entities/`: `Enemy`, `CapitolShip`, `CapitolMissile`, `Projectile`, `Explosion` and `Reticle`.
- `Rendering/`: `GameRenderer` draws a frame; `Wireframe3D`, `VectorGraphics`, `PerspectiveGrid` and
  `Starfield` do the 3D projection and the glow.
- `Views/`: `GameControl.cs` is the custom control the game draws on (and saves screenshots from);
  `MainWindow.axaml` hosts it.
- `ViewModels/`: the template's MVVM scaffolding, set as the window's `DataContext`; the game logic
  lives in `Game/`.
- `runtimes/osx/native/libbass.dylib`: a committed native library; the project file does not reference it.
- `Images/`: screenshots used by the README.

## Rules that keep it working

- **Variable time step.** `GameEngine` ticks from a 60 Hz `DispatcherTimer` and passes the real elapsed time,
  capped at 0.1 s, to every `Update(deltaTime)`. Scale all movement and timers by `deltaTime`.
- **Randomness is not seeded.** Entities and the starfield each use their own `new Random()`; only the sound
  synthesis uses fixed seeds. Play is not reproducible.
- **Input.** `InputManager` gives a `dy` of -1 for up (Up or W). The mouse moves the reticle directly.
- **Sound needs a player program.** On Linux `NetCoreAudio` uses `aplay` (alsa-utils); without it the game is silent.
  Audio errors are swallowed on purpose so the game still runs.
- **Files.** Nothing is saved except screenshots (Ctrl+S), which go to `Documents/AVAGunner_Screenshots` under
  the user's home folder.

## Making a change

- Build and play the change by hand: there is no test project.
- Update `README.md` (features, controls) to match.

## CI and releases

`.github/workflows/build.yml` builds every push and pull request (no tests). It then publishes self-contained
single-file builds for win-x64/x86/arm64, linux-x64/arm64/arm and osx-x64/arm64, kept as workflow artifacts.
Pushes to `main` replace the assets of the `latest` pre-release, and a `v*` tag makes a proper release.
