# Hounds of the Marches — asset list

Both packs share clip names and image keys. Gameplay reads `pack.json`. Paths are relative to `games/HoundsOfTheMarches/assets/`.

## Version B — original (generated here)

Generator: `OriginalAssetBaker` in `games/HoundsOfTheMarches/assets/scripts/OriginalBaker.cs`. A `Raster` draws flat cel shapes, then dilates opaque pixels by a radius so the black ink sits in the PNG. `PngImage` writes RGBA PNGs (zlib). No downloaded images. Run `dotnet run --project games/HoundsOfTheMarches -- --bake` to regenerate. The first Play also bakes if `Original/characters/player.png` or `audio/hit.wav` is missing.

Character sheets are 1800×220, ten cells of 180×220. Outline radius on actors is 5 px inside the cell. Clip map (same for all four sheets):

| Clip | Cells |
|---|---|
| idle | 0 |
| run | 1, 2 |
| air, lightWindup, thrustWindup | 3 (sword cocked back) |
| light, thrust, recover | 4 (thrust) |
| heavyWindup, windup, slashWindup | 5 (overhead) |
| heavy, active, slash | 6 (down-slash) |
| dodge, leap | 7 |
| block, phase2 | 8 (shield forward; captain's cloak opens) |
| hurt | 9 |

| File | Size | What it is |
|---|---|---|
| `Original/characters/player.png` | 1800×220 | Hound-knight: grey plate, ochre tabard, red cloak, muzzle, ear crests, pavise |
| `Original/characters/swordsman.png` | 1800×220 | Amber cat, gold eye, tail, one-hand sword |
| `Original/characters/halberdier.png` | 1800×220 | Wider dark helm, long pole, axe head on the active cells |
| `Original/characters/boss.png` | 1800×220 | Captain, dark coat, gold emblem, cloak, shield |
| `Original/characters/*.json` | — | Frame rects for the sheet |
| `Original/tiles/mud.png` | 64×64 | Village ground, ink edge |
| `Original/tiles/stone.png` | 64×64 | Courtyard ashlar |
| `Original/tiles/bridge.png` | 64×64 | Planks, transparent gaps |
| `Original/tiles/palisade.png` | 64×64 | Stakes |
| `Original/props/puddle.png` | 96×48 | Night-blue puddle |
| `Original/props/ladder.png` | 48×128 | Rope-and-wood ladder |
| `Original/props/gate.png` | 120×180 | Keep gate |
| `Original/props/shrine.png` | 96×140 | Stone shrine, ochre flame |
| `Original/props/banner.png` | 64×120 | Red banner, gold chevron |
| `Original/props/shield.png` | 72×96 | Pavise |
| `Original/props/blood.png` | 80×48 | Stinger stain |
| `Original/props/stinger.png` | 48×48 | Currency |
| `Original/bg/far.png` | 640×360 | Night sky, moon, far hills. Parallax 0.12 |
| `Original/bg/mid.png` | 640×360 | Village masses. Parallax 0.38 |
| `Original/bg/near.png` | 640×360 | Palisade silhouettes. Parallax 0.62 |
| `Original/hud/heart.png` | 48×48 | HP icon |
| `Original/hud/d0.png` … `d9.png` | 40×48 | Stinger digits |
| `shared/title_*.png`, `shared/inscribe_*.png` | text width × ~40 | Heraldic banners. Used by both packs |
| `audio/hit.wav`, `swing.wav`, `parry.wav`, `hurt.wav`, `death.wav`, `checkpoint.wav` | 22050 Hz, 16-bit mono | Placeholder tones, both packs |

World sprite heights for this pack: player 1.9, swordsman 1.85, halberdier 2.1, captain 2.45.

## Version A — external (Kenney, CC0)

Pack: Platformer Pack Redux by Kenney Vleugels (Kenney). License CC0 1.0. Per-file credit is in `games/HoundsOfTheMarches/assets/External/CREDITS.md`. These are alien stand-ins. They are not hounds or cats, and they have no painted attack sheet. The jump pose is the windup telegraph. Retouch if you replace them:

- Blue alien → hound helm, grey plate, ochre tabard, red cloak, pavise on the `front` pose. Keep the thick readable silhouette.
- Green alien → cat ears, amber and black, gold eye. Replace `jump` with a sword cocked back and held for the whole windup (at least 0.35 s; the game already waits 24 frames).
- Yellow alien → heavier helm and a halberd whose head is visible on `hit`. `jump` stays the telegraph.
- Pink alien → captain's coat-of-arms cloak (black field, gold chevron, a cat's head). `front` is the phase-2 pose. Do not trace another game. Do not add rats. Do not shorten enemy windup below 21 frames.

Character frames are full 128×256 PNGs, one file per pose. Sheets: `External/characters/{player,swordsman,halberdier,boss}.json`.

| Role | Files | Frames |
|---|---|---|
| Player (blue) | `alienBlue_{stand,walk1,walk2,jump,hit,duck,front}.png` | idle 1, run 2, attacks reuse jump/hit, dodge duck, block front |
| Swordsman (green) | `alienGreen_*` same poses | same clip map |
| Halberdier (yellow) | `alienYellow_*` | same clip map |
| Captain (pink) | `alienPink_*` | same clip map, phase2 = front |

World heights for this pack: grunts 2.0, halberdier 2.15, captain 2.6.

| Key | File | Size |
|---|---|---|
| mud | `External/kenney/Ground/dirtMid.png` | 128×128 |
| stone | `External/kenney/Ground/stoneMid.png` | 128×128 |
| bridge | `External/kenney/Tiles/bridgeA.png` | 128×128 |
| palisade | `External/kenney/Tiles/fence.png` | 128×128 |
| puddle | `External/kenney/Tiles/water.png` | 128×128 |
| ladder | `External/kenney/Tiles/ladderMid.png` | 128×128 |
| gate | `External/kenney/Tiles/doorClosed_top.png` | 128×128 |
| shrine | `External/kenney/Tiles/torch1.png` | 128×128 |
| banner | `External/kenney/Tiles/sign.png` | 128×128 |
| shield | `External/kenney/Tiles/window.png` | 128×128 |
| blood | `External/kenney/HUD/hudJewel_red.png` | 128×128 |
| coin | `External/kenney/HUD/hudCoin.png` | 128×128 |
| heart | `External/kenney/HUD/hudHeart_full.png` | 128×128 |
| digit0–9 | `External/kenney/HUD/hud0.png` … `hud9.png` | 128×128 |
| parallax_far | `External/kenney/Backgrounds/blue_land.png` | 1024×1024 |
| parallax_mid | `External/kenney/Backgrounds/colored_grass.png` | 1024×1024 |
| parallax_near | `External/kenney/Backgrounds/colored_desert.png` | 1024×1024 |

Titles and WAVs are the shared procedural files, not Kenney. The pack's mouse sprite was left out on purpose.
