---
name: editor-ui-infrastructure
description: Guide proper usage of Editor UI infrastructure including Drawers (ButtonDrawer, ModalDrawer, TableDrawer, etc.), Elements (drag-drop targets, ComponentSelector), FieldEditors, and EditorUIConstants. Use when implementing editor panels, component editors, or any ImGui UI code to ensure consistency and code reuse.
---

# Editor UI Infrastructure

## Overview
This skill provides comprehensive guidance for using the Editor's UI infrastructure, ensuring consistent styling, proper code reuse, and adherence to established UI patterns across all editor panels and component editors.

## When to Use
Invoke this skill when:
- Implementing editor panels or component editors
- Adding UI elements to existing panels
- Questions about which UI utility to use
- Implementing drag-and-drop functionality
- Creating modal dialogs or confirmation prompts
- Rendering tables, trees, or structured data
- Adding buttons with consistent styling
- Working with field editors for primitive types

## Editor UI Infrastructure Components

### 1. UI Drawers (`Editor/UI/Drawers/`)

**Purpose**: Static utility classes for drawing common UI patterns with consistent styling.

#### ButtonDrawer
Provides 15+ button variants with consistent sizing and behavior.

**When to Use**:
- ✅ Always use ButtonDrawer instead of raw `ImGui.Button()`
- ✅ Use `DrawColoredButton()` for semantic actions (success/error/warning)
- ✅ Use `DrawIconButton()` for toolbar icons
- ✅ Use `DrawToggleButton()` for on/off states
- ✅ Use `DrawModalButtonPair()` for OK/Cancel dialogs

**Available Methods**:
```csharp
// Standard button with EditorUIConstants sizing
ButtonDrawer.DrawButton("Save");

// Custom size button
ButtonDrawer.DrawButton("Export", width: 150, height: 30);

// Full-width button (useful for lists)
ButtonDrawer.DrawFullWidthButton("Create New Scene");

// Compact button (minimal padding, toolbar use)
ButtonDrawer.DrawCompactButton("×");

// Small utility button (×, +, -, icons)
ButtonDrawer.DrawSmallButton("×", tooltip: "Remove");

// Colored semantic button
ButtonDrawer.DrawColoredButton("Delete", MessageType.Error);
ButtonDrawer.DrawColoredButton("Save", MessageType.Success);

// Icon button with selection state
ButtonDrawer.DrawIconButton("play_btn", playIconTexture, size, isSelected: isPlaying);

// Toggle button (changes label based on state)
ButtonDrawer.DrawToggleButton("Loop ON", "Loop OFF", ref isLooping);

// Modal button pair (OK/Cancel)
ButtonDrawer.DrawModalButtonPair("Create", "Cancel",
    onOk: () => CreateProject(),
    onCancel: () => CloseModal());

// Toolbar button group (automatic spacing)
ButtonDrawer.DrawToolbarButtonGroup(
    ("Play", () => Play()),
    ("Pause", () => Pause()),
    ("Stop", () => Stop())
);
```

**Example Usage**:
```csharp
// ❌ WRONG - Don't use raw ImGui buttons
if (ImGui.Button("Save", new Vector2(120, 30)))
{
    Save();
}

// ✅ CORRECT - Use ButtonDrawer for consistency
if (ButtonDrawer.DrawButton("Save", onClick: Save))
{
    // Button was clicked
}
```

#### ModalDrawer
Handles modal dialogs, popups, and confirmation prompts.

**Available Methods**:
```csharp
// Simple confirmation modal
ModalDrawer.RenderConfirmationModal(
    title: "Delete Scene",
    showModal: ref _showDeleteConfirm,
    message: "Are you sure you want to delete this scene? This cannot be undone.",
    onOk: () => DeleteScene());

// Custom modal with content
ModalDrawer.BeginModal("Create Project", ref _showCreateProjectModal);
// ... render modal content
ModalDrawer.EndModal();

// Centered popup
ModalDrawer.BeginCenteredPopup("Settings", ImGuiWindowFlags.AlwaysAutoResize);
// ... render popup content
ModalDrawer.EndPopup();
```

