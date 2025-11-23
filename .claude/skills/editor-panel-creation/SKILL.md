---
name: editor-panel-creation
description: Guide creation of new ImGui editor panels following dependency injection patterns, EditorUIConstants usage, panel interface design, and integration with EditorLayer. Use when adding new editor tools, asset browsers, debugging panels, or workflow windows to the game engine editor.
---

# Editor Panel Creation

## Overview
This skill provides comprehensive guidance for creating new ImGui-based editor panels, ensuring consistency with the engine's dependency injection architecture, UI styling standards, and editor integration patterns.

## When to Use
Invoke this skill when:
- Adding a new editor panel or tool window
- Creating asset browsers or managers
- Building debugging or profiling panels
- Implementing workflow tools for artists/designers
- Questions about editor architecture and patterns
- Integrating panels with the editor layer system

## Panel Creation Workflow

### Step 1: Define Panel Interface
**Location**: `Editor/Panels/`

**Pattern**: All panels use interface-based design for testability and DI

**Interface Template**:
```csharp
namespace Editor.Panels;

/// <summary>
/// Interface for the [PanelName] panel.
/// </summary>
public interface IMyNewPanel
{
    /// <summary>
    /// Renders the panel using ImGui.
    /// </summary>
    void OnImGuiRender();

    /// <summary>
    /// Gets or sets whether the panel is currently open.
    /// </summary>
    bool IsOpen { get; set; }
}
```

**Naming Convention**:
- Interface: `I[PanelName]Panel` or `I[PanelName]`
- Implementation: `[PanelName]Panel` or `[PanelName]`
- Examples: `ISceneHierarchyPanel`, `IConsolePanel`, `ITileMapPanel`

### Step 2: Implement Panel Class
**Location**: `Editor/Panels/`

**Guidelines**:
- Use constructor injection for ALL dependencies
- Use `EditorUIConstants` for sizing, spacing, colors
- Maintain panel state in private fields
- Implement proper disposal if managing resources
- Follow ImGui immediate-mode UI patterns

**Panel Template**:
```csharp
namespace Editor.Panels;

using Editor.UI;
using ImGuiNET;
using Editor.Managers;

/// <summary>
/// Panel for managing and displaying [functionality].
/// </summary>
public class MyNewPanel : IMyNewPanel
{
    // Injected dependencies
    private readonly ISceneManager _sceneManager;
    private readonly IProjectManager _projectManager;

    // Panel state
    private bool _isOpen = true;
    private string _filterText = string.Empty;
    private int _selectedIndex = -1;

    // Input buffers (use EditorUIConstants for sizes)
    private readonly byte[] _nameBuffer = new byte[EditorUIConstants.MaxNameLength];

    /// <summary>
    /// Initializes a new instance of the <see cref="MyNewPanel"/> class.
    /// </summary>
    /// <param name="sceneManager">Scene manager service.</param>
    /// <param name="projectManager">Project manager service.</param>
    public MyNewPanel(
        ISceneManager sceneManager,
        IProjectManager projectManager)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
    }

    /// <inheritdoc/>
    public bool IsOpen
    {
        get => _isOpen;
        set => _isOpen = value;
    }

    /// <inheritdoc/>
    public void OnImGuiRender()
    {
        if (!_isOpen)
            return;

        // Use EditorUIConstants for window flags if needed
        ImGuiWindowFlags flags = ImGuiWindowFlags.None;

        if (ImGui.Begin("My Panel", ref _isOpen, flags))
        {
            DrawToolbar();
            ImGui.Separator();
            DrawContent();
        }
        ImGui.End();
    }

    private void DrawToolbar()
    {
        // Filter input
        ImGui.SetNextItemWidth(EditorUIConstants.FilterInputWidth);
        if (ImGui.InputText("##Filter", _nameBuffer, (uint)_nameBuffer.Length))
        {
            _filterText = System.Text.Encoding.UTF8.GetString(_nameBuffer).TrimEnd('\0');
        }

        ImGui.SameLine();

        // Action buttons
        if (ImGui.Button("Action", new Vector2(
            EditorUIConstants.StandardButtonWidth,
            EditorUIConstants.StandardButtonHeight)))
        {
            PerformAction();
        }
    }

    private void DrawContent()
    {
        // Use injected services
        var scene = _sceneManager.GetActiveScene();
        if (scene == null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.WarningColor);
            ImGui.Text("No active scene");
            ImGui.PopStyleColor();
            return;
        }

        // Panel content here
        ImGui.Text($"Scene: {scene.Name}");

        // Example: List with EditorUIConstants
        if (ImGui.BeginListBox("##Items", new Vector2(
            -1,
            EditorUIConstants.MaxVisibleListItems * ImGui.GetTextLineHeightWithSpacing())))
        {
            // List items here
            ImGui.EndListBox();
        }
    }

    private void PerformAction()
    {
        // Use injected dependencies for actions
        var scene = _sceneManager.GetActiveScene();
        // ... action logic
    }
}
```

