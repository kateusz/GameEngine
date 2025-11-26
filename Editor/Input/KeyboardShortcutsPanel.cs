using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.Input;

/// <summary>
/// Panel that displays all available keyboard shortcuts grouped by category.
/// </summary>
public class KeyboardShortcutsPanel
{
    private readonly ShortcutManager _shortcutManager;
    private bool _isVisible;
    private string _filterText = string.Empty;

    public KeyboardShortcutsPanel(ShortcutManager shortcutManager)
    {
        _shortcutManager = shortcutManager;
    }

    /// <summary>
    /// Shows the shortcuts panel.
    /// </summary>
    public void Show()
    {
        _isVisible = true;
    }

    /// <summary>
    /// Hides the shortcuts panel.
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
    }

    /// <summary>
    /// Toggles the visibility of the shortcuts panel.
    /// </summary>
    public void Toggle()
    {
        _isVisible = !_isVisible;
    }

    /// <summary>
    /// Whether the panel is currently visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    /// <summary>
    /// Renders the shortcuts panel UI.
    /// </summary>
    public void Draw()
    {
        if (!_isVisible)
            return;

        ImGui.SetNextWindowSize(new Vector2(600, 500), ImGuiCond.FirstUseEver);

        var windowActive = ImGui.Begin("Keyboard Shortcuts", ref _isVisible, ImGuiWindowFlags.None);
        if (windowActive)
        {
            DrawHeader();
            ImGui.Separator();

            DrawShortcutsTable();
        }
        ImGui.End();
    }

    private void DrawHeader()
    {
        ImGui.Text("All available keyboard shortcuts:");
        ImGui.Spacing();

        // Filter input
        LayoutDrawer.DrawFilterInput("Filter", ref _filterText);
        LayoutDrawer.DrawTooltip("Filter shortcuts by description or key combination");

        ImGui.SameLine();
        ButtonDrawer.DrawButton("Clear Filter", () => _filterText = string.Empty);
    }

    private void DrawShortcutsTable()
    {
        var categories = _shortcutManager.GetCategories();

        if (categories.Count == 0)
        {
            TextDrawer.DrawInfoText("No shortcuts registered.");
            return;
        }

        var childActive = ImGui.BeginChild("ShortcutsScrollRegion", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.None);
        if (childActive)
        {
            foreach (var category in categories)
            {
                var shortcuts = _shortcutManager.GetShortcutsByCategory(category);

                // Filter shortcuts if filter is active
                if (!string.IsNullOrWhiteSpace(_filterText))
                {
                    shortcuts = shortcuts.Where(s =>
                        s.Description.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                        s.GetDisplayString().Contains(_filterText, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Skip empty categories after filtering
                if (shortcuts.Count == 0)
                    continue;

                // Category header
                if (ImGui.CollapsingHeader(category, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawCategoryShortcuts(shortcuts);
                    ImGui.Spacing();
                }
            }
        }
        ImGui.EndChild();
    }

    private void DrawCategoryShortcuts(List<KeyboardShortcut> shortcuts)
    {
        TableDrawer.DrawTwoColumnTable(
            "ShortcutsTable",
            "Shortcut",
            "Description",
            150,
            () =>
            {
                foreach (var shortcut in shortcuts.OrderBy(s => s.GetDisplayString()))
                {
                    TableDrawer.DrawTableRow(
                        () => TableDrawer.DrawColoredCell(shortcut.GetDisplayString(), EditorUIConstants.SuccessColor),
                        () => ImGui.Text(shortcut.Description)
                    );
                }
            }
        );
    }
}