**Example Usage**:
```csharp
public class MyPanel
{
    private bool _showDeleteConfirm;

    public void OnImGuiRender()
    {
        if (ButtonDrawer.DrawButton("Delete"))
        {
            _showDeleteConfirm = true;
        }

        // Render confirmation modal
        ModalDrawer.RenderConfirmationModal(
            title: "Confirm Deletion",
            showModal: ref _showDeleteConfirm,
            message: "Delete this item?",
            onOk: () => DeleteItem());
    }
}
```

#### TableDrawer
Renders tables with consistent styling and behavior.

**Available Methods**:
```csharp
// Begin table with column headers
TableDrawer.BeginTable("MyTable", new[] { "Name", "Type", "Size" });

// Draw rows
foreach (var item in items)
{
    TableDrawer.DrawRow(item.Name, item.Type, item.Size.ToString());
}

// End table
TableDrawer.EndTable();

// Sortable table with callbacks
TableDrawer.BeginSortableTable("Items", columns, onSort: HandleSort);
// ... draw rows
TableDrawer.EndTable();
```

#### TreeDrawer
Renders tree structures with expand/collapse behavior.

**Available Methods**:
```csharp
// Simple tree node
if (TreeDrawer.BeginTreeNode("Folder Name"))
{
    // Render child items
    TreeDrawer.EndTreeNode();
}

// Tree node with icon
if (TreeDrawer.BeginTreeNodeWithIcon("Entity", folderIcon))
{
    // Render children
    TreeDrawer.EndTreeNode();
}

// Leaf node (no children, bullet point)
TreeDrawer.DrawLeafNode("Item.txt");
```

#### LayoutDrawer
Layout utilities for spacing, separators, tooltips, and alignment.

**Available Methods**:
```csharp
// Standard spacing (uses EditorUIConstants.StandardPadding)
LayoutDrawer.DrawSpacing();

// Custom spacing
LayoutDrawer.DrawSpacing(pixels: 10);

// Horizontal separator
LayoutDrawer.DrawSeparator();

// Tooltip (shows on hover)
LayoutDrawer.DrawTooltip("This is a helpful tooltip");

// Center text horizontally
LayoutDrawer.DrawCenteredText("Centered Text");

// Right-align next item
LayoutDrawer.AlignRight(itemWidth: 100);

// Add indent
LayoutDrawer.Indent(amount: 20);
LayoutDrawer.Unindent(amount: 20);
```

#### TextDrawer
Text rendering with semantic color coding.

**Available Methods**:
```csharp
// Colored text based on message type
TextDrawer.DrawText("Success!", MessageType.Success);
TextDrawer.DrawText("Error occurred", MessageType.Error);
TextDrawer.DrawText("Warning!", MessageType.Warning);
TextDrawer.DrawText("Info", MessageType.Info);

// Wrapped text (auto line-break)
TextDrawer.DrawWrappedText("Long text that will wrap to multiple lines...");

// Monospace text (for code/paths)
TextDrawer.DrawMonospaceText("/path/to/file.txt");
```

#### DragDropDrawer
Handles drag-and-drop visualization and logic.

**Available Methods**:
```csharp
// File drop target with validation
DragDropDrawer.HandleFileDropTarget(
    payloadType: DragDropDrawer.ContentBrowserItemPayload,
    isValid: path => Path.GetExtension(path) == ".png",
    onDrop: path => LoadTexture(path));

// Drag source (for content browser items)
DragDropDrawer.SetDragSource("ContentItem", itemPath);

// Check if drag is in progress
bool isDragging = DragDropDrawer.IsDragging();
```

### 2. UI Elements (`Editor/UI/Elements/`)

**Purpose**: Complex, stateful UI components for specific interactions.

#### Drag-Drop Targets
Specialized drop targets for different asset types.

