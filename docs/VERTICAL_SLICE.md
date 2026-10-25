# Hounds of the Marches — vertical slice

Working title. Polish: Ogary Marchii. A hound-knight holds one stretch of the marches against feline mercenaries. Dry heraldry, no high magic, no rats.

## Loop

Ashmere's shrine is still lit. Cross the village, the broken plank bridge, and the keep gate. Light swordsmen and helmeted halberdiers hold the road. A gilt captain waits in the courtyard and changes pattern at half health. Shrines refill you and put the garrison back on post. Death drops every stinger you are carrying onto a stain; walk back and pick them up. Beating the captain ends the slice.

## Controls

| Input | Action |
|---|---|
| A / D or Left / Right | Run |
| Space | Short jump. Coyote 0.10 s, buffer 0.12 s |
| Walk off a ledge | Drop. Pits below y = −3 kill |
| W / S or Up / Down on the ladder | Climb. Space lets go |
| J | Light attack |
| K | Heavy attack |
| Left Shift | Dodge |
| L | Block. The first frames are the parry |
| E | Rest at a shrine |

## Numbers at 60 FPS

Player: 120 HP, 40 poise, 100 stamina. Run 6.4, jump 8.4, dodge speed 11.5. Gravity −9.8. Stamina regenerates at 22/s after 0.45 s. Poise regenerates at 16/s after 1.3 s. Mud puddles multiply speed by 0.62. Ladder climb is 3.3.

Dodge lasts 18 frames. Invulnerable frames are 1 through 10 inclusive (10 frames) and cost 26 stamina. Parry is the first 7 frames of a block: it staggers the attacker and returns 10 stamina. A held block negates the hit for 16 stamina. Empty stamina guard-breaks into a 28-frame stagger. Hitstop is about 0.05 s on a light hit, 0.09 s on a heavy hit, and 0.12 s on a parry.

| Attack | Windup | Active | Recover | Damage | Poise | Stamina | Reach | Knock |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Player light | 8 | 6 | 12 | 16 | 12 | 14 | 0.95 | 4.2 |
| Player heavy | 22 | 8 | 18 | 34 | 28 | 28 | 1.15 | 7.5 |

Heavy windup is 22 frames (0.37 s). Every enemy windup is at least 21 frames (0.35 s).

## Enemies

| | HP | Poise | Windup | Active | Recover | Damage | Reach | Stingers |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Cat swordsman | 55 | 22 | 24 | 8 | 14 | 14 | 1.15 | 6 |
| Cat halberdier | 110 | 50 | 36 | 10 | 20 | 24 | 1.70 | 10 |
| Feline captain, slash | 260 | 80 | 24 | 8 | 16 | 20 | 1.45 | 40 |
| Feline captain, thrust | | | 27 | 8 | 16 | 22 | 1.85 | |
| Feline captain, leap | | | 33 | 10 | 18 | 28 | 1.35 | |

Swordsman and halberdier patrol, aggro inside 7 units, and leash at 13. The captain aggros at 9 and leashes at 16. Below half HP the captain's windups become `max(21, base × 3/4)` — 21, 21, and 24 frames — and the order becomes slash, leap, thrust, slash. Bodies (half extents): player 0.32×0.78, swordsman 0.30×0.74, halberdier 0.38×0.82, captain 0.42×0.95.

## Level

One march, 96 tiles wide, about six screens at orthographic size 4.4. Three rooms and two transitions, at x = 30 and x = 58. Village mud, a palisade with a ladder, a puddle, banners, shields, a shrine. Three plank runs over two pits. Courtyard stone, a gate, a second shrine, the captain. Rows, spawns, and room camera clamps live in `games/HoundsOfTheMarches/assets/levels/marches.json`. Another level is another file of the same shape.

## Where the names landed

The slice uses this engine's components. `Transform2D` is `TransformComponent`. `Sprite` is `SpriteRendererComponent`; animated cells are `SubTextureRendererComponent`. Solid `ColliderAABB` volumes are `BoxCollider2DComponent`. Melee tests are `Melee.Aabb`, because enemies are not rigidbodies. `Animator2D`, `Health`, `Stamina`, `Combat`, `AIBrain`, and `Checkpoint` are game components in `assets/scripts/Components.cs`. Systems, in order: Level 80, AI 88, Combat 90, Movement 92, Flow 96, engine physics 100, Animation 110, CameraFollow 112, Hud 113, hierarchy 115, render 150. `Render2D` is the existing scene renderer. It now sorts the 2D batch by `SortOrder`. The game writes Y-sort as `140 − worldY`, because physics pins transform Z to 0.

## Assumptions

Editor Play does not receive `--art`. The editor switch is `assets/artpack.txt`. Command-line `--art` wins when the Runtime is launched with it.

`LevelSystem` takes `ISceneContext` from `Engine.Scene` so it can spawn the march. Samples do not spawn entities; this is the narrow hook. The editor script compiler already references `Engine` from the AppDomain. `System.Text.Json` and `System.IO.Compression` were added to that compiler's runtime references so the level loader and the PNG baker compile in Play.

Title cards and the six WAV hooks are procedural and shared by both packs. External characters are Kenney's CC0 aliens, stand-ins with retouch notes in the asset list, not painted hounds or cats. Enemies raycast for ground and do not physically block the player.
