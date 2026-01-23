# Tilemap Collision Rectangle Editor - Implementation Plan

## Overview
Implement a visual tool for drawing per-tile collision rectangles in the editor, similar to Godot's collision editor. Each tile type in a tileset will have its own collision rectangle that can be visually edited in the viewport.

## User Requirements
- **Per-tile collision**: Each tile type has its own collision rectangle (stored in tileset)
- **Viewport tool**: Visual editing in scene viewport (like Move/Scale tools)
- **Grid-aligned**: Collision rectangles snap to tile boundaries

## Architecture

### Data Model
Store collision rectangles in **normalized tile coordinates (0.0-1.0)** for resolution independence:

```csharp
// New record: TileCollisionRect.cs
public record TileCollisionRect
{
    public required Vector2 Offset { get; init; }  // 0.0-1.0 from tile top-left
    public required Vector2 Size { get; init; }    // 0.0-1.0 relative to tile size
}

// Extend Tile record (Tile.cs)
public record Tile
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required SubTexture2D? SubTexture { get; init; }
    public TileCollisionRect? CollisionRect { get; init; }  // NEW: null = no collision
}
```

**Serialization**: Separate metadata file `{tileset}.meta.json` alongside texture:
```json
{
  "TexturePath": "tileset.png",
  "Columns": 8,
  "Rows": 8,
  "TileCollisions": {
    "0": { "Offset": { "X": 0.0, "Y": 0.0 }, "Size": { "X": 1.0, "Y": 1.0 } },
    "5": { "Offset": { "X": 0.25, "Y": 0.5 }, "Size": { "X": 0.75, "Y": 0.5 } }
  }
}
```

### Physics Integration
Create new component and system for tilemap collision (separate from BoxCollider2D):

- **TileMapCollider2DComponent**: Physics material properties (Density, Friction, Restitution)
- **TileMapPhysicsSystem**: Creates Box2D bodies from tile collision data
- **Strategy**: Single static body with multiple fixtures (one per tile with collision)

### Editor Tool
New viewport tool **TileCollisionTool** for visual editing:

1. User selects tilemap entity
2. Activates TileCollision tool from toolbar (🔳 button)
3. Tool shows collision overlays on all tiles
4. Click tile → select it (yellow highlight)
5. Drag to draw collision rectangle (grid-snapped, cyan preview)
6. Release → saves to tile's collision metadata
7. Right-click → remove collision from tile

**Visual Style**:
- Existing collision: Semi-transparent green wireframe
- Selected tile: Yellow border
- Drawing: Cyan filled rectangle with white border
- Grid snap: 1/8 tile increments

## Implementation Phases

### Phase 1: Data Model (2-3 hours)
Create collision data structures and serialization.

**New Files**:
- `/Engine/Tiles/TileCollisionRect.cs` - Collision rectangle record
- `/Engine/Tiles/TileSetMetadata.cs` - Metadata serialization model

**Modified Files**:
- `/Engine/Tiles/Tile.cs` - Add `CollisionRect` property
- `/Engine/Tiles/TileSet.cs` - Add `LoadMetadata()`, `SaveMetadata()`, `SetTileCollision()` methods

**Key Methods**:
```csharp
// TileSet.cs
public void LoadMetadata();  // Load from {TexturePath}.meta.json
public void SaveMetadata();  // Save to {TexturePath}.meta.json
public void SetTileCollision(int tileId, TileCollisionRect? collision);
```

### Phase 2: Viewport Tool (4-5 hours)
Create visual editing tool following RulerTool pattern.

**New Files**:
- `/Editor/Features/Viewport/Tools/TileCollisionTool.cs` - Viewport tool implementation

**Modified Files**:
- `/Editor/EditorMode.cs` - Add `TileCollision` enum value
- `/Editor/Features/Scene/SceneToolbar.cs` - Add toolbar button (🔳 icon)
- `/Editor/Features/Viewport/ViewportToolManager.cs` - Register TileCollisionTool