**Available Elements**:
- `TextureDropTarget`: For texture files (.png, .jpg)
- `AudioDropTarget`: For audio files (.wav, .ogg)
- `MeshDropTarget`: For mesh files (.obj, .fbx)
- `ModelTextureDropTarget`: For model textures
- `PrefabDropTarget`: For prefab assets

**Usage Pattern**:
```csharp
// ❌ WRONG - Implementing custom drag-drop logic
ImGui.Button("Texture");
if (ImGui.BeginDragDropTarget())
{
    // ... complex drag-drop logic
}

// ✅ CORRECT - Use specialized drop target
TextureDropTarget.Draw("Texture",
    onTextureChanged: texture => component.Texture = texture,
    assetsManager: _assetsManager);

// ✅ Audio drop target
AudioDropTarget.Draw("Audio Clip",
    onAudioChanged: clip => component.AudioClip = clip,
    assetsManager: _assetsManager);

// ✅ Mesh drop target
MeshDropTarget.Draw("Mesh",
    onMeshChanged: mesh => component.Mesh = mesh,
    assetsManager: _assetsManager);
```

#### ComponentSelector
Popup for adding components to entities.

**Usage**:
```csharp
public class PropertiesPanel
{
    private readonly ComponentSelector _componentSelector = new();

    public void OnImGuiRender()
    {
        if (ButtonDrawer.DrawButton("Add Component"))
        {
            _componentSelector.Show(selectedEntity);
        }

        // Must call every frame
        _componentSelector.Draw();
    }
}
```

#### EntityContextMenu
Right-click context menu for entity operations.

**Usage**:
```csharp
private readonly EntityContextMenu _contextMenu = new();

public void OnImGuiRender()
{
    // In entity list rendering
    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
    {
        _contextMenu.Show(entity, scene);
    }

    // Must call every frame
    _contextMenu.Draw();
}
```

#### PrefabManager
Prefab creation and instantiation UI.

**Usage**:
```csharp
private readonly IPrefabManager _prefabManager;

public MyPanel(IPrefabManager prefabManager)
{
    _prefabManager = prefabManager;
}

public void OnImGuiRender()
{
    if (ButtonDrawer.DrawButton("Create Prefab"))
    {
        _prefabManager.CreatePrefabFromEntity(selectedEntity);
    }
}
```

### 3. Field Editors (`Editor/UI/FieldEditors/`)

**Purpose**: Generic type-safe editors for primitive types, used in component editors.

**Available Field Editors**:
- `IFieldEditor<bool>` - BoolFieldEditor (checkbox)
- `IFieldEditor<int>` - IntFieldEditor (drag int)
- `IFieldEditor<float>` - FloatFieldEditor (drag float)
- `IFieldEditor<double>` - DoubleFieldEditor (drag double)
- `IFieldEditor<string>` - StringFieldEditor (text input)
- `IFieldEditor<Vector2>` - Vector2FieldEditor (X/Y)
- `IFieldEditor<Vector3>` - Vector3FieldEditor (X/Y/Z with axis colors)
- `IFieldEditor<Vector4>` - Vector4FieldEditor (X/Y/Z/W)

**Dependency Injection Pattern**:
```csharp
// ❌ WRONG - Creating editors inline
public class MyComponentEditor : IComponentEditor<MyComponent>
{
    public void DrawEditor(MyComponent component)
    {
        ImGui.DragFloat("Speed", ref component.Speed);
        ImGui.Checkbox("Enabled", ref component.IsEnabled);
    }
}

// ✅ CORRECT - Inject field editors
public class MyComponentEditor : IComponentEditor<MyComponent>
{
    private readonly IFieldEditor<float> _floatEditor;
    private readonly IFieldEditor<bool> _boolEditor;
    private readonly IFieldEditor<Vector3> _vectorEditor;

    public MyComponentEditor(
        IFieldEditor<float> floatEditor,
        IFieldEditor<bool> boolEditor,
        IFieldEditor<Vector3> vectorEditor)
    {
        _floatEditor = floatEditor;
        _boolEditor = boolEditor;
        _vectorEditor = vectorEditor;
    }

    public void DrawEditor(MyComponent component)
    {
        _floatEditor.DrawField("Speed", ref component.Speed);
        _boolEditor.DrawField("Enabled", ref component.IsEnabled);
        _vectorEditor.DrawField("Offset", ref component.Offset);
    }
}
```

