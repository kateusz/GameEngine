# UI Drawers API Reference

Complete API documentation for all Editor UI Drawers. Drawers are static utility classes providing common UI patterns with consistent styling.

## Table of Contents
1. [ButtonDrawer](#buttondrawer)
2. [ModalDrawer](#modaldrawer)
3. [TableDrawer](#tabledrawer)
4. [TreeDrawer](#treedrawer)
5. [LayoutDrawer](#layoutdrawer)
6. [TextDrawer](#textdrawer)
7. [DragDropDrawer](#dragdropdrawer)

---

## ButtonDrawer

Provides 15+ button variants with consistent sizing and behavior using EditorUIConstants.

### Core Methods

#### DrawButton
Standard button with EditorUIConstants sizing.

```csharp
// Default size (StandardButtonWidth × StandardButtonHeight)
bool clicked = ButtonDrawer.DrawButton("Save");

// Custom size
bool clicked = ButtonDrawer.DrawButton("Export", width: 150, height: 30);

// With onClick callback
ButtonDrawer.DrawButton("Save", onClick: () => SaveFile());
```

#### DrawFullWidthButton
Button that spans full available width.

```csharp
bool clicked = ButtonDrawer.DrawFullWidthButton("Create New Scene");
```

**Use for**: List items, full-width actions in panels.

#### DrawCompactButton
Minimal padding button for toolbars.

```csharp
bool clicked = ButtonDrawer.DrawCompactButton("×");
```

**Use for**: Close buttons, toolbar icons.

#### DrawSmallButton
Small utility button (20×20px) for inline actions.

```csharp
bool clicked = ButtonDrawer.DrawSmallButton("×", tooltip: "Remove");
bool clicked = ButtonDrawer.DrawSmallButton("+", tooltip: "Add");
```

**Use for**: Remove buttons in lists, inline add/remove actions.

#### DrawColoredButton
Semantic colored button using EditorUIConstants colors.

```csharp
// Error (red)
ButtonDrawer.DrawColoredButton("Delete", MessageType.Error);

// Success (green)
ButtonDrawer.DrawColoredButton("Save", MessageType.Success);

// Warning (yellow)
ButtonDrawer.DrawColoredButton("Overwrite", MessageType.Warning);

// Info (blue)
ButtonDrawer.DrawColoredButton("Help", MessageType.Info);
```

**Use for**: Destructive actions (Error), confirmations (Success), cautions (Warning).

#### DrawIconButton
Button with texture icon and optional selection state.

```csharp
bool clicked = ButtonDrawer.DrawIconButton(
    id: "play_btn",
    iconTexture: playIconTexture,
    size: new Vector2(32, 32),
    isSelected: isPlaying
);
```

**Use for**: Toolbar buttons, mode toggles (Play/Pause/Stop).

#### DrawToggleButton
Button that changes label based on boolean state.

```csharp
bool isLooping = true;
ButtonDrawer.DrawToggleButton("Loop ON", "Loop OFF", ref isLooping);
// Shows "Loop ON" when isLooping=true, "Loop OFF" when false
```

**Use for**: Boolean toggles where state should be visible in label.

#### DrawModalButtonPair
OK/Cancel button pair for modals with consistent spacing.

```csharp
ButtonDrawer.DrawModalButtonPair(
    okLabel: "Create",
    cancelLabel: "Cancel",
    onOk: () => CreateProject(),
    onCancel: () => CloseModal()
);
```

**Use for**: Confirmation dialogs, modal forms.

#### DrawToolbarButtonGroup
Horizontal button group with automatic spacing.

```csharp
ButtonDrawer.DrawToolbarButtonGroup(
    ("Play", () => Play()),
    ("Pause", () => Pause()),
    ("Stop", () => Stop())
);
```

**Use for**: Toolbar button sequences, grouped actions.

### Usage Guidelines

**Always use ButtonDrawer instead of `ImGui.Button()`**:
- ✅ Consistent sizing via EditorUIConstants
- ✅ Semantic color coding
- ✅ Proper tooltip support
- ✅ Callback-based API reduces boilerplate

**Example Anti-Pattern**:
```csharp
// ❌ WRONG
if (ImGui.Button("Save", new Vector2(120, 30)))
{
    Save();
}

// ✅ CORRECT
if (ButtonDrawer.DrawButton("Save", onClick: Save))
{
    // Additional logic if needed
}
```

---

## ModalDrawer

Handles modal dialogs, popups, and confirmation prompts with consistent behavior.

### Core Methods

#### RenderConfirmationModal
Simple confirmation dialog with OK/Cancel buttons.

```csharp
private bool _showDeleteConfirm;

// In OnImGuiRender()
ModalDrawer.RenderConfirmationModal(
    title: "Delete Scene",
    showModal: ref _showDeleteConfirm,
    message: "Are you sure you want to delete this scene? This cannot be undone.",
    onOk: () => DeleteScene()
);
```

**Parameters**:
- `title`: Modal window title
- `showModal`: ref bool controlling visibility (set to true to open)
- `message`: Confirmation message text
- `onOk`: Callback when OK clicked (optional onCancel callback available)

**Use for**: Delete confirmations, destructive action warnings.

#### BeginModal / EndModal
Custom modal with full control over content.

```csharp
private bool _showCreateProjectModal;

public void OnImGuiRender()
{
    if (ModalDrawer.BeginModal("Create Project", ref _showCreateProjectModal))
    {
        // Render modal content
        ImGui.Text("Project Name:");
        ImGui.InputText("##name", _nameBuffer, (uint)_nameBuffer.Length);

        ButtonDrawer.DrawModalButtonPair(
            "Create", "Cancel",
            onOk: () => CreateProject(),
            onCancel: () => _showCreateProjectModal = false
        );

        ModalDrawer.EndModal();
    }
}
```

**Use for**: Forms, multi-field dialogs, custom modal content.

#### BeginCenteredPopup / EndPopup
Centered popup window (lighter than modal, doesn't block interaction).

```csharp
if (ModalDrawer.BeginCenteredPopup("Settings", ImGuiWindowFlags.AlwaysAutoResize))
{
    // Render popup content
    ImGui.Text("Settings");
    // ...

    ModalDrawer.EndPopup();
}
```

**Use for**: Settings panels, context menus, tooltips.

### Usage Guidelines

**State Management**:
- Use `ref bool` to control modal visibility
- Set to `true` to open modal
- Modal automatically sets to `false` when closed

**Example Pattern**:
```csharp
public class MyPanel
{
    private bool _showModal;

    public void OnImGuiRender()
    {
        if (ButtonDrawer.DrawButton("Delete"))
        {
            _showModal = true;
        }

        ModalDrawer.RenderConfirmationModal(
            "Confirm", ref _showModal,
            "Delete item?",
            onOk: () => Delete()
        );
    }
}
```

---

## TableDrawer

Renders tables with consistent styling and behavior.

### Core Methods

#### BeginTable / EndTable
Basic table with column headers.

```csharp
TableDrawer.BeginTable("MyTable", new[] { "Name", "Type", "Size" });

foreach (var item in items)
{
    TableDrawer.DrawRow(item.Name, item.Type, item.Size.ToString());
}

TableDrawer.EndTable();
```

#### DrawRow
Renders table row with automatic column population.

```csharp
TableDrawer.DrawRow("Entity1", "GameObject", "1024 bytes");
```

**Supports**: Variable argument count matching column count from BeginTable.

#### BeginSortableTable
Table with sortable columns and callbacks.

```csharp
TableDrawer.BeginSortableTable(
    id: "Items",
    columns: new[] { "Name", "Modified", "Size" },
    onSort: (columnIndex, ascending) => SortItems(columnIndex, ascending)
);

// Draw rows...

TableDrawer.EndTable();
```

**Use for**: File browsers, asset lists, sortable data views.

### Usage Guidelines

**Always prefer TableDrawer over raw ImGui tables**:
- ✅ Consistent column sizing
- ✅ Header styling
- ✅ Sortable column support
- ✅ Row hover effects

---

## TreeDrawer

Renders tree structures with expand/collapse behavior.

### Core Methods

#### BeginTreeNode / EndTreeNode
Expandable tree node with children.

```csharp
if (TreeDrawer.BeginTreeNode("Folder Name"))
{
    // Render child items
    TreeDrawer.DrawLeafNode("File1.txt");
    TreeDrawer.DrawLeafNode("File2.txt");

    TreeDrawer.EndTreeNode();
}
```

#### BeginTreeNodeWithIcon
Tree node with custom icon texture.

```csharp
if (TreeDrawer.BeginTreeNodeWithIcon("Entity", folderIcon))
{
    // Render children
    TreeDrawer.EndTreeNode();
}
```

**Use for**: Entity hierarchies, file browsers.

#### DrawLeafNode
Non-expandable leaf node (no children).

```csharp
TreeDrawer.DrawLeafNode("Item.txt");
```

**Renders**: Bullet point instead of expand arrow.

### Usage Guidelines

**Tree Structure Pattern**:
```csharp
void DrawHierarchy(Entity entity)
{
    if (entity.HasChildren)
    {
        if (TreeDrawer.BeginTreeNode(entity.Name))
        {
            foreach (var child in entity.Children)
                DrawHierarchy(child); // Recursive

            TreeDrawer.EndTreeNode();
        }
    }
    else
    {
        TreeDrawer.DrawLeafNode(entity.Name);
    }
}
```

---

## LayoutDrawer

Layout utilities for spacing, separators, tooltips, and alignment.

### Core Methods

#### DrawSpacing
Vertical spacing using EditorUIConstants.

```csharp
// Standard spacing (StandardPadding = 8px)
LayoutDrawer.DrawSpacing();

// Custom spacing
LayoutDrawer.DrawSpacing(pixels: 16);
```

#### DrawSeparator
Horizontal separator line.

```csharp
LayoutDrawer.DrawSeparator();
```

**Use for**: Section dividers, visual grouping.

#### DrawTooltip
Tooltip shown on hover of previous item.

```csharp
ImGui.Text("Hover me");
LayoutDrawer.DrawTooltip("This is a helpful tooltip");
```

**Must call**: Immediately after item you want tooltip for.

#### DrawCenteredText
Horizontally centered text.

```csharp
LayoutDrawer.DrawCenteredText("Centered Text");
```

#### AlignRight
Positions next item right-aligned.

```csharp
LayoutDrawer.AlignRight(itemWidth: 100);
ImGui.Button("Right Button");
```

**Use for**: Right-aligned buttons in headers, corner actions.

#### Indent / Unindent
Add/remove horizontal indent.

```csharp
LayoutDrawer.Indent(amount: 20);
ImGui.Text("Indented content");
LayoutDrawer.Unindent(amount: 20);
```

**Use for**: Hierarchical content, grouped settings.

### Usage Guidelines

**Spacing Consistency**:
- Always use `LayoutDrawer.DrawSpacing()` instead of `ImGui.Dummy()`
- Use EditorUIConstants spacing values (StandardPadding, LargePadding)
- Consistent spacing improves visual rhythm

---

## TextDrawer

Text rendering with semantic color coding.

### Core Methods

#### DrawText
Colored text based on message type.

```csharp
TextDrawer.DrawText("Success!", MessageType.Success);    // Green
TextDrawer.DrawText("Error occurred", MessageType.Error); // Red
TextDrawer.DrawText("Warning!", MessageType.Warning);    // Yellow
TextDrawer.DrawText("Info", MessageType.Info);           // Blue
```

**Use for**: Status messages, validation feedback, console output.

#### DrawWrappedText
Text that wraps to multiple lines automatically.

```csharp
TextDrawer.DrawWrappedText("Long text that will wrap to multiple lines based on available width...");
```

**Use for**: Descriptions, help text, modal messages.

#### DrawMonospaceText
Monospace font text for code/paths.

```csharp
TextDrawer.DrawMonospaceText("/path/to/file.txt");
TextDrawer.DrawMonospaceText("public class MyClass { }");
```

**Use for**: File paths, code snippets, configuration values.

### Usage Guidelines

**Semantic Colors**:
- Error (Red): Validation failures, exceptions
- Warning (Yellow): Non-critical issues, cautions
- Success (Green): Confirmations, completed actions
- Info (Blue): Neutral information, help text

---

## DragDropDrawer

Handles drag-and-drop visualization and logic.

### Core Methods

#### HandleFileDropTarget
File drop target with validation callback.

```csharp
DragDropDrawer.HandleFileDropTarget(
    payloadType: DragDropDrawer.ContentBrowserItemPayload,
    isValid: path => Path.GetExtension(path) == ".png",
    onDrop: path => LoadTexture(path)
);
```

**Parameters**:
- `payloadType`: Expected payload identifier (use constants)
- `isValid`: Validation function (returns true if file acceptable)
- `onDrop`: Callback when valid file dropped

**Use for**: Custom drop targets not covered by specialized Elements.

#### SetDragSource
Mark item as draggable source.

```csharp
// In content browser item rendering
ImGui.Selectable(itemName);
DragDropDrawer.SetDragSource("ContentItem", itemPath);
```

**Use for**: Content browser items, custom draggable elements.

#### IsDragging
Check if drag operation in progress.

```csharp
bool isDragging = DragDropDrawer.IsDragging();

// Change UI during drag
if (isDragging)
{
    ImGui.PushStyleColor(ImGuiCol.Border, highlightColor);
}
```

**Use for**: Visual feedback during drag operations.

### Usage Guidelines

**Prefer Specialized Drop Targets**:
- Use `TextureDropTarget`, `AudioDropTarget`, etc. from UI/Elements/ when possible
- Only use DragDropDrawer directly for custom/uncommon drag-drop scenarios
- Specialized targets handle validation, error messages, and visual feedback automatically

**Payload Type Constants**:
```csharp
DragDropDrawer.ContentBrowserItemPayload = "CONTENT_BROWSER_ITEM";
DragDropDrawer.EntityPayload = "SCENE_ENTITY";
```

---

## Common Patterns

### Modal with Confirmation
```csharp
private bool _showConfirm;

if (ButtonDrawer.DrawButton("Delete"))
    _showConfirm = true;

ModalDrawer.RenderConfirmationModal(
    "Confirm Deletion",
    ref _showConfirm,
    "Delete this item permanently?",
    onOk: () => DeleteItem()
);
```

### Colored Action Buttons
```csharp
if (ButtonDrawer.DrawColoredButton("Save", MessageType.Success))
    Save();

if (ButtonDrawer.DrawColoredButton("Delete", MessageType.Error))
    _showConfirm = true;
```

### Tree with Context Menu
```csharp
if (TreeDrawer.BeginTreeNode("Entity"))
{
    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        _contextMenu.Show(entity);

    // Children...
    TreeDrawer.EndTreeNode();
}
```

### Table with Sortable Columns
```csharp
TableDrawer.BeginSortableTable("Assets",
    new[] { "Name", "Type", "Size" },
    onSort: SortAssets
);

foreach (var asset in _sortedAssets)
    TableDrawer.DrawRow(asset.Name, asset.Type, asset.Size);

TableDrawer.EndTable();
```

---

## See Also

- [elements-api.md](elements-api.md) - Stateful UI components (drop targets, selectors)
- [constants-catalog.md](constants-catalog.md) - EditorUIConstants reference
- [../SKILL.md](../SKILL.md) - Main UI infrastructure guide
