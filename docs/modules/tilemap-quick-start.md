# TileMap Editor - Quick Start Guide

## 5-Minute Setup

### Step 1: Prepare Your Tileset
Create or obtain a tileset texture (PNG format):
- **Recommended size**: 256x256, 512x512, or 1024x1024
- **Tile size**: 16x16, 32x32, or 64x64 pixels
- **Layout**: Grid of tiles without spacing
- Save to: `assets/tilesets/your_tileset.png`

### Step 2: Create TileMap Entity
1. In Scene Hierarchy, right-click → Create Entity
2. Name it "TileMap"
3. In Properties panel → Add Component → TileMap

### Step 3: Configure TileMap
In the TileMap component properties:
```
Width: 16
Height: 16
Tile Size: 1.0, 1.0
TileSet Path: assets/tilesets/your_tileset.png
Columns: 8  (texture_width / tile_width)
Rows: 8     (texture_height / tile_height)
```

### Step 4: Open Editor
Click **"Open TileMap Editor"** button

### Step 5: Paint
1. Select a tile from the left palette (click on it)
2. Use tools to paint:
   - **Paint**: Left-click to place tiles
   - **Erase**: Remove tiles
   - **Fill**: Flood-fill an area
3. Pan: Middle mouse button
4. Zoom: Mouse wheel

## Common Operations

### Add a New Layer
1. In TileMap Editor → Layers section
2. Click "Add Layer"
3. Click new layer to make it active
4. Paint on the new layer

### Hide/Show Layers
Click the checkbox next to layer name

### Change Tile Size
In Properties panel, adjust "Tile Size" (e.g., 2.0 for larger tiles)

### Resize TileMap
In Properties panel, change Width/Height values

## Example Workflow: Creating a Small Room

```
1. Create TileMap entity
2. Configure: 10x10 tiles, 1.0 tile size
3. Load dungeon tileset (32x32 pixel tiles, 8x8 grid)
4. Open editor

Layer 0 - "Floor":
5. Select floor tile (id: 0)
6. Fill tool → Click to flood-fill entire map

Layer 1 - "Walls":
7. Add new layer named "Walls"
8. Select wall tile (id: 8)
9. Paint tool → Draw walls around edges

Layer 2 - "Decorations":
10. Add new layer named "Decorations"
11. Select torch tile (id: 24)
12. Paint tool → Add torches on walls

Done! You have a multi-layered room.
```

## Tips

✅ **DO**
- Start simple: single layer, small map
- Use descriptive layer names
- Keep tile size at 1.0 for pixel-perfect rendering
- Save scene frequently

❌ **DON'T**
- Make maps too large (keep under 100x100)
- Forget to set TileSet Path
- Paint without selecting a tile first
- Use too many layers (3-5 is ideal)

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Mouse Wheel | Zoom |
| Middle Mouse | Pan |
| Left Click | Paint/Erase |
| ESC | Close editor |

## Troubleshooting

**Can't see tiles?**
→ Check TileSet Path is correct and file exists

**Wrong tiles shown?**
→ Verify Columns/Rows match your texture dimensions

**Editor won't open?**
→ Make sure entity has TileMapComponent

**Tiles too small/large?**
→ Adjust Tile Size in Properties panel

---

Now you're ready to create levels! For advanced features, see the full documentation.