**Field Editor Features**:
- Automatic label rendering with PropertyLabelRatio
- Consistent spacing using EditorUIConstants
- Drag behavior for numeric types
- Axis color coding for vectors (X=red, Y=green, Z=blue)
- Reset buttons for vectors (right-click)

### 4. EditorUIConstants (`Editor/UI/Constants/`)

**Purpose**: Centralized constants for consistent styling across all UI.

**Available Constants**:

**Button Sizes**:
```csharp
EditorUIConstants.StandardButtonWidth;     // 120
EditorUIConstants.StandardButtonHeight;    // 30
EditorUIConstants.WideButtonWidth;         // 200
EditorUIConstants.MediumButtonWidth;       // 100
EditorUIConstants.SmallButtonSize;         // 20
```

**Layout Ratios**:
```csharp
EditorUIConstants.PropertyLabelRatio;      // 0.33f (33% for label)
EditorUIConstants.PropertyInputRatio;      // 0.67f (67% for input)

// Usage in component editors
float totalWidth = ImGui.GetContentRegionAvail().X;
float labelWidth = totalWidth * EditorUIConstants.PropertyLabelRatio;
ImGui.SetNextItemWidth(labelWidth);
```

**Column Widths**:
```csharp
EditorUIConstants.DefaultColumnWidth;      // 150
EditorUIConstants.WideColumnWidth;         // 300
EditorUIConstants.FilterInputWidth;        // 200
```

**Spacing**:
```csharp
EditorUIConstants.StandardPadding;         // 8
EditorUIConstants.LargePadding;            // 16
EditorUIConstants.SmallPadding;            // 4
```

**Input Buffers**:
```csharp
EditorUIConstants.MaxTextInputLength;      // 256
EditorUIConstants.MaxNameLength;           // 128
EditorUIConstants.MaxPathLength;           // 512

// Usage for ImGui text buffers
private readonly byte[] _nameBuffer = new byte[EditorUIConstants.MaxNameLength];
```

**Colors**:
```csharp
EditorUIConstants.ErrorColor;              // Red (1, 0, 0, 1)
EditorUIConstants.WarningColor;            // Yellow (1, 1, 0, 1)
EditorUIConstants.SuccessColor;            // Green (0, 1, 0, 1)
EditorUIConstants.InfoColor;               // Blue (0, 0.5, 1, 1)

// Axis colors for vector editors
EditorUIConstants.AxisXColor;              // Red (0.8, 0.2, 0.2, 1)
EditorUIConstants.AxisYColor;              // Green (0.2, 0.8, 0.2, 1)
EditorUIConstants.AxisZColor;              // Blue (0.2, 0.2, 0.8, 1)
```

**UI Sizes**:
```csharp
EditorUIConstants.SelectorListBoxWidth;    // 300
EditorUIConstants.MaxVisibleListItems;     // 10
```

**Usage Rules**:
- ❌ NEVER hardcode magic numbers: `new Vector2(120, 30)`
- ✅ ALWAYS use constants: `new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)`
- ❌ NEVER hardcode colors: `new Vector4(1, 0, 0, 1)`
- ✅ ALWAYS use constants: `EditorUIConstants.ErrorColor`

## Best Practices

### 1. Always Use Drawers Over Raw ImGui
```csharp
// ❌ WRONG
if (ImGui.Button("Save", new Vector2(120, 30)))
    Save();

// ✅ CORRECT
if (ButtonDrawer.DrawButton("Save", onClick: Save))
{
    // Additional logic if needed
}
```

