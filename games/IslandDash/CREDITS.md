# Credits

Everything shipped with Island Dash is CC0. No attribution is required; it is given here anyway.

## 3D models

**Platformer Kit 4.1** by Kenney (www.kenney.nl), Creative Commons CC0 1.0.
https://kenney.nl/assets/platformer-kit
https://creativecommons.org/publicdomain/zero/1.0/

The license text shipped with the pack is in `assets/models/kenney_platformer/License.txt`. Models used, all `.glb` plus the shared `Textures/colormap.png`:

- Characters: `character-ooli` (player), `character-oodi` (patrol enemy), `character-oozi` (chase enemy)
- Pickups and crates: `jewel`, `heart`, `flag`, `star`, `crate`, `crate-item`, `crate-strong`
- Terrain and platforms: `block-grass`, `block-grass-large`, `platform`, `block-moving`, `block-moving-blue`, `spike-block`
- Hazards and set dressing: `trap-spikes`, `tree`, `tree-pine`, `tree-pine-small`, `flowers`, `flowers-tall`, `grass`, `rocks`, `mushrooms`, `sign`, `fence-straight`, `barrel`, `poles`, `arrow`

Rigs and animation clips inside the character files are ignored. The game poses the whole mesh.

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
