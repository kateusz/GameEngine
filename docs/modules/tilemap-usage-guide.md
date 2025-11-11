# TileMap Usage Guide - From Editor to Game

## What You've Done So Far ✅

1. ✅ Created a TileMap entity
2. ✅ Configured the tileset (path, columns, rows)
3. ✅ Opened the TileMap Editor
4. ✅ Painted some tiles

## How to Use Your TileMap in the Game

### Understanding What Happens

When you paint tiles in the editor, they're stored in the `TileMapComponent` on your entity. The `TileMapRenderSystem` automatically renders these tiles when you run the game.

---

## Step-by-Step: Using Your TileMap

### 1. **Save Your Scene**
First, save your scene so the tilemap data is persisted.

**In Editor:**
- `Scene... → Save` (or `Ctrl+S`)
- Your scene file (`.scene` JSON) now contains all tile data

**What's Saved:**
```json
{
  "TileMapComponent": {
    "Width": 16,
    "Height": 16,
    "TileSize": { "X": 1.0, "Y": 1.0 },
    "TileSetPath": "assets/tilesets/tiles.png",
    "Layers": [
      {
        "Name": "Ground",
        "Tiles": [[0, 1, 2], [3, -1, 4], ...]  // Your painted tiles!
      }
    ]
  }
}
```

### 2. **Run Your Game**
When you play the scene, the TileMapRenderSystem automatically:

1. Loads the tileset texture
2. Creates SubTexture2D for each tile
3. Renders all tiles in each layer
4. Respects layer visibility and opacity

**In Editor:**
- Click the **Play** button in the toolbar
- Or use `Scene → Play`

### 3. **See Your Tiles Rendered**
The tiles will appear in the game viewport at the position of your TileMap entity.

**Positioning:**
- TileMap entity's `Transform` determines where the tilemap appears
- Each tile is positioned relative to the entity transform
- `TileSize` property controls how large tiles appear in world units

---

## Practical Examples

### Example 1: Simple Platform Level

```
1. Create TileMap entity at position (0, 0, 0)
2. Set TileSize to (1.0, 1.0) - each tile = 1 world unit
3. Paint:
   - Ground layer: grass/dirt tiles at bottom
   - Walls layer: wall tiles on sides
4. Save scene
5. Play → See your platform level!
```

### Example 2: Top-Down RPG Map

```
1. Create TileMap entity
2. Add layers:
   - "Terrain" - grass, water, paths
   - "Objects" - trees, rocks, buildings
   - "Decorations" - flowers, signs
3. Paint each layer
4. Toggle layer visibility to work on specific layers
5. Save and play!
```

### Example 3: Multiple Tilemaps

You can have multiple TileMap entities in one scene:

```
Entity 1: "Background TileMap"
- Position: (0, 0, -1) - Behind player
- Ground tiles, environment

Entity 2: "Foreground TileMap"  
- Position: (0, 0, 1) - In front of player
- Decorative overlays, effects

Entity 3: "Collision TileMap"
- Invisible (layer opacity = 0)
- Only used for collision detection
```

---

## Understanding Properties

### TileMapComponent Properties

| Property | What It Does | Example |
|----------|--------------|---------|
| **Width** | Number of tiles horizontally | 20 |
| **Height** | Number of tiles vertically | 15 |
| **TileSize** | World-space size of each tile | (1.0, 1.0) |
| **TileSetPath** | Path to spritesheet texture | "assets/tiles.png" |
| **TileSetColumns** | Columns in spritesheet | 20 |
| **TileSetRows** | Rows in spritesheet | 13 |
| **Layers** | Multiple tile layers | Ground, Walls, Decorations |

### TileSize Explained

`TileSize` controls the **world-space size** of tiles:

- **TileSize = (1.0, 1.0)**: Each tile is 1x1 world units (typical)
- **TileSize = (2.0, 2.0)**: Each tile is 2x2 world units (larger)
- **TileSize = (0.5, 0.5)**: Each tile is 0.5x0.5 world units (smaller)

**Example:**
```csharp
// For a 16x16 grid with TileSize (1.0, 1.0)
Total world size = 16 * 1.0 = 16x16 world units

// With TileSize (2.0, 2.0)
Total world size = 16 * 2.0 = 32x32 world units
```

---

## Working with Layers

### Layer Properties

Each layer has:
- **Name**: Identify the layer ("Ground", "Walls", etc.)
- **Visible**: Show/hide in editor and game
- **Opacity**: 0.0 (transparent) to 1.0 (opaque)
- **ZIndex**: Render order (higher = drawn on top)

### Layer Workflow

**In Editor:**
1. Click a layer to make it active
2. Paint tiles (only affects active layer)
3. Toggle visibility to see/hide layers
4. Use "Add Layer" to create new layers
5. Use "Remove Layer" to delete layers

**At Runtime:**
- All visible layers render automatically
- Layers sorted by ZIndex (lower first)
- Opacity applied during rendering

---

## Programmatic Access

You can also manipulate tilemaps from code:

### C# Script Example