### 2. Use Drag-Drop Targets for Asset References
```csharp
// ❌ WRONG - Custom drag-drop implementation
ImGui.Text("Texture:");
ImGui.SameLine();
ImGui.Button(texturePath ?? "None");
if (ImGui.BeginDragDropTarget())
{
    // Complex payload handling...
}

// ✅ CORRECT - Use specialized drop target
TextureDropTarget.Draw("Texture",
    onTextureChanged: tex => component.Texture = tex,
    assetsManager: _assetsManager);
```

### 3. Inject Field Editors in Component Editors
```csharp
// ❌ WRONG - Direct ImGui calls
public void DrawEditor(TransformComponent transform)
{
    ImGui.DragFloat3("Position", ref transform.Position);
    ImGui.DragFloat3("Rotation", ref transform.Rotation);
}

// ✅ CORRECT - Inject and use field editors
private readonly IFieldEditor<Vector3> _vectorEditor;

public TransformComponentEditor(IFieldEditor<Vector3> vectorEditor)
{
    _vectorEditor = vectorEditor;
}

public void DrawEditor(TransformComponent transform)
{
    _vectorEditor.DrawField("Position", ref transform.Position);
    _vectorEditor.DrawField("Rotation", ref transform.Rotation);
}
```

### 4. Use EditorUIConstants for All Sizing/Spacing
```csharp
// ❌ WRONG - Magic numbers
ImGui.Button("Export", new Vector2(150, 35));
ImGui.Dummy(new Vector2(0, 10));
ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));

// ✅ CORRECT - Use constants
ButtonDrawer.DrawButton("Export",
    width: EditorUIConstants.WideButtonWidth,
    height: EditorUIConstants.StandardButtonHeight);
LayoutDrawer.DrawSpacing();
ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
    new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
```

### 5. Use ModalDrawer for Confirmation Dialogs
```csharp
// ❌ WRONG - Custom modal implementation
if (_showModal)
{
    ImGui.OpenPopup("Confirm");
}
if (ImGui.BeginPopupModal("Confirm"))
{
    ImGui.Text("Are you sure?");
    if (ImGui.Button("OK")) { /* ... */ }
    if (ImGui.Button("Cancel")) { /* ... */ }
    ImGui.EndPopup();
}

// ✅ CORRECT - Use ModalDrawer
ModalDrawer.RenderConfirmationModal(
    title: "Confirm Action",
    showModal: ref _showModal,
    message: "Are you sure you want to proceed?",
    onOk: () => PerformAction());
```

### 6. Use TableDrawer for Structured Data
```csharp
// ❌ WRONG - Manual table creation
if (ImGui.BeginTable("Items", 3))
{
    ImGui.TableSetupColumn("Name");
    ImGui.TableSetupColumn("Type");
    ImGui.TableSetupColumn("Size");
    ImGui.TableHeadersRow();
    // ... rows
    ImGui.EndTable();
}

// ✅ CORRECT - Use TableDrawer
TableDrawer.BeginTable("Items", new[] { "Name", "Type", "Size" });
foreach (var item in items)
{
    TableDrawer.DrawRow(item.Name, item.Type, item.Size.ToString());
}
TableDrawer.EndTable();
```

## Common Anti-Patterns

### Anti-Pattern 1: Hardcoded Sizes
```csharp
// ❌ WRONG
ImGui.Button("Save", new Vector2(120, 30));
ImGui.SetNextItemWidth(200);

// ✅ CORRECT
ButtonDrawer.DrawButton("Save");
ImGui.SetNextItemWidth(EditorUIConstants.DefaultColumnWidth);
```

### Anti-Pattern 2: Custom Drag-Drop Logic
```csharp
// ❌ WRONG
if (ImGui.BeginDragDropTarget())
{
    var payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
    if (payload.NativePtr != null)
    {
        // Complex validation and handling...
    }
    ImGui.EndDragDropTarget();
}

// ✅ CORRECT
TextureDropTarget.Draw("Texture", onTextureChanged, assetsManager);
```

