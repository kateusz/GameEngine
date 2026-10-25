# Island Dash

A 3D platformer in the style of Crash Bandicoot, built on this engine's existing scene, rendering, input, and audio APIs. The whole level is 3D geometry: Kenney's Platformer Kit models (or colored cubes, if a file is missing), a perspective camera, and real jumps across platforms.

There is no skeletal animation. Characters are static meshes posed by `ProceduralAnimator`.

## Run

From `games/IslandDash`:

```bash
./run.sh
```

Windows:

```powershell
.\run.ps1
```

That builds the engine `Runtime`, compiles `assets/scripts` into `GameAssembly.dll`, stages `game.config.json` and the assets, cooks a `.mesh` sibling next to every model (the standalone player only loads those), and launches the game.

`./run.sh --no-run` (or `.\run.ps1 -NoRun`) stages the build without launching. `CONFIG=Debug ./run.sh` builds the Debug runtime.

You can also open this folder as an editor project and press Play. The startup scene is `assets/scenes/level1.scene`. Editor Play needs the same cooked `.mesh` siblings as the standalone player — run `.\run.ps1 -NoRun` (or `./run.sh --no-run`) once after cloning or changing models.

A logic check (collision sweep, manifest parsing, level reachability) runs with:

```bash
dotnet run --project tests/SelfCheck -c Release
```

## Controls

Keyboard and mouse. The engine has no gamepad API.

| Action | Key |
| --- | --- |
| Move | WASD or arrow keys |
| Jump, double jump | Space (hold longer for a higher jump) |
| Spin attack | K, J, Left Shift, or left mouse button |
| Orbit the camera | Q / E, or hold the right mouse button and drag |
| Pause | P or Escape |
| Start, confirm, restart | Enter |

Movement is relative to the camera. In a corridor marked fixed in the level, the orbit keys let go and the view eases back behind the player.

## How it plays

Four lives. Crystals add to a counter; 100 crystals grant an extra life. Crates break from a spin or from landing on them: plain crates spill crystals, a life crate gives a life, a checkpoint crate moves the respawn point. Spikes, crushers, enemies, and falling off the world cost a life and send you back to the last checkpoint. Stomp an enemy by landing on it, or spin into it. The goal at the end of the island is the level complete screen. Enter after Game Over or Level Complete rebuilds the level.

Jumping has coyote time, jump buffering, and a height that depends on how long Space is held. Tapping Space is a short hop.

Every movement number lives on the **Game** entity in the scene (`PlayerTuningComponent`, `CameraTuningComponent`, `GameSettingsComponent`) and can be edited in the inspector.

## Project

```
games/IslandDash/
  game.config.json          window, title, startup scene
  run.sh / run.ps1          one-command build and launch
  README.md  CREDITS.md  REPORT.md
  assets/
    scenes/level1.scene     camera, sun, sky, and the tuning components
    levels/level1.lvl       the level, one kit piece per line
    config/assets.manifest  logical model name -> file, scale, pivot, tint
    models/  hdri/  audio/  scripts/
  tests/SelfCheck/          collision and level checks
  tools/MeshCook/           writes the .mesh files the runtime loads
```

Gameplay code is under `assets/scripts/` and is the same source the editor compiles into the game assembly. `ProceduralAnimatorComponent` plus `IAnimationDriver` is the animation front end: gameplay only sets parameters and fires events (`Jump`, `Land`, `Die`, `Pop`, ...). Swap a driver in `AnimationDrivers` to hand-animate or to plug in skeletal animation later; gameplay code does not change.

## Level kit

`assets/levels/level1.lvl` is plain text. The track runs toward -Z. A `#` comments out the rest of a line, and `repeat N dx dy dz` stamps a piece. The header of that file lists every piece and its keys. Positions of floors and platforms are the **top** center; characters, crates, crystals, and hazards sit on a surface.

Swapping art is an edit to `assets/config/assets.manifest` (path, scale, pivot, tint). A missing name or a model that fails to load draws a magenta cube instead of stopping the game.

## Engine notes

The engine ships 2D Box2D physics only, so movement uses a swept box collider in the game code (one axis at a time, so a fast body cannot tunnel). There is no runtime text widget, so the HUD is emissive 3D text and model icons parented to the camera. See `REPORT.md` for the gaps, the two engine fixes this game needed, and what is worth animating by hand next.