```csharp
public class TileMapController : ScriptableEntity
{
    public override void OnUpdate(TimeSpan deltaTime)
    {
        var tileMap = Entity.GetComponent<TileMapComponent>();
        
        // Get tile at position
        int tileId = tileMap.GetTile(x: 5, y: 3, layer: 0);
        
        // Set tile at position
        tileMap.SetTile(x: 5, y: 3, tileIndex: 12, layer: 0);
        
        // Add new layer
        tileMap.AddLayer("Dynamic Layer");
        
        // Resize tilemap
        tileMap.Resize(newWidth: 32, newHeight: 24);
        
        // Change layer visibility
        tileMap.Layers[0].Visible = false;
        
        // Change layer opacity
        tileMap.Layers[1].Opacity = 0.5f;
    }
}
```

### Use Cases for Code Access

- **Procedural generation**: Generate tiles algorithmically
- **Destructible terrain**: Remove tiles when destroyed
- **Dynamic levels**: Change tiles based on game state
- **Day/night cycle**: Swap tiles or change opacity
- **Secret paths**: Reveal hidden tiles when triggered

---

## Common Workflows

### Workflow 1: Creating a Level

1. ✅ Create TileMap entity
2. ✅ Configure tileset (path, columns, rows)
3. ✅ Open TileMap Editor
4. ✅ Paint ground layer
5. ✅ Add walls layer, paint walls
6. ✅ Add decorations layer, add details
7. ✅ Save scene
8. ✅ Test in Play mode
9. ⚙️ Adjust TileSize if needed
10. ⚙️ Add collision components if needed

### Workflow 2: Importing Existing Tilemap

If you have a tilemap from another tool (Tiled, etc.):

1. Export as image or data
2. Create TileMap entity
3. Set tileset to your spritesheet
4. Either:
   - Paint manually in editor, OR
   - Write a script to import data programmatically

### Workflow 3: Multi-Layer Scene

1. Create base TileMap for terrain
2. Create separate TileMap for buildings (different transform)
3. Create TileMap for overlay effects
4. Each can use different tilesets
5. Position using Transform component

---

## Performance Tips

### Optimize Rendering

✅ **DO:**
- Use reasonable tilemap sizes (< 100x100 for dense levels)
- Keep layer count low (3-5 typical)
- Use layer visibility to hide unused layers
- Reuse tilesets across multiple tilemaps

❌ **DON'T:**
- Create massive 1000x1000 tilemaps
- Use 20+ layers
- Load different textures for each tilemap unnecessarily

### Memory Usage

Each tile = 4 bytes (int32)
- 16x16 tilemap = 256 tiles = 1 KB
- 100x100 tilemap = 10,000 tiles = 40 KB
- With 3 layers = 120 KB

Very efficient! Even large tilemaps are lightweight.

---

## Troubleshooting

### Tiles Not Rendering

**Problem:** Painted tiles don't show in game

**Solutions:**
1. ✅ Check TileSetPath is correct
2. ✅ Verify Columns/Rows match your texture
3. ✅ Ensure layer is Visible
4. ✅ Check entity has TransformComponent
5. ✅ Verify scene was saved

### Wrong Tiles Displayed

**Problem:** Different tiles than what you painted

**Solutions:**
1. ✅ Recalculate Columns/Rows (texture size / tile size)
2. ✅ Check for spacing/margin in tileset
3. ✅ Verify tile indices are correct
4. ✅ Reload the scene

### Tiles Too Large/Small

**Problem:** Tiles don't match expected size

**Solutions:**
1. ✅ Adjust TileSize property (world units)
2. ✅ Check camera zoom/position
3. ✅ Verify entity Transform scale

---

## Next Steps

Now that you know how to use tilemaps, you can:

### 1. **Add Collision**
```csharp
// Attach colliders based on tile data
if (tileMap.GetTile(x, y) == WALL_TILE_ID)
{
    // Add BoxCollider2D here
}
```

### 2. **Implement Tile Properties**
Use the `Tile.CustomProperties` dictionary:
```csharp
tile.CustomProperties["IsCollidable"] = true;
tile.CustomProperties["Damage"] = 10;
tile.CustomProperties["Sound"] = "water";
```

### 3. **Create Prefabs**
Save tilemap entities as prefabs for reuse:
- "Forest Section.prefab"
- "Dungeon Room.prefab"
- "City Block.prefab"

### 4. **Build Level Editor**
Extend the tilemap system:
- Custom tile properties editor
- Collision layer painting
- Auto-tiling rules
- Tile animation support

---

## Quick Reference

### Essential Shortcuts

| Action | Shortcut |
|--------|----------|
| Save Scene | Ctrl+S |
| Play Scene | F5 (if configured) |
| Open TileMap Editor | Click button in Properties |
| Close Editor | Click X on window |

### Essential Workflow

```
1. Add TileMap component to entity
2. Configure tileset properties
3. Click "Open TileMap Editor"
4. Select tile from palette
5. Click/drag to paint on canvas
6. Use tools: Paint, Erase, Fill
7. Manage layers as needed
8. Close editor (X button)
9. Save scene (Ctrl+S)
10. Play and see your tiles!
```

---

## Summary

Your painted tiles are now:
- ✅ Stored in the scene file
- ✅ Rendered automatically by TileMapRenderSystem
- ✅ Editable any time in the editor
- ✅ Accessible from C# scripts
- ✅ Ready to use in your game!

**Just save your scene and play!** The tiles will render automatically.

---

**Need more help?** Check the full documentation:
- `docs/modules/tilemap-editor.md` - Complete technical docs
- `docs/modules/tilemap-quick-start.md` - Quick start guide
- `docs/modules/tilemap-tileset-configs.md` - Tileset examples

**Happy level building! 🎮**

