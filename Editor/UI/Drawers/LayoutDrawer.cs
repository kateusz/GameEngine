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
    /// Draws a toolbar section with common controls.
    /// Handles button placement with consistent spacing.
    /// </summary>
    /// <param name="buttons">Array of button configurations (label, callback)</param>
    public static void DrawToolbarButtons(params (string Label, Action OnClick)[] buttons)
    {
        for (var i = 0; i < buttons.Length; i++)
        {
            if (i > 0)
                ImGui.SameLine();

            var (label, onClick) = buttons[i];
            if (ImGui.Button(label))
            {
                onClick.Invoke();
            }
        }
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
    /// Renders a labeled section with a header and indented content.
    /// </summary>
    /// <param name="header">Header text</param>
    /// <param name="renderContent">Action to render the section content</param>
    public static void DrawLabeledSection(string header, Action renderContent)
    {
        ImGui.Text(header);
        DrawIndentedSection(renderContent);
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

    /// <summary>
    /// Draws a disabled text label with standard disabled styling.
    /// </summary>
    /// <param name="text">Text to display</param>
    public static void DrawDisabledText(string text)
    {
        ImGui.TextDisabled(text);
    }

    /// <summary>
    /// Draws a two-column layout with standard property editor proportions.
    /// </summary>
    /// <param name="label">Label text for the left column</param>
    /// <param name="renderValue">Action to render the value in the right column</param>
    public static void DrawPropertyRow(string label, Action renderValue)
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        renderValue();
        ImGui.Columns(1);
    }

    #region Input Field Helpers

    /// <summary>
    /// Draws a labeled text input field with standard property row layout.
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="value">Current value (will be modified)</param>
    /// <param name="maxLength">Maximum input length</param>
    /// <param name="flags">Optional input flags</param>
    /// <returns>True if the value was changed</returns>
    public static bool DrawLabeledInputText(string label, ref string value, uint maxLength,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        var changed = ImGui.InputText($"##{label}", ref value, maxLength, flags);
        ImGui.Columns(1);
        return changed;
    }

    /// <summary>
    /// Draws a labeled integer input field with standard property row layout.
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="value">Current value (will be modified)</param>
    /// <param name="step">Step increment (default: 1)</param>
    /// <param name="stepFast">Fast step increment (default: 10)</param>
    /// <returns>True if the value was changed</returns>
    public static bool DrawLabeledInputInt(string label, ref int value, int step = 1, int stepFast = 10)
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        var changed = ImGui.InputInt($"##{label}", ref value, step, stepFast);
        ImGui.Columns(1);
        return changed;
    }

    /// <summary>
    /// Draws a labeled float input field with standard property row layout.
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="value">Current value (will be modified)</param>
    /// <param name="step">Step increment (default: 0.1f)</param>
    /// <param name="stepFast">Fast step increment (default: 1.0f)</param>
    /// <param name="format">Display format (default: "%.3f")</param>
    /// <returns>True if the value was changed</returns>
    public static bool DrawLabeledInputFloat(string label, ref float value, float step = 0.1f,
        float stepFast = 1.0f, string format = "%.3f")
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        var changed = ImGui.InputFloat($"##{label}", ref value, step, stepFast, format);
        ImGui.Columns(1);
        return changed;
    }

    /// <summary>
    /// Draws an input field with validation and error message display.
    /// Error message appears below the input field in red.
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="value">Current value (will be modified)</param>
    /// <param name="maxLength">Maximum input length</param>
    /// <param name="errorMessage">Error message to display (null/empty for no error)</param>
    /// <param name="flags">Optional input flags</param>
    /// <returns>True if the value was changed</returns>
    public static bool DrawValidatedInputText(string label, ref string value, uint maxLength,
        string? errorMessage, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        var changed = ImGui.InputText(label, ref value, maxLength, flags);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
            ImGui.TextWrapped(errorMessage);
            ImGui.PopStyleColor();
        }

        return changed;
    }

    /// <summary>
    /// Draws a file path input field with a browse button.
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="path">Current path (will be modified)</param>
    /// <param name="maxLength">Maximum path length</param>
    /// <param name="onBrowse">Callback when browse button is clicked</param>
    /// <param name="browseButtonLabel">Label for browse button (default: "...")</param>
    /// <returns>True if the path was changed via input</returns>
    public static bool DrawFilePathInput(string label, ref string path, uint maxLength,
        Action onBrowse, string browseButtonLabel = "...")
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X -
                               EditorUIConstants.StandardButtonWidth -
                               EditorUIConstants.SmallPadding);

        var changed = ImGui.InputText(label, ref path, maxLength);

        ImGui.SameLine();
        if (ImGui.Button(browseButtonLabel,
                new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
        {
            onBrowse();
        }

        return changed;
    }

    /// <summary>
    /// Draws a read-only display field that looks like a disabled input.
    /// Used for showing non-editable information in forms.
    /// </summary>
    /// <param name="label">Label for the field</param>
    /// <param name="value">Value to display</param>
    public static void DrawReadOnlyField(string label, string value)
    {
        DrawPropertyRow(label, () =>
        {
            ImGui.BeginDisabled();
            ImGui.InputText($"##{label}_readonly", ref value, (uint)value.Length,
                ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
        });
    }

    /// <summary>
    /// Draws a multi-line text input field with standard height.
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="value">Current value (will be modified)</param>
    /// <param name="maxLength">Maximum input length</param>
    /// <param name="height">Height of the text area (default: 100)</param>
    /// <param name="flags">Optional input flags</param>
    /// <returns>True if the value was changed</returns>
    public static bool DrawMultilineInput(string label, ref string value, uint maxLength,
        float height = 100f, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        ImGui.Text(label);
        var size = new Vector2(-1, height);
        return ImGui.InputTextMultiline($"##{label}", ref value, maxLength, size, flags);
    }

    /// <summary>
    /// Draws an input field with a unit suffix (e.g., "px", "ms", "%").
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="value">Current value (will be modified)</param>
    /// <param name="unit">Unit suffix to display</param>
    /// <param name="step">Step increment (default: 1)</param>
    /// <param name="stepFast">Fast step increment (default: 10)</param>
    /// <returns>True if the value was changed</returns>
    public static bool DrawInputWithUnit(string label, ref int value, string unit,
        int step = 1, int stepFast = 10)
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X -
                               ImGui.CalcTextSize(unit).X -
                               EditorUIConstants.SmallPadding);
        var changed = ImGui.InputInt($"##{label}", ref value, step, stepFast);
        ImGui.SameLine();
        ImGui.TextDisabled(unit);

        ImGui.Columns(1);
        return changed;
    }

    /// <summary>
    /// Draws a float input field with a unit suffix (e.g., "px", "ms", "%").
    /// </summary>
    /// <param name="label">Label for the input field</param>
    /// <param name="value">Current value (will be modified)</param>
    /// <param name="unit">Unit suffix to display</param>
    /// <param name="step">Step increment (default: 0.1f)</param>
    /// <param name="stepFast">Fast step increment (default: 1.0f)</param>
    /// <param name="format">Display format (default: "%.2f")</param>
    /// <returns>True if the value was changed</returns>
    public static bool DrawInputFloatWithUnit(string label, ref float value, string unit,
        float step = 0.1f, float stepFast = 1.0f, string format = "%.2f")
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X -
                               ImGui.CalcTextSize(unit).X -
                               EditorUIConstants.SmallPadding);
        var changed = ImGui.InputFloat($"##{label}", ref value, step, stepFast, format);
        ImGui.SameLine();
        ImGui.TextDisabled(unit);

        ImGui.Columns(1);
        return changed;
    }

    #endregion
}