### Step 3: Register in Dependency Injection
**Location**: `Editor/Program.cs`

**Registration Pattern**:
```csharp
private static void ConfigureServices(Container container)
{
    // ... existing registrations

    // Register new panel
    container.Register<IMyNewPanel, MyNewPanel>(Reuse.Singleton);
}
```

**Guidelines**:
- Always register as singleton (one instance per editor session)
- Register interface → implementation mapping
- Ensure all dependencies are registered before the panel

### Step 4: Inject into EditorLayer
**Location**: `Editor/EditorLayer.cs`

**Constructor Injection**:
```csharp
public class EditorLayer : Layer
{
    // Existing panels
    private readonly ISceneHierarchyPanel _sceneHierarchyPanel;
    private readonly IPropertiesPanel _propertiesPanel;
    private readonly IConsolePanel _consolePanel;

    // New panel
    private readonly IMyNewPanel _myNewPanel;

    public EditorLayer(
        // ... existing parameters
        IMyNewPanel myNewPanel)
    {
        // ... existing initializations
        _myNewPanel = myNewPanel ?? throw new ArgumentNullException(nameof(myNewPanel));
    }

    public override void OnImGuiRender()
    {
        // ... existing panel renders
        _myNewPanel.OnImGuiRender();
    }
}
```

### Step 5: Add Menu Integration
**Location**: `Editor/EditorLayer.cs` (in menu bar rendering)

**Add Panel Toggle Menu**:
```csharp
private void DrawMenuBar()
{
    if (ImGui.BeginMenu("Window"))
    {
        // Existing menu items
        if (ImGui.MenuItem("Scene Hierarchy", "", _sceneHierarchyPanel.IsOpen))
            _sceneHierarchyPanel.IsOpen = !_sceneHierarchyPanel.IsOpen;

        if (ImGui.MenuItem("Properties", "", _propertiesPanel.IsOpen))
            _propertiesPanel.IsOpen = !_propertiesPanel.IsOpen;

        // New panel menu item
        if (ImGui.MenuItem("My Panel", "", _myNewPanel.IsOpen))
            _myNewPanel.IsOpen = !_myNewPanel.IsOpen;

        ImGui.EndMenu();
    }
}
```

**Keyboard Shortcut** (optional):
```csharp
private void HandleShortcuts()
{
    // Existing shortcuts
    // Ctrl+Shift+M to toggle My Panel
    if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) &&
        ImGui.IsKeyDown(ImGuiKey.LeftShift) &&
        ImGui.IsKeyPressed(ImGuiKey.M))
    {
        _myNewPanel.IsOpen = !_myNewPanel.IsOpen;
    }
}
```

### Step 6: Use EditorUIConstants Throughout

**All panels MUST use EditorUIConstants** - never hardcode UI values!

**Common Usage Patterns**:

```csharp
using Editor.UI;

// Button sizes
ImGui.Button("Save", new Vector2(
    EditorUIConstants.StandardButtonWidth,
    EditorUIConstants.StandardButtonHeight));

ImGui.Button("Wide Action", new Vector2(
    EditorUIConstants.WideButtonWidth,
    EditorUIConstants.StandardButtonHeight));

// Property layout (2-column with 33% label, 67% input)
float labelWidth = ImGui.GetContentRegionAvail().X * EditorUIConstants.PropertyLabelRatio;

ImGui.Text("Property Name");
ImGui.SameLine(labelWidth);
ImGui.SetNextItemWidth(-1); // Fill remaining space
ImGui.DragFloat("##Value", ref value);

// Spacing and padding
ImGui.Dummy(new Vector2(0, EditorUIConstants.StandardPadding));
ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
    new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));

// Colors (consistent across editor)
ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
ImGui.Text("Error message");
ImGui.PopStyleColor();

ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.WarningColor);
ImGui.Text("Warning message");
ImGui.PopStyleColor();

// Axis colors for vector editors
ImGui.PushStyleColor(ImGuiCol.Button, EditorUIConstants.AxisXColor); // Red for X
ImGui.PushStyleColor(ImGuiCol.Button, EditorUIConstants.AxisYColor); // Green for Y
ImGui.PushStyleColor(ImGuiCol.Button, EditorUIConstants.AxisZColor); // Blue for Z

// Input buffer sizes
byte[] nameBuffer = new byte[EditorUIConstants.MaxNameLength];
byte[] pathBuffer = new byte[EditorUIConstants.MaxPathLength];
byte[] textBuffer = new byte[EditorUIConstants.MaxTextInputLength];

// List boxes
ImGui.BeginListBox("##List", new Vector2(
    EditorUIConstants.SelectorListBoxWidth,
    EditorUIConstants.MaxVisibleListItems * ImGui.GetTextLineHeightWithSpacing()));

// Filter input width
ImGui.SetNextItemWidth(EditorUIConstants.FilterInputWidth);
ImGui.InputText("##Filter", buffer, bufferSize);

// Column widths
ImGui.SetColumnWidth(0, EditorUIConstants.DefaultColumnWidth);
ImGui.SetColumnWidth(1, EditorUIConstants.WideColumnWidth);
```

## Advanced Panel Patterns

### Dockable Panel
```csharp
public void OnImGuiRender()
{
    if (!_isOpen)
        return;

    ImGuiWindowFlags flags = ImGuiWindowFlags.None;

    if (ImGui.Begin("My Panel", ref _isOpen, flags))
    {
        // Panel is dockable by default in ImGui
        DrawContent();
    }
    ImGui.End();
}
```

### Panel with Tabs
```csharp
private void DrawContent()
{
    if (ImGui.BeginTabBar("##MyTabs"))
    {
        if (ImGui.BeginTabItem("Tab 1"))
        {
            DrawTab1Content();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Tab 2"))
        {
            DrawTab2Content();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}
```

### Panel with Context Menu
```csharp
private void DrawItem(string itemName)
{
    ImGui.Selectable(itemName);

    if (ImGui.BeginPopupContextItem($"##{itemName}Context"))
    {
        if (ImGui.MenuItem("Edit"))
            EditItem(itemName);

        if (ImGui.MenuItem("Delete"))
            DeleteItem(itemName);

        ImGui.EndPopup();
    }
}
```

### Panel with Modal Dialog
```csharp
private bool _showDeleteConfirmation = false;

private void DrawContent()
{
    if (ImGui.Button("Delete"))
        _showDeleteConfirmation = true;

    // Modal dialog
    if (_showDeleteConfirmation)
    {
        ImGui.OpenPopup("Delete Confirmation");
        _showDeleteConfirmation = false;
    }

    if (ImGui.BeginPopupModal("Delete Confirmation"))
    {
        ImGui.Text("Are you sure you want to delete?");

        if (ImGui.Button("Yes", new Vector2(
            EditorUIConstants.StandardButtonWidth,
            EditorUIConstants.StandardButtonHeight)))
        {
            PerformDelete();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("No", new Vector2(
            EditorUIConstants.StandardButtonWidth,
            EditorUIConstants.StandardButtonHeight)))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }
}
```

## Existing Panels Reference

The editor currently has **17 panels**:

