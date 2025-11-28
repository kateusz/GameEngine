using System.Numerics;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.UI.Drawers;

/// <summary>
/// Utility class for common ImGui layout patterns used throughout the editor.
/// </summary>
public static class LayoutDrawer
{
    /// <summary>
    /// Renders a search input field with an optional clear button.
    /// </summary>
    /// <param name="hint">Placeholder hint text</param>
    /// <param name="searchQuery">Current search query (will be modified)</param>
    /// <param name="onQueryChanged">Optional callback when query changes</param>
    /// <returns>True if the query was changed</returns>
    public static bool DrawSearchInput(string hint, ref string searchQuery, Action<string>? onQueryChanged = null)
    {
        var contentWidth = ImGui.GetContentRegionAvail().X;
        var inputWidth = contentWidth;

        // Calculate width for clear button if search query is not empty
        if (!string.IsNullOrEmpty(searchQuery)) 
            inputWidth = contentWidth - EditorUIConstants.SmallButtonSize - EditorUIConstants.SmallPadding;

        ImGui.SetNextItemWidth(inputWidth);

        var changed = ImGui.InputTextWithHint("##searchInput", hint, ref searchQuery,
            EditorUIConstants.MaxNameLength);

        if (changed) 
            onQueryChanged?.Invoke(searchQuery);

        // Clear button
        if (string.IsNullOrEmpty(searchQuery))
            return changed;
        
        ImGui.SameLine();
        if (ImGui.Button("×", new Vector2(EditorUIConstants.SmallButtonSize, EditorUIConstants.SmallButtonSize)))
        {
            searchQuery = string.Empty;
            onQueryChanged?.Invoke(searchQuery);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Renders a filter input field with an icon and optional clear button.
    /// </summary>
    /// <param name="label">Label for the filter input</param>
    /// <param name="filterText">Current filter text (will be modified)</param>
    /// <param name="width">Width of the input field (default: FilterInputWidth)</param>
    /// <returns>True if the filter was changed</returns>
    public static bool DrawFilterInput(string label, ref string filterText, float width = EditorUIConstants.FilterInputWidth)
    {
        ImGui.SetNextItemWidth(width);
        return ImGui.InputText(label, ref filterText, EditorUIConstants.MaxTextInputLength);
    }

    /// <summary>
    /// Draws a horizontal separator with optional spacing.
    /// </summary>
    /// <param name="addSpacingBefore">Add spacing before the separator</param>
    /// <param name="addSpacingAfter">Add spacing after the separator</param>
    public static void DrawSeparatorWithSpacing(bool addSpacingBefore = true, bool addSpacingAfter = true)
    {
        if (addSpacingBefore)
            ImGui.Spacing();

        ImGui.Separator();

        if (addSpacingAfter)
            ImGui.Spacing();
    }

    /// <summary>
    /// Renders a checkbox with a specific color for the text.
    /// Commonly used for log level filters.
    /// </summary>
    /// <param name="label">Checkbox label</param>
    /// <param name="value">Checkbox value (will be modified)</param>
    /// <param name="color">Color for the checkbox text</param>
    /// <returns>True if the checkbox was toggled</returns>
    public static bool DrawColoredCheckbox(string label, ref bool value, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        var changed = ImGui.Checkbox(label, ref value);
        ImGui.PopStyleColor();
        return changed;
    }

    /// <summary>
    /// Creates an indented section for grouping related UI elements.
    /// </summary>
    /// <param name="renderContent">Action to render the indented content</param>
    public static void DrawIndentedSection(Action renderContent)
    {
        ImGui.Indent();
        renderContent();
        ImGui.Unindent();
    }

    /// <summary>
    /// Draws a combo box with standard width and styling.
    /// </summary>
    /// <param name="label">Combo label</param>
    /// <param name="currentItem">Currently selected item</param>
    /// <param name="items">Array of selectable items</param>
    /// <param name="onSelected">Callback when an item is selected</param>
    /// <param name="width">Width of the combo box (default: WideColumnWidth)</param>
    public static void DrawComboBox(string label, string currentItem, string[] items, Action<string> onSelected,
        float width = EditorUIConstants.WideColumnWidth)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo(label, currentItem))
        {
            foreach (var item in items)
            {
                var isSelected = item == currentItem;
                if (ImGui.Selectable(item, isSelected))
                {
                    onSelected(item);
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
    }

    /// <summary>
    /// Draws a context menu for an item with common operations.
    /// </summary>
    /// <param name="itemId">Unique identifier for the context menu</param>
    /// <param name="menuItems">Array of menu item configurations (label, callback)</param>
    /// <returns>True if a menu item was clicked</returns>
    public static bool DrawContextMenu(string itemId, params (string Label, Action OnClick)[] menuItems)
    {
        var itemClicked = false;

        if (ImGui.BeginPopupContextItem(itemId))
        {
            foreach (var (label, onClick) in menuItems)
            {
                if (ImGui.MenuItem(label))
                {
                    onClick?.Invoke();
                    itemClicked = true;
                }
            }

            ImGui.EndPopup();
        }

        return itemClicked;
    }

    /// <summary>
    /// Draws a tooltip when the last item is hovered.
    /// </summary>
    /// <param name="text">Tooltip text</param>
    public static void DrawTooltip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(text);
            ImGui.EndTooltip();
        }
    }
}
