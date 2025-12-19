# TileSet Configuration Guide

## Calculating Columns and Rows

Use this formula to determine your tileset grid:
- **Columns** = Texture Width / Tile Width
- **Rows** = Texture Height / Tile Height

### Common Configurations

| Texture Size | Tile Size | Columns | Rows | Total Tiles |
|-------------|-----------|---------|------|-------------|
| 256x256     | 16x16     | 16      | 16   | 256         |
| 512x512     | 32x32     | 16      | 16   | 256         |
| 512x256     | 32x32     | 16      | 8    | 128         |
| 1024x1024   | 64x64     | 16      | 16   | 256         |
| 256x128     | 16x16     | 16      | 8    | 128         |

## Recommended Tile Sizes

**Pixel Art Games:**
- **16x16**: Classic retro look
- **32x32**: Modern pixel art
- **64x64**: High detail pixel art

**Texture Dimensions:**
- Use power-of-2 sizes: 256, 512, 1024, 2048
- Square textures are easiest (e.g., 512x512)
- Rectangular textures work too (e.g., 1024x512)

## Creating Your Own Tileset

### Layout Guidelines

1. **No spacing between tiles** - simplifies UV calculations
2. **Organize by category** - group floors, walls, decorations together
3. **Consistent color palette** - keeps visual coherence
4. **Leave room for expansion** - don't fill the entire texture
5. **Number tiles logically** - e.g., tiles 0-15 for floors, 16-31 for walls

### Recommended Tools

- **Aseprite**: Best for pixel art tilesets
- **Tiled Map Editor**: Can export tilesets
- **GIMP/Photoshop**: For arranging tiles
- **Pyxel Edit**: Tile-focused pixel editor

## Common Layer Patterns

### 3-Layer Setup (Most Common)
- **Layer 0 "Ground"**: Base terrain (grass, dirt, stone)
- **Layer 1 "Walls"**: Obstacles and boundaries
- **Layer 2 "Decorations"**: Visual details and props

### 5-Layer Setup (Detailed)
- **Layer 0 "Background"**: Far background elements
- **Layer 1 "Terrain"**: Ground and floors
- **Layer 2 "Walls"**: Solid obstacles
- **Layer 3 "Objects"**: Interactive items
- **Layer 4 "Foreground"**: Near foreground overlays

## Tile Transforms

### Using Rotation
Rotation allows one tile to serve multiple orientations. A single corner tile can create all four corner variations by rotating 0, 90, 180, and 270 degrees.

**In Editor:** Press `R` to rotate 90 degrees clockwise before painting.

### Using Flip
Flip creates mirror images of tiles:
- **Flip Horizontal**: Creates left/right mirror
- **Flip Vertical**: Creates top/bottom mirror

**In Editor:** Click "Flip H" or "Flip V" in toolbar before painting.

### Transform Best Practices

**DO:**
- Design tiles that work well when rotated (corners, pipes, paths)
- Use symmetrical tiles that benefit from flipping
- Keep one "master" orientation in tileset, derive others with transforms
- This reduces tileset size by 4-8x for applicable tiles

**DON'T:**
- Include all rotations manually in tileset (wastes texture space)
- Rotate non-symmetrical detailed tiles (text, faces, asymmetric objects)
- Forget to test all rotation states look correct

## Unique Tile Deduplication

The tileset palette automatically detects and hides duplicate tiles. If your tileset has multiple identical tiles (e.g., repeated empty spaces), only the first occurrence is shown.

The palette header shows unique vs total tile count (e.g., "45 unique / 256 total").

**How It Works:**
- Each tile's pixel data is hashed
- Tiles with identical pixel content share the same hash
- Only the first occurrence of each unique visual is displayed

**Benefits:**
- Cleaner palette with less scrolling
- No confusion from selecting duplicate tiles
- Works automatically with no configuration needed

## Tile Size in World Space

The **Tile Size** property controls how large tiles appear in world units:
- **Tile Size 1.0**: Each tile is 1x1 world units (typical for pixel-perfect)
- **Tile Size 2.0**: Each tile is 2x2 world units (larger appearance)
- **Tile Size 0.5**: Each tile is 0.5x0.5 world units (smaller appearance)

For a 16x16 tilemap with Tile Size 1.0, the total world size is 16x16 units. With Tile Size 2.0, it becomes 32x32 units.

## Free Tileset Resources

- **OpenGameArt.org**: Free game assets (CC licensed)
- **Itch.io**: Many free and paid tilesets
- **Kenney.nl**: High-quality free assets
- **GameArt2D.com**: Free sprite sheets

---

Use these guidelines as starting points for creating your own tilemaps!
