using System.Numerics;
using Editor.Input;
using Editor.UI;
using ImGuiNET;

namespace Editor.Panels;

/// <summary>
/// Panel that displays all available keyboard shortcuts grouped by category.
/// </summary>
public class KeyboardShortcutsPanel
{
    private readonly ShortcutManager _shortcutManager;
    private bool _isVisible;
    private string _filterText = string.Empty;
    private readonly char[] _filterBuffer = new char[EditorUIConstants.MaxTextInputLength];

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
        ImGui.SetNextItemWidth(EditorUIConstants.FilterInputWidth);
        if (ImGui.InputText("##Filter", _filterBuffer, EditorUIConstants.MaxTextInputLength))
        {
            _filterText = new string(_filterBuffer).TrimEnd('\0');
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Filter shortcuts by description or key combination");
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Filter", new Vector2(EditorUIConstants.StandardButtonWidth, 0)))
        {
            Array.Clear(_filterBuffer, 0, _filterBuffer.Length);
            _filterText = string.Empty;
        }
    }

    private void DrawShortcutsTable()
    {
        var categories = _shortcutManager.GetCategories();

        if (categories.Count == 0)
        {
            ImGui.TextColored(EditorUIConstants.InfoColor, "No shortcuts registered.");
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
        // Create a table for better layout
        if (ImGui.BeginTable($"ShortcutsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            // Setup columns
            ImGui.TableSetupColumn("Shortcut", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            // Draw shortcuts
            foreach (var shortcut in shortcuts.OrderBy(s => s.GetDisplayString()))
            {
                ImGui.TableNextRow();

                // Shortcut key column
                ImGui.TableSetColumnIndex(0);
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.SuccessColor);
                ImGui.Text(shortcut.GetDisplayString());
                ImGui.PopStyleColor();

                // Description column
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(shortcut.Description);
            }

            ImGui.EndTable();
        }
    }
}