**Key Features**:
- Implements `IViewportTool` and `IEntityTargetTool`
- Mouse interaction: Click to select tile, drag to draw rectangle
- Coordinate conversion: World-space ↔ Normalized tile coordinates
- Visual overlay: ImGui DrawList rendering (similar to RulerTool:60-72)
- Grid snapping: Snap to 1/8 tile subdivisions

**Rendering**:
```csharp
public void Render(Vector2[] viewportBounds, OrthographicCamera camera)
{
    var drawList = ImGui.GetWindowDrawList();

    // 1. Render existing collision rectangles (green)
    RenderExistingCollisions(drawList, viewportBounds, camera);

    // 2. Highlight selected tile (yellow)
    if (_selectedTileX >= 0)
        RenderSelectedTileHighlight(drawList, viewportBounds, camera);

    // 3. Render active drawing (cyan)
    if (_isDrawingCollision)
        RenderActiveCollisionDraw(drawList, viewportBounds, camera);
}
```

**DI Registration**: Add to Program.cs or DI container setup:
```csharp
container.Register<TileCollisionTool>(Reuse.Singleton);
```

### Phase 3: Physics Integration (3-4 hours)
Generate Box2D collision bodies from tile collision data.

**New Files**:
- `/Engine/Scene/Components/TileMapCollider2DComponent.cs` - Tilemap collision component
- `/Engine/Scene/Systems/TileMapPhysicsSystem.cs` - Physics body creation system
- `/Editor/ComponentEditors/TileMapCollider2DComponentEditor.cs` - Component inspector

**Modified Files**:
- `/Engine/Scene/Scene.cs` - Create tilemap collision bodies in `OnRuntimeStart()` (after line 136)
- `/Engine/Scene/Systems/SystemPriorities.cs` - Add `TileMapPhysicsSystem = 90`

**Body Creation Strategy** (single static body with multiple fixtures):
```csharp
// Scene.OnRuntimeStart() - Add after RigidBody2D creation (line 136)
var tileMapView = _context.View<TileMapComponent, TileMapCollider2DComponent>();
foreach (var (entity, tileMap, collider) in tileMapView)
{
    CreateTileMapCollisionBodies(entity, tileMap, collider);
}

private void CreateTileMapCollisionBodies(Entity entity, TileMapComponent tileMap, TileMapCollider2DComponent collider)
{
    // 1. Load tileset with collision metadata
    // 2. Create single static Box2D body
    // 3. For each tile with collision:
    //    - Convert normalized collision rect to world space
    //    - Create PolygonShape fixture
    //    - Apply scale from transform
    // 4. Store RuntimeBody reference in component
}
```

**Coordinate Conversion**:
```csharp
// Normalized (0.0-1.0) → World space
var collisionOffsetWorld = collisionRect.Offset * tileMap.TileSize;
var collisionSizeWorld = collisionRect.Size * tileMap.TileSize;

// Apply tile position and transform scale
var tileWorldX = tileX * tileMap.TileSize.X * transform.Scale.X;
var tileWorldY = (tileMap.Height - 1 - tileY) * tileMap.TileSize.Y * transform.Scale.Y; // Y-flip
```

### Phase 4: Editor Polish (2-3 hours)
Optional enhancements for better UX.

**Optional New Files**:
- `/Editor/Panels/TileCollisionEditorPanel.cs` - Side panel for fine-tuning collision values

**Features**:
- Keyboard shortcuts: C key (toggle tool), ESC (deselect), Delete (remove collision)
- Visual distinction: Color-coded tiles with/without collision
- Tooltips and help text
- Fine-tune collision values in side panel (drag Vector2 fields)

## Critical Files Reference

### Files to Create (7 core files)
1. `/Engine/Tiles/TileCollisionRect.cs`
2. `/Engine/Tiles/TileSetMetadata.cs`
3. `/Engine/Scene/Components/TileMapCollider2DComponent.cs`
4. `/Engine/Scene/Systems/TileMapPhysicsSystem.cs`
5. `/Editor/Features/Viewport/Tools/TileCollisionTool.cs`
6. `/Editor/ComponentEditors/TileMapCollider2DComponentEditor.cs`
7. `/Editor/Panels/TileCollisionEditorPanel.cs` (optional)

