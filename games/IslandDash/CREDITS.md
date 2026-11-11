# Credits

Everything shipped with Island Dash is CC0. No attribution is required; it is given here anyway.

## 3D models

**Platformer Kit 4.1** by Kenney (www.kenney.nl), Creative Commons CC0 1.0.
https://kenney.nl/assets/platformer-kit
https://creativecommons.org/publicdomain/zero/1.0/

The license text shipped with the pack is in `assets/models/kenney_platformer/License.txt`. Models used (characters, pickups, crates, hazards, a few props):

- Characters: `character-ooli` (player), `character-oodi` (patrol enemy), `character-oozi` (chase enemy)
- Pickups and crates: `jewel`, `heart`, `flag`, `star`, `crate`, `crate-item`, `crate-strong`
- Hazards / leftover props: `trap-spikes`, `spike-block`, `barrel`, `poles`, `arrow`

Rigs and animation clips inside the character files are ignored. The game poses the whole mesh.

**Nature Kit 2.1** by Kenney (www.kenney.nl), Creative Commons CC0 1.0.
https://kenney.nl/assets/nature-kit

License text: `assets/models/kenney_nature/License.txt`. Curated `.glb` subset used for the outdoor look (paths, rivers, irregular grass pads, cliffs, bridge, trees, rocks, flowers, fences):

- Ground: `ground_pathStraight`, `ground_pathRocks`, `ground_pathSide`, `ground_pathOpen`, `ground_grass`, `ground_riverStraight`, `ground_riverBend`, `ground_riverSide`, …
- Platforms / cliffs / bridge: `platform_grass`, `cliff_large_rock`, `cliff_rock`, `cliff_half_rock`, `cliff_corner_rock`, `cliff_blockSlope_rock`, `bridge_woodRound`, `bridge_wood`
- Trees / plants / props: `tree_default`, `tree_oak`, `tree_detailed`, `tree_pineTallA`, `tree_pineRoundA`, `tree_pineSmallA`, `flower_*`, `grass_large`, `plant_bush*`, `rock_*`, `mushroom_*`, `fence_simple`, `sign`

## Audio

Kenney, Creative Commons CC0 1.0. Files were renamed; the originals are below.

**Digital Audio** — https://kenney.nl/assets/digital-audio

| Game file | Original |
| --- | --- |
| `audio/jump.ogg` | `phaseJump1.ogg` |
| `audio/double_jump.ogg` | `phaseJump3.ogg` |
| `audio/spin.ogg` | `phaserUp2.ogg` |
| `audio/death.ogg` | `lowDown.ogg` |
| `audio/extra_life.ogg` | `powerUp7.ogg` |
| `audio/level_complete.ogg` | `powerUp11.ogg` |
| `audio/game_over.ogg` | `zapThreeToneDown.ogg` |

**Impact Sounds** — https://kenney.nl/assets/impact-sounds

| Game file | Original |
| --- | --- |
| `audio/crate_break.ogg` | `impactWood_medium_000.ogg` |
| `audio/stomp.ogg` | `impactPunch_medium_000.ogg` |
| `audio/land.ogg` | `impactSoft_heavy_000.ogg` |

**Interface Sounds** — https://kenney.nl/assets/interface-sounds

| Game file | Original |
| --- | --- |
| `audio/collect.ogg` | `glass_001.ogg` |
| `audio/checkpoint.ogg` | `confirmation_002.ogg` |
| `audio/menu.ogg` | `maximize_003.ogg` |

## Sky

**Kloofendal 48d Partly Cloudy (Pure Sky)**, 2k HDR, from Poly Haven (https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky). CC0. Greg Zaal (original photograph), Jarod Guest (sky edits).

The file in `assets/hdri/` is that HDR with RGB scaled to 55%. The engine's tonemap has no exposure control, and the unmodified HDR bloomed the whole frame to white. The scale is the only change.
