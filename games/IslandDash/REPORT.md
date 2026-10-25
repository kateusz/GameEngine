# Island Dash — report

A playable 3D corridor platformer in `games/IslandDash`. Launch with `./run.sh` (see `README.md`). Verified by playing the standalone runtime: running and jumping, collecting crystals, breaking a crate with the spin, standing on crates, dying to an enemy and to a pit, respawning at the checkpoint with the crystal count kept, pausing, riding the view while a moving platform travels, reaching the goal, and the Game Over screen.

## What works

- Third-person movement relative to the camera, with acceleration, coyote time, jump buffering, a jump height that depends on how long Space is held, and a double jump.
- Spin attack (breaks crates, defeats enemies) and a stomp (landing on a crate or an enemy). Landing on a crate was confirmed; the stomp-on-enemy rule is the same contact test that already kills the player from the side.
- Swept box collision, one axis at a time, so a fast fall cannot tunnel through a thin platform. Covered by `tests/SelfCheck`.
- Spring-arm camera: smoothed follow, slower vertically, look-ahead, orbit with Q/E or right-drag, and a ray that pulls the arm in before it enters level geometry. Fixed camera zones in the level ease the orbit back.
- One linear level of a few minutes: meadow, stepping stones, a moving platform, a vanishing platform, patrolling and chasing enemies, spikes, falling platforms, crusher corridors, and a goal. The piece list is `assets/levels/level1.lvl`. The self-check refuses a gap wider than a running jump along the main path, and it checks that 100 crystals are actually placed (107, crates included).
- Lives, checkpoints, extra life at 100 crystals, extra-life crate, Game Over, Level Complete with crystals, crates, and time, and Enter to rebuild.
- HUD drawn with the engine's 3D renderer: dot-matrix text, a spinning crystal, and the player icon. Counters, "GO!", pause, game over, and level complete were all seen in play.
- Directional sun with shadows, a Poly Haven sky (image-based lighting and the skybox), and the engine's bloom and Reinhard tonemap.
- Sound effects wired through `IAudio.PlayOneShot` for jump, spin, collect, crate, stomp, land, death, checkpoint, extra life, level complete, game over, and menu. This machine has no audio device, so they were not heard here; the engine disables playback cleanly in that case.

`ProceduralAnimator` is the animation seam. Gameplay writes speed, facing, grounded, and events. A driver turns those into a pose on a separate visual entity, so the collider never scales or leans. Drivers registered today: player (turn, lean, run bob, squash and stretch, spin, death, respawn), collectible (spin and bob, then pop), crate pop, enemy (turn, waddle, sway, flatten or tumble), and platform (shake, blink, pop back in).

## What the engine does not have

Filled in game code, not in the engine:

| Gap | What the game does instead |
| --- | --- |
| No 3D physics. Box2D is 2D only, and a 2D body cannot carry this game. | `ColliderComponent` plus `Collision`: swept AABB, depenetration, camera ray. |
| No skeletal animation, and the mesh path ignores skinning. | `ProceduralAnimator` / `IAnimationDriver`. |
| No runtime UI or font. ImGui is editor-only, and sprites are drawn before the 3D pass, so they cannot sit on top of the level. | Emissive cubes for text and small models for icons, placed in front of the camera each frame. |
| No gamepad or rebinding service. | Keyboard and mouse only, in `GameInput`. |
| No way for a script to spawn an entity. Scripts only receive the ECS `Context`. | `Spawner` allocates ids from 1,000,000 up and registers the entity. |
| No asset-root API for files the game reads itself. | `AssetPaths` walks up from the assembly to `game.config.json`. Model, texture, and audio paths are still resolved by the engine. |
| The standalone player and editor Play both load cooked `.mesh` siblings only. The editor UI cooks models opened in a scene; this game spawns models from code, so nothing cooked them. | `tools/MeshCook`, run by `run.sh` / `run.ps1` into `assets/models` (then copied to the stage). |
| Tonemap has no exposure. A bright HDRI crosses the bloom threshold of 1 and washes the frame out. | The shipped HDR is the Poly Haven file scaled to 55%. |

## Engine changes

Two, both required for the standalone player to draw a frame on this Mesa driver. Neither is gameplay.

1. `Engine/assets/shaders/OpenGL/cube.frag` and `modelShader.frag` enable `GL_ARB_gpu_shader5`. The shadow shader indexes a sampler array with a loop variable, which GLSL 3.30 forbids. Mesa rejects the program; the extension allows it. The include of `shadow.glsl` stays below the `#version` and `#extension` lines.
2. `Engine/Renderer/Pipeline/Graphics3D.cs` and `ShadowQuality.cs`. `new ShadowQuality()` on a record struct zeroes every field, including the shadow-map resolution. The editor always assigns a preset; the runtime never does, so the first directional shadow pass tried to allocate a 0×0 depth texture and threw `FramebufferIncompleteAttachment`. The default is now the Low preset (1024², one cascade) explicitly.

## Known limits

- Enemies do not fall and do not collide with walls. They stay at spawn height and patrol or chase in XZ. Fine on the flat ground they are placed on; a sloped level would need the same sweep the player uses.
- Colliders are axis-aligned and ignore the entity's rotation. Spun crates and tilted poses are visual only.
- The Kenney grass tiles carry painted tufts. Up close those read as dark flecks. It is the texture, at the kit's 2 m tile size.
- The Game Over text appears where you fell, which is often open sky, because the camera follows the death. Enter still restarts.
- The first frame after launch hitches while every model uploads. Later frames are steady. A software GL driver (this VM) spends real time on the shadow and bloom passes; a GPU will not.
- Crystals and broken crates stay collected after a death. Only a full restart (Game Over or Level Complete, then Enter) rebuilds them.
- The double jump is implemented and the single jump was confirmed in play, including the short hop you get from tapping Space. A separate double-jump was not isolated on camera.
- No music bed. Effects only.

## Worth animating by hand

The drivers are the list (`AnimationDrivers.For`). In order of how much it shows:

1. Player run: legs and arms, instead of the whole-body bob and waddle.
2. Player jump, double jump, and landing squash. The events `Jump`, `AirJump`, and `Land` already fire, with `LandImpact` set from the fall speed.
3. Player spin. `Spinning` is true for the whole attack.
4. Player death and respawn. `Die` and `Respawn` already start those sequences.
5. Patrol enemy walk, and a faster cycle for the chase.
6. Enemy defeat: the stomp already sends `Squash`, the spin sends `Die`.
7. Crate hit and break. `Squash` on a bump, `Pop` when it breaks. The reward model is swapped on before the pop.
8. Checkpoint flag. It is a static mesh today; a wave would be a new driver or a second bone later.
9. Idle crystal, heart, and goal star. The spin driver is already close to a final idle; a hand loop would mostly add a glint.

Moving platforms, the crusher, and the spikes are transform motion, not character animation. They can stay procedural.