### Files to Modify (8 files)
1. `/Engine/Tiles/Tile.cs` - Add CollisionRect property
2. `/Engine/Tiles/TileSet.cs` - Metadata loading/saving
3. `/Engine/Scene/Scene.cs` - Body creation in OnRuntimeStart (after line 136)
4. `/Engine/Scene/Systems/SystemPriorities.cs` - Add constant
5. `/Editor/EditorMode.cs` - Add enum value
6. `/Editor/Features/Scene/SceneToolbar.cs` - Add button
7. `/Editor/Features/Viewport/ViewportToolManager.cs` - Register tool
8. DI container (Program.cs or similar) - Register new services

## Testing & Verification

### Manual Testing Checklist

**Editor Workflow**:
1. Load scene with tilemap entity
2. Activate TileCollision tool from toolbar (🔳 button or C key)
3. Click tile in viewport → yellow highlight appears
4. Drag to draw collision rectangle → cyan preview with grid snapping
5. Release mouse → green overlay appears, metadata saved
6. Right-click tile → collision removed
7. Save scene → verify `{tileset}.meta.json` file created
8. Reload scene → collision overlays render correctly

**Physics Integration**:
1. Add TileMapCollider2DComponent to tilemap entity
2. Play scene (Runtime mode)
3. Create dynamic entity (RigidBody2D + BoxCollider2D) above tilemap
4. Entity falls and collides with tiles that have collision rectangles
5. Entity passes through tiles without collision
6. Enable "Show Collider Bounds" → debug wireframes match editor overlays

**Edge Cases**:
- Empty tilemap (no tiles) → tool doesn't crash
- Tilemap without tileset → shows warning
- Large tilemap (100×100) → performance acceptable (<16ms frame time)
- Tilemap with rotation/scale → collision transforms correctly

### Visual Debugging
Enable `DebugSettings.ShowColliderBounds` in runtime:
- Editor overlays (green) should match runtime debug wireframes exactly
- Any mismatch indicates coordinate conversion bug

### Integration Tests
```csharp
// TileMapPhysicsSystemTests.cs
[Fact]
public void CreatesCollisionBodies_ForTilesWithCollision()
{
    // Arrange: Create tilemap with collision metadata
    // Act: Call OnRuntimeStart()
    // Assert: Verify RuntimeBody created with correct fixtures
}

[Fact]
public void ConvertsNormalizedToWorldSpace_Correctly()
{
    // Arrange: TileCollisionRect with normalized coords
    // Act: Convert to world space with tile size
    // Assert: World coords match expected values
}
```

## Design Decisions

### Why Normalized Coordinates?
**Pros**: Resolution-independent, simpler serialization, easier scaling
**Cons**: Requires conversion to world-space
**Verdict**: Normalized (0.0-1.0) for flexibility

### Why Single Body with Multiple Fixtures?
**Pros**: Better performance, simpler memory management, lower Box2D overhead
**Cons**: All tiles must be static, can't have dynamic tiles
**Verdict**: Single body (initial implementation). Can add separate bodies option later.

### Why Separate Metadata File?
**Pros**: Plain text (Git-friendly), human-readable, doesn't modify source texture
**Cons**: Two files per tileset
**Verdict**: `.meta.json` file alongside texture (e.g., `tileset.png.meta.json`)

## Success Criteria
- ✅ Users can visually draw collision rectangles on tilemap tiles
- ✅ Collision data persists across editor sessions
- ✅ Physics system correctly generates Box2D bodies from tile collision
- ✅ Performance acceptable for tilemaps up to 100×100 tiles
- ✅ Tool integrates seamlessly with existing viewport tools
- ✅ Debug visualization matches editor overlays exactly

## Future Enhancements (Not in Initial Implementation)
- Polygon collision shapes (arbitrary vertices, not just rectangles)
- Per-instance collision overrides (customize collision per tilemap)
- Auto-generate collision from tile alpha channel
- Collision templates (full, half-top, half-bottom, slopes)
- Runtime collision modification (destructible terrain)
- Collision optimization (merge adjacent tiles into larger rectangles)