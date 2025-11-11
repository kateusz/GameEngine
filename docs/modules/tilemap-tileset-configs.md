# Sample TileSet Configurations

## Example 1: Retro Dungeon (16x16 tiles)

```json
{
  "name": "Retro Dungeon",
  "texture": "assets/tilesets/dungeon_16x16.png",
  "textureSize": "256x256",
  "tileSize": "16x16",
  "columns": 16,
  "rows": 16,
  "totalTiles": 256,
  "spacing": 0,
  "margin": 0,
  "tiles": {
    "0-15": "Floor variants",
    "16-31": "Wall variants",
    "32-47": "Door/Gate tiles",
    "48-63": "Decoration tiles",
    "64-79": "Treasure/Items",
    "80+": "Special tiles"
  }
}
```

**TileMap Configuration:**
```csharp
Width: 32
Height: 24
Tile Size: 1.0, 1.0
TileSet Path: assets/tilesets/dungeon_16x16.png
Columns: 16
Rows: 16
```

## Example 2: Platformer (32x32 tiles)

```json
{
  "name": "Platformer Tiles",
  "texture": "assets/tilesets/platform_32x32.png",
  "textureSize": "512x512",
  "tileSize": "32x32",
  "columns": 16,
  "rows": 16,
  "totalTiles": 256,
  "tiles": {
    "0-31": "Ground tiles (grass, dirt, stone)",
    "32-63": "Platforms",
    "64-95": "Background decorations",
    "96-127": "Collectibles/Items",
    "128-159": "Hazards (spikes, lava)",
    "160-191": "Interactive (switches, doors)",
    "192+": "Special/Animated tiles"
  }
}
```

**TileMap Configuration:**
```csharp
Width: 40
Height: 22
Tile Size: 1.0, 1.0
TileSet Path: assets/tilesets/platform_32x32.png
Columns: 16
Rows: 16
```

## Example 3: Top-Down RPG (16x16 tiles)

```json
{
  "name": "RPG World",
  "texture": "assets/tilesets/rpg_world_16x16.png",
  "textureSize": "256x256",
  "tileSize": "16x16",
  "columns": 16,
  "rows": 16,
  "layers": [
    {
      "name": "Terrain",
      "zIndex": 0,
      "tiles": "0-63 (grass, water, sand, path)"
    },
    {
      "name": "Objects",
      "zIndex": 1,
      "tiles": "64-127 (trees, rocks, bushes)"
    },
    {
      "name": "Buildings",
      "zIndex": 2,
      "tiles": "128-191 (houses, walls, roofs)"
    },
    {
      "name": "Decorations",
      "zIndex": 3,
      "tiles": "192-255 (flowers, signs, misc)"
    }
  ]
}
```

**TileMap Configuration:**
```csharp
Width: 50
Height: 50
Tile Size: 1.0, 1.0
TileSet Path: assets/tilesets/rpg_world_16x16.png
Columns: 16
Rows: 16

// Create layers
AddLayer("Terrain")    // Ground layer
AddLayer("Objects")    // Trees, rocks
AddLayer("Buildings")  // Structures
AddLayer("Decorations") // Details
```

## Calculating Columns and Rows

### Formula
```
Columns = Texture Width / Tile Width
Rows = Texture Height / Tile Height
```

### Examples

| Texture Size | Tile Size | Columns | Rows | Total Tiles |
|-------------|-----------|---------|------|-------------|
| 256x256     | 16x16     | 16      | 16   | 256         |
| 512x512     | 32x32     | 16      | 16   | 256         |
| 512x256     | 32x32     | 16      | 8    | 128         |
| 1024x1024   | 64x64     | 16      | 16   | 256         |
| 256x128     | 16x16     | 16      | 8    | 128         |

## Creating Your Own Tileset

### Recommended Sizes

**Pixel Art Games:**
- 16x16 tiles: Classic retro look
- 32x32 tiles: Modern pixel art
- 64x64 tiles: High detail pixel art

**Texture Dimensions:**
- Use power-of-2: 256, 512, 1024, 2048
- Square is easiest: 512x512
- Rectangle works: 1024x512

### Layout Guidelines

1. **No spacing between tiles** (for simplicity)
2. **Organize by category** (floors, walls, decorations)
3. **Consistent color palette**
4. **Leave room for expansion**
5. **Number tiles logically** (0-15 floors, 16-31 walls, etc.)

### Tools for Creating Tilesets

- **Aseprite**: Best for pixel art tilesets
- **Tiled Map Editor**: Export tilesets
- **GIMP/Photoshop**: For arranging tiles
- **Pyxel Edit**: Tile-focused pixel editor

## Common Patterns

### 3-Layer Setup (Most Common)
```
Layer 0: "Ground"      - Base terrain
Layer 1: "Walls"       - Obstacles
Layer 2: "Decorations" - Visual details
```

### 5-Layer Setup (Detailed)
```
Layer 0: "Background"   - Far background
Layer 1: "Terrain"      - Ground/floors
Layer 2: "Walls"        - Solid obstacles
Layer 3: "Objects"      - Interactive items
Layer 4: "Foreground"   - Near foreground
```

### Tile Size Conversion

**In Engine:**
- Tile Size = 1.0 means 1 world unit per tile
- Tile Size = 2.0 means 2 world units per tile
- Use 1.0 for pixel-perfect rendering

**Example:**
```csharp
// 16x16 pixel tiles at 1:1 scale
TileSize = new Vector2(1.0f, 1.0f)

// 32x32 pixel tiles at 2:1 scale
TileSize = new Vector2(2.0f, 2.0f)
```

## Free Tileset Resources

- **OpenGameArt.org**: Free game assets
- **Itch.io**: Many free/paid tilesets
- **Kenney.nl**: High-quality free assets
- **GameArt2D.com**: Free sprite sheets

---

Use these configurations as starting points for your own tilemaps!