### Anti-Pattern 3: Inline Field Editors
```csharp
// ❌ WRONG
ImGui.DragFloat("Speed", ref speed);
ImGui.Checkbox("Enabled", ref enabled);

// ✅ CORRECT
_floatEditor.DrawField("Speed", ref speed);
_boolEditor.DrawField("Enabled", ref enabled);
```

### Anti-Pattern 4: Creating New UI Patterns
```csharp
// ❌ WRONG - Implementing custom button style
ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1));
ImGui.Button("Success");
ImGui.PopStyleColor();

// ✅ CORRECT - Use existing pattern
ButtonDrawer.DrawColoredButton("Success", MessageType.Success);
```

## Integration Checklist

When implementing editor UI, ensure:

- [ ] All buttons use ButtonDrawer (not raw ImGui.Button)
- [ ] All asset references use drag-drop targets (TextureDropTarget, etc.)
- [ ] All component editors inject field editors for primitive types
- [ ] All sizes/spacing use EditorUIConstants (no magic numbers)
- [ ] All colors use EditorUIConstants (ErrorColor, SuccessColor, etc.)
- [ ] All modals use ModalDrawer.RenderConfirmationModal()
- [ ] All tables use TableDrawer.BeginTable()
- [ ] All trees use TreeDrawer
- [ ] All tooltips use LayoutDrawer.DrawTooltip()
- [ ] All spacing uses LayoutDrawer.DrawSpacing()

## Reference Documentation

**Related Files**:
- `Editor/UI/Drawers/` - All drawer implementations
- `Editor/UI/Elements/` - All element implementations
- `Editor/UI/FieldEditors/` - All field editor implementations
- `Editor/UI/Constants/EditorUIConstants.cs` - Constant definitions
- `CLAUDE.md` sections:
  - "Editor UI Constants" (lines 546-627)
  - "Editor UI Infrastructure" (lines 628-750)
  - "Creating a Component Editor" (lines 883-948)

**Related Skills**:
- `editor-panel-creation`: For creating new panels
- `component-workflow`: For creating component editors
- `architecture-consistency`: For Editor project organization

## Output Format

When reviewing code or suggesting improvements, use this format:

### Example Output
```text
**Issue**: Using raw ImGui buttons instead of ButtonDrawer

**Location**: `Editor/Panels/MyPanel.cs:45-48`

**Problem**:
```csharp
if (ImGui.Button("Save", new Vector2(120, 30)))
{
    Save();
}
```

**Fix**:
```csharp
if (ButtonDrawer.DrawButton("Save", onClick: Save))
{
    // Button clicked
}
```

**Rationale**: ButtonDrawer ensures consistent sizing using EditorUIConstants and provides better maintainability. All editor UI should use Drawers for consistency.

---

**Issue**: Hardcoded sizes instead of EditorUIConstants

**Location**: `Editor/ComponentEditors/MyComponentEditor.cs:67`

**Problem**:
```csharp
ImGui.SetNextItemWidth(200);
```

**Fix**:
```csharp
ImGui.SetNextItemWidth(EditorUIConstants.DefaultColumnWidth);
```

**Rationale**: EditorUIConstants ensures consistent sizing across all editor UI and allows global style changes.
```

## Summary

The Editor UI infrastructure provides:
1. **Drawers**: 7 static utility classes for common patterns
2. **Elements**: 9 stateful components for complex interactions
3. **FieldEditors**: 8 generic type editors for component properties
4. **Constants**: 30+ constants for consistent styling

**Golden Rule**: Never reimplement existing UI patterns. Always check if a Drawer, Element, or FieldEditor exists before writing custom ImGui code.

By using the UI infrastructure consistently, we ensure:
- Visual consistency across all editor panels
- Reduced code duplication
- Easier maintenance and global style changes
- Better user experience through familiar patterns
