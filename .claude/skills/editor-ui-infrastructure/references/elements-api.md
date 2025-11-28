# UI Elements API Reference

Complete API documentation for Editor UI Elements. Elements are complex, stateful UI components for specific interactions.

## Table of Contents
1. [Drag-Drop Targets](#drag-drop-targets)
   - [TextureDropTarget](#texturedrop target)
   - [AudioDropTarget](#audiodrop target)
   - [MeshDropTarget](#meshdrop target)
   - [ModelTextureDropTarget](#modeltexturedrop target)
   - [PrefabDropTarget](#prefabdrop target)
2. [ComponentSelector](#componentselector)
3. [EntityContextMenu](#entitycontextmenu)
4. [PrefabManager](#prefabmanager)

---

## Drag-Drop Targets

Specialized drop targets for different asset types with built-in validation, error handling, and visual feedback.

### TextureDropTarget

Drop target for texture files (.png, .jpg, .jpeg, .bmp, .tga).

#### Usage

```csharp
public class SpriteRendererEditor : IComponentEditor<SpriteRendererComponent>
{
    private readonly IAssetsManager _assetsManager;

    public SpriteRendererEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawEditor(SpriteRendererComponent component)
    {
        TextureDropTarget.Draw(
            label: "Texture",
            currentPath: component.TexturePath,
            onTextureChanged: newPath => component.TexturePath = newPath,
            assetsManager: _assetsManager
        );
    }
}
```

#### Parameters

- `label` (string): Display label for the drop target
- `currentPath` (string): Current texture path (can be null)
- `onTextureChanged` (Action<string>): Callback when valid texture dropped
- `assetsManager` (IAssetsManager): Asset manager for validation

#### Features

- ✅ Validates file extension (.png, .jpg, etc.)
- ✅ Shows thumbnail preview if texture loaded
- ✅ Displays "None" if no texture assigned
- ✅ Highlights on hover during drag
- ✅ Shows error message if invalid file dropped

#### Visual Behavior

- **No texture**: Displays button with "(None)"
- **With texture**: Displays texture name + thumbnail
- **During drag**: Highlights border if valid texture
- **Invalid drop**: Shows error message briefly

### AudioDropTarget

Drop target for audio files (.wav, .ogg, .mp3).

#### Usage

```csharp
public class AudioSourceEditor : IComponentEditor<AudioSourceComponent>
{
    private readonly IAssetsManager _assetsManager;

    public AudioSourceEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawEditor(AudioSourceComponent component)
    {
        AudioDropTarget.Draw(
            label: "Audio Clip",
            currentPath: component.AudioClipPath,
            onAudioChanged: newPath => component.AudioClipPath = newPath,
            assetsManager: _assetsManager
        );
    }
}
```

#### Parameters

- `label` (string): Display label for the drop target
- `currentPath` (string): Current audio clip path (can be null)
- `onAudioChanged` (Action<string>): Callback when valid audio file dropped
- `assetsManager` (IAssetsManager): Asset manager for validation

#### Features

- ✅ Validates audio file formats (.wav, .ogg, .mp3)
- ✅ Displays audio file name or "(None)"
- ✅ Highlights during valid drag operation
- ✅ Error feedback for invalid files

### MeshDropTarget

Drop target for mesh files (.obj, .fbx, .gltf, .glb).

#### Usage

```csharp
public class MeshComponentEditor : IComponentEditor<MeshComponent>
{
    private readonly IAssetsManager _assetsManager;

    public MeshComponentEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawEditor(MeshComponent component)
    {
        MeshDropTarget.Draw(
            label: "Mesh",
            currentPath: component.MeshPath,
            onMeshChanged: newPath => component.MeshPath = newPath,
            assetsManager: _assetsManager
        );
    }
}
```

#### Parameters

- `label` (string): Display label for the drop target
- `currentPath` (string): Current mesh path (can be null)
- `onMeshChanged` (Action<string>): Callback when valid mesh dropped
- `assetsManager` (IAssetsManager): Asset manager for validation

#### Features

- ✅ Validates mesh file formats
- ✅ Shows mesh file name or "(None)"
- ✅ Drag operation visual feedback
- ✅ Validates mesh can be loaded

### ModelTextureDropTarget

Drop target for model textures (used in ModelRenderer component).

#### Usage

```csharp
public class ModelRendererEditor : IComponentEditor<ModelRendererComponent>
{
    private readonly IAssetsManager _assetsManager;

    public ModelRendererEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawEditor(ModelRendererComponent component)
    {
        ModelTextureDropTarget.Draw(
            label: "Diffuse Texture",
            currentPath: component.DiffuseTexturePath,
            onTextureChanged: newPath => component.DiffuseTexturePath = newPath,
            assetsManager: _assetsManager
        );
    }
}
```

#### Parameters

Same as TextureDropTarget, specialized for 3D model textures.

### PrefabDropTarget

Drop target for prefab assets (.prefab).

#### Usage

```csharp
public class PrefabSpawnerEditor : IComponentEditor<PrefabSpawnerComponent>
{
    private readonly IAssetsManager _assetsManager;

    public PrefabSpawnerEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawEditor(PrefabSpawnerComponent component)
    {
        PrefabDropTarget.Draw(
            label: "Prefab",
            currentPath: component.PrefabPath,
            onPrefabChanged: newPath => component.PrefabPath = newPath,
            assetsManager: _assetsManager
        );
    }
}
```

#### Parameters

- `label` (string): Display label for the drop target
- `currentPath` (string): Current prefab path (can be null)
- `onPrefabChanged` (Action<string>): Callback when valid prefab dropped
- `assetsManager` (IAssetsManager): Asset manager for validation

#### Features

- ✅ Validates .prefab file extension
- ✅ Shows prefab name or "(None)"
- ✅ Validates prefab can be loaded

### Common Drop Target Features

All drop targets share these behaviors:

**Validation**:
- File extension validation
- Asset existence check
- Format validation via AssetsManager

**Visual Feedback**:
- Hover highlight during drag
- Border color change for valid drops
- Error message display for invalid drops

**Usage Pattern**:
```csharp
// ❌ WRONG - Custom drag-drop implementation
ImGui.Text("Texture:");
ImGui.SameLine();
ImGui.Button(texturePath ?? "None");
if (ImGui.BeginDragDropTarget())
{
    var payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
    if (payload.NativePtr != null)
    {
        // Complex validation, error handling...
    }
    ImGui.EndDragDropTarget();
}

// ✅ CORRECT - Use specialized drop target
TextureDropTarget.Draw("Texture",
    currentPath: component.TexturePath,
    onTextureChanged: path => component.TexturePath = path,
    assetsManager: _assetsManager
);
```

---

## ComponentSelector

Popup UI for adding components to entities. Provides searchable list of all available component types.

### Usage

```csharp
public class PropertiesPanel : IPanel
{
    private readonly ComponentSelector _componentSelector = new();
    private Entity? _selectedEntity;

    public void OnImGuiRender()
    {
        if (_selectedEntity.HasValue)
        {
            // "Add Component" button
            if (ButtonDrawer.DrawButton("Add Component"))
            {
                _componentSelector.Show(_selectedEntity.Value);
            }

            // Must call every frame to render popup
            _componentSelector.Draw();
        }
    }
}
```

### API

#### Constructor
```csharp
var selector = new ComponentSelector();
```

No constructor parameters needed. ComponentSelector discovers available components automatically via reflection.

#### Show(Entity entity)
Opens the component selector popup for the specified entity.

```csharp
_componentSelector.Show(selectedEntity);
```

**Parameters**:
- `entity` (Entity): Entity to add component to

#### Draw()
Renders the popup. **Must be called every frame** even when popup not visible.

```csharp
_componentSelector.Draw();
```

### Features

- ✅ Searchable component list (type to filter)
- ✅ Shows all available ECS components
- ✅ Alphabetically sorted
- ✅ Automatic component instantiation
- ✅ Prevents duplicate components (where appropriate)
- ✅ Keyboard navigation support

### Component Discovery

ComponentSelector automatically discovers all types implementing component interfaces:
- Classes with component attributes
- Registered component types in ECS system

### Usage Guidelines

**Dependency Injection**:
```csharp
// ✅ CORRECT - Create as field, no DI needed
private readonly ComponentSelector _componentSelector = new();

// ❌ WRONG - Don't inject (stateful, per-panel)
public PropertiesPanel(ComponentSelector componentSelector) { }
```

**Lifecycle**:
```csharp
public void OnImGuiRender()
{
    // Show when button clicked
    if (ButtonDrawer.DrawButton("Add Component"))
        _componentSelector.Show(entity);

    // MUST call every frame
    _componentSelector.Draw();
}
```

---

## EntityContextMenu

Right-click context menu for entity operations (duplicate, delete, create child, etc.).

### Usage

```csharp
public class SceneHierarchyPanel : IPanel
{
    private readonly EntityContextMenu _contextMenu = new();
    private readonly Scene _scene;

    public SceneHierarchyPanel(Scene scene)
    {
        _scene = scene;
    }

    public void OnImGuiRender()
    {
        foreach (var entity in _scene.Entities)
        {
            ImGui.Selectable(entity.Name);

            // Show context menu on right-click
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _contextMenu.Show(entity, _scene);
            }
        }

        // Must call every frame
        _contextMenu.Draw();
    }
}
```

### API

#### Constructor
```csharp
var contextMenu = new EntityContextMenu();
```

#### Show(Entity entity, Scene scene)
Opens context menu for the specified entity.

```csharp
_contextMenu.Show(entity, scene);
```

**Parameters**:
- `entity` (Entity): Target entity
- `scene` (Scene): Current scene context

#### Draw()
Renders the context menu. **Must be called every frame**.

```csharp
_contextMenu.Draw();
```

### Features

- ✅ Duplicate Entity
- ✅ Delete Entity
- ✅ Create Child Entity
- ✅ Copy/Paste Entity (with components)
- ✅ Rename Entity
- ✅ Create Empty Child
- ✅ Keyboard shortcuts displayed

### Menu Items

| Action | Shortcut | Description |
|--------|----------|-------------|
| Duplicate | Ctrl+D | Duplicates entity with all components |
| Delete | Delete | Deletes entity and children |
| Rename | F2 | Opens rename dialog |
| Create Child | - | Creates empty child entity |
| Copy | Ctrl+C | Copies entity to clipboard |
| Paste | Ctrl+V | Pastes entity from clipboard |

### Usage Guidelines

**Right-Click Pattern**:
```csharp
// Render entity in hierarchy
ImGui.Selectable(entity.Name);

// Check for right-click on the item just rendered
if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
{
    _contextMenu.Show(entity, scene);
}
```

**Lifecycle**:
- Create once per panel (as field)
- Call `Show()` on right-click
- Call `Draw()` every frame

---

## PrefabManager

UI for creating and instantiating prefabs from entities.

### Usage

```csharp
public class PropertiesPanel : IPanel
{
    private readonly IPrefabManager _prefabManager;
    private Entity? _selectedEntity;

    public PropertiesPanel(IPrefabManager prefabManager)
    {
        _prefabManager = prefabManager;
    }

    public void OnImGuiRender()
    {
        if (_selectedEntity.HasValue)
        {
            if (ButtonDrawer.DrawButton("Create Prefab"))
            {
                _prefabManager.CreatePrefabFromEntity(_selectedEntity.Value);
            }
        }
    }
}
```

### API

#### CreatePrefabFromEntity(Entity entity)
Creates prefab asset from entity.

```csharp
_prefabManager.CreatePrefabFromEntity(entity);
```

**Behavior**:
- Opens save dialog for prefab location
- Serializes entity and all components
- Saves .prefab file to project assets
- Preserves component relationships

#### InstantiatePrefab(string prefabPath, Scene scene)
Instantiates prefab into scene.

```csharp
Entity entity = _prefabManager.InstantiatePrefab("Assets/Prefabs/Player.prefab", scene);
```

**Returns**: Newly created entity

#### InstantiatePrefabAtPosition(string prefabPath, Scene scene, Vector3 position)
Instantiates prefab at specific position.

```csharp
Entity entity = _prefabManager.InstantiatePrefabAtPosition(
    "Assets/Prefabs/Enemy.prefab",
    scene,
    new Vector3(10, 0, 5)
);
```

### Features

- ✅ Serializes entire entity hierarchy
- ✅ Preserves all component data
- ✅ Handles asset references (textures, meshes, etc.)
- ✅ Supports nested prefabs
- ✅ Validates prefab before instantiation

### Prefab Workflow

**Creating Prefabs**:
1. Configure entity in scene
2. Select entity in hierarchy
3. Click "Create Prefab" button
4. Choose save location
5. Prefab asset created

**Using Prefabs**:
1. Drag prefab from Content Browser to scene
2. Or use `InstantiatePrefab()` via code
3. Entity created with all components
4. Modify instance independently

### Usage Guidelines

**Dependency Injection**:
```csharp
// ✅ CORRECT - Inject IPrefabManager
public MyPanel(IPrefabManager prefabManager)
{
    _prefabManager = prefabManager;
}

// ❌ WRONG - Don't create directly
var prefabManager = new PrefabManager();
```

**Prefab Best Practices**:
- Use prefabs for reusable entities (enemies, props, UI elements)
- Keep prefab hierarchy shallow (avoid deep nesting)
- Use descriptive names for prefab assets
- Store prefabs in organized folder structure (Assets/Prefabs/)

---

## Common Patterns

### Component Editor with Drop Target
```csharp
public class MyComponentEditor : IComponentEditor<MyComponent>
{
    private readonly IAssetsManager _assetsManager;

    public MyComponentEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawEditor(MyComponent component)
    {
        TextureDropTarget.Draw("Icon",
            component.IconPath,
            path => component.IconPath = path,
            _assetsManager
        );
    }
}
```

### Entity Hierarchy with Context Menu
```csharp
public class SceneHierarchyPanel
{
    private readonly EntityContextMenu _contextMenu = new();

    public void OnImGuiRender()
    {
        foreach (var entity in scene.Entities)
        {
            ImGui.Selectable(entity.Name);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                _contextMenu.Show(entity, scene);
        }

        _contextMenu.Draw();
    }
}
```

### Add Component Flow
```csharp
public class PropertiesPanel
{
    private readonly ComponentSelector _selector = new();

    public void OnImGuiRender()
    {
        // Render existing components...

        LayoutDrawer.DrawSpacing();

        if (ButtonDrawer.DrawFullWidthButton("+ Add Component"))
            _selector.Show(selectedEntity);

        _selector.Draw();
    }
}
```

### Prefab Creation Flow
```csharp
public class EntityActions
{
    private readonly IPrefabManager _prefabManager;

    public void CreatePrefabFromSelection(Entity entity)
    {
        if (ButtonDrawer.DrawColoredButton("Save as Prefab", MessageType.Success))
        {
            _prefabManager.CreatePrefabFromEntity(entity);
            TextDrawer.DrawText("Prefab created successfully!", MessageType.Success);
        }
    }
}
```

---

## See Also

- [drawers-api.md](drawers-api.md) - Static UI utilities (buttons, modals, tables)
- [constants-catalog.md](constants-catalog.md) - EditorUIConstants reference
- [../SKILL.md](../SKILL.md) - Main UI infrastructure guide