1. **SceneHierarchyPanel** - Entity tree view
2. **PropertiesPanel** - Component inspector
3. **ConsolePanel** - Logging output
4. **ContentBrowserPanel** - Asset browser
5. **ViewportPanel** - Scene rendering viewport
6. **GameViewPanel** - Game view
7. **StatsPanel** - Performance statistics
8. **AssetPanel** - Asset management
9. **ProjectSettingsPanel** - Project configuration
10. **BuildSettingsPanel** - Build configuration
11. **PreferencesPanel** - Editor preferences
12. **TileMapPanel** - Tilemap editor
13. **ShortcutsPanel** - Keyboard shortcuts
14. **AboutPanel** - About dialog
15. **SceneSettingsPanel** - Scene configuration
16. **AudioPanel** - Audio system controls
17. **PhysicsPanel** - Physics debugging

**Reference these panels** for implementation patterns and UI consistency.

## Dependency Injection Best Practices

### Common Service Dependencies

```csharp
// Scene management
private readonly ISceneManager _sceneManager;

// Project management
private readonly IProjectManager _projectManager;

// Factories
private readonly ITextureFactory _textureFactory;
private readonly IShaderFactory _shaderFactory;
private readonly IAudioClipFactory _audioClipFactory;

// Systems
private readonly SystemManager _systemManager;

// Other panels (for cross-panel communication)
private readonly ISceneHierarchyPanel _sceneHierarchyPanel;
```

### Constructor Pattern
```csharp
public MyPanel(
    ISceneManager sceneManager,
    IProjectManager projectManager,
    ITextureFactory textureFactory)
{
    _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
    _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
    _textureFactory = textureFactory ?? throw new ArgumentNullException(nameof(textureFactory));
}
```

### Never Create Static Singletons
```csharp
// ❌ WRONG - Do not create static singletons
public static class MyPanelManager
{
    public static MyPanelManager Instance { get; } = new();
}

// ✅ CORRECT - Use DI container registration
container.Register<IMyPanel, MyPanel>(Reuse.Singleton);
```

## Testing Checklist

- [ ] Panel interface defined
- [ ] Panel implementation with constructor injection
- [ ] All dependencies properly injected (no nulls)
- [ ] `EditorUIConstants` used throughout (no magic numbers)
- [ ] Registered in `Program.cs` DI container
- [ ] Injected into `EditorLayer`
- [ ] Menu item added to Window menu
- [ ] Panel opens and closes correctly
- [ ] Panel state persists during session
- [ ] Panel works with docking system
- [ ] Keyboard shortcuts added (if applicable)
- [ ] Panel performs expected functionality
- [ ] Cross-panel communication works (if needed)

## Documentation Requirements

### Code Documentation
- XML comments on interface and public methods
- Clear parameter descriptions
- Usage examples in comments

### Module Documentation
Update `docs/modules/editor.md`:
- Add panel to list of editor panels
- Describe panel purpose and features
- Include screenshot if visual changes are significant
- Document keyboard shortcuts

## Reference Documentation
- **Architecture**: `CLAUDE.md` - Editor architecture and DI patterns
- **Module Docs**: `docs/modules/editor.md` - Editor panel documentation
- **UI Constants**: `Editor/UI/EditorUIConstants.cs` - All UI sizing constants
- **Existing Panels**: `Editor/Panels/` - 17 reference implementations
- **EditorLayer**: `Editor/EditorLayer.cs` - Panel integration point

## Integration with Agents
This skill complements the **game-editor-architect** agent. Use this skill for panel structure and workflow, then delegate to game-editor-architect for ImGui-specific UI implementation and styling details.

## Common Pitfalls to Avoid

1. **Hardcoded UI values** - Always use EditorUIConstants
2. **Static state** - Use instance fields, inject dependencies
3. **Missing null checks** - Validate constructor parameters
4. **Inconsistent styling** - Follow existing panel patterns
5. **Direct service access** - Use dependency injection
6. **Forgetting IsOpen check** - Always check before rendering
7. **ImGui misuse** - Follow Begin/End pairing strictly
8. **Performance issues** - Avoid heavy computation in OnImGuiRender

## Tool Restrictions
None - this skill may create files, edit code, and update documentation for complete panel implementation.
