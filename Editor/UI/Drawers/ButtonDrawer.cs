using System.Numerics;
using Editor.UI.Constants;
using Editor.Utilities;
using ImGuiNET;

namespace Editor.UI.Drawers;

/// <summary>
/// Provides standardized button rendering with consistent sizing, coloring, and state management.
/// </summary>
public static class ButtonDrawer
{
    /// <summary>
    /// Draws a standard button with EditorUIConstants sizing.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="disabled">Whether the button should be disabled</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawButton(string label, Action? onClick = null, bool disabled = false)
    {
        if (disabled)
            ImGui.BeginDisabled();

        var clicked = ImGui.Button(label, new Vector2(EditorUIConstants.StandardButtonWidth,
            EditorUIConstants.StandardButtonHeight));

        if (disabled)
            ImGui.EndDisabled();

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a button with custom size.
    /// Use this when standard button dimensions don't fit your layout.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="width">Button width</param>
    /// <param name="height">Button height</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="disabled">Whether the button should be disabled</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawButton(string label, float width, float height,
        Action? onClick = null, bool disabled = false)
    {
        if (disabled)
            ImGui.BeginDisabled();

        var clicked = ImGui.Button(label, new Vector2(width, height));

        if (disabled)
            ImGui.EndDisabled();

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a button that spans the full available width.
    /// Useful for panel actions and list items.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="disabled">Whether the button should be disabled</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawFullWidthButton(string label, Action? onClick = null, bool disabled = false)
    {
        if (disabled)
            ImGui.BeginDisabled();

        var clicked = ImGui.Button(label, new Vector2(-1, EditorUIConstants.StandardButtonHeight));

        if (disabled)
            ImGui.EndDisabled();

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a compact button with minimal padding.
    /// Useful for toolbar buttons and dense UI layouts.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="disabled">Whether the button should be disabled</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawCompactButton(string label, Action? onClick = null, bool disabled = false)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));

        if (disabled)
            ImGui.BeginDisabled();

        var clicked = ImGui.Button(label);

        if (disabled)
            ImGui.EndDisabled();

        ImGui.PopStyleVar();

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a standard modal button with EditorUIConstants sizing.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="width">Button width (default: StandardButtonWidth)</param>
    /// <param name="height">Button height (default: StandardButtonHeight)</param>
    /// <param name="disabled">Whether the button should be disabled</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawModalButton(string label, Action? onClick = null,
        float width = EditorUIConstants.StandardButtonWidth,
        float height = EditorUIConstants.StandardButtonHeight,
        bool disabled = false)
    {
        if (disabled)
            ImGui.BeginDisabled();

        var clicked = ImGui.Button(label, new Vector2(width, height));

        if (disabled)
            ImGui.EndDisabled();

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a colored button with semantic meaning (success, danger, warning).
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="type">Message type determining color scheme</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="width">Button width (default: StandardButtonWidth)</param>
    /// <param name="height">Button height (default: StandardButtonHeight)</param>
    /// <param name="disabled">Whether the button should be disabled</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawColoredButton(string label, MessageType type, Action? onClick = null,
        float width = EditorUIConstants.StandardButtonWidth,
        float height = EditorUIConstants.StandardButtonHeight,
        bool disabled = false)
    {
        var color = type switch
        {
            MessageType.Success => EditorUIConstants.SuccessColor,
            MessageType.Error => EditorUIConstants.ErrorColor,
            MessageType.Warning => EditorUIConstants.WarningColor,
            MessageType.Info => EditorUIConstants.InfoColor,
            _ => Vector4.One
        };

        // Apply darker tint to button color for better visibility
        var buttonColor = color with { W = 0.6f };
        var buttonHoverColor = color with { W = 0.8f };
        var buttonActiveColor = color with { W = 1.0f };

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonHoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonActiveColor);

        if (disabled)
            ImGui.BeginDisabled();

        var clicked = ImGui.Button(label, new Vector2(width, height));

        if (disabled)
            ImGui.EndDisabled();

        ImGui.PopStyleColor(3);

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws an icon button (ImageButton) with optional selection state.
    /// Commonly used for toolbar icons and mode selectors.
    /// </summary>
    /// <param name="id">Unique identifier for the button</param>
    /// <param name="textureId">OpenGL texture ID</param>
    /// <param name="size">Button size</param>
    /// <param name="isSelected">Whether this button is currently selected</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawIconButton(string id, uint textureId, Vector2 size,
        bool isSelected = false, Action? onClick = null, string? tooltip = null)
    {
        if (isSelected)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 0.8f));

        var clicked = ImGui.ImageButton(id, (IntPtr)textureId, size,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector4(0, 0, 0, 0), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        if (isSelected)
            ImGui.PopStyleColor();

        if (!string.IsNullOrEmpty(tooltip))
            LayoutDrawer.DrawTooltip(tooltip);

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a transparent toolbar icon button with hover/active effects.
    /// Used for toolbar buttons with minimal visual weight.
    /// </summary>
    /// <param name="id">Unique identifier for the button</param>
    /// <param name="textureId">OpenGL texture ID</param>
    /// <param name="size">Button size</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawTransparentIconButton(string id, uint textureId, Vector2 size,
        Action? onClick = null, string? tooltip = null)
    {
        var colors = ImGui.GetStyle().Colors;
        var buttonHovered = colors[(int)ImGuiCol.ButtonHovered];
        var buttonActive = colors[(int)ImGuiCol.ButtonActive];

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonHovered with { W = 0.5f });
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonActive with { W = 0.5f });

        var clicked = ImGui.ImageButton(id, (IntPtr)textureId, size,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector4(0, 0, 0, 0), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        ImGui.PopStyleColor(3);

        if (!string.IsNullOrEmpty(tooltip))
            LayoutDrawer.DrawTooltip(tooltip);

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a small utility button (like "×" for clear, delete icons, etc.).
    /// Uses SmallButtonSize from EditorUIConstants.
    /// </summary>
    /// <param name="label">Button label (typically a single character or icon)</param>
    /// <param name="onClick">Callback when button is clicked</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <returns>True if the button was clicked</returns>
    public static bool DrawSmallButton(string label, Action? onClick = null, string? tooltip = null)
    {
        var clicked = ImGui.Button(label,
            new Vector2(EditorUIConstants.SmallButtonSize, EditorUIConstants.SmallButtonSize));

        if (!string.IsNullOrEmpty(tooltip))
            LayoutDrawer.DrawTooltip(tooltip);

        if (clicked && onClick != null)
            onClick();

        return clicked;
    }

    /// <summary>
    /// Draws a group of toolbar buttons with automatic spacing using SameLine().
    /// </summary>
    /// <param name="buttons">Array of button configurations (label, onClick)</param>
    public static void DrawToolbarButtonGroup(params (string Label, Action OnClick)[] buttons)
    {
        for (var i = 0; i < buttons.Length; i++)
        {
            if (i > 0)
                ImGui.SameLine();

            var (label, onClick) = buttons[i];
            if (ImGui.Button(label))
                onClick.Invoke();
        }
    }

    /// <summary>
    /// Draws a toggle button that changes appearance based on state.
    /// Useful for on/off toggles like Loop, Play/Pause, etc.
    /// </summary>
    /// <param name="labelWhenOn">Label to show when state is true</param>
    /// <param name="labelWhenOff">Label to show when state is false</param>
    /// <param name="state">Current state (will be toggled if clicked)</param>
    /// <param name="width">Button width (default: StandardButtonWidth)</param>
    /// <param name="height">Button height (default: StandardButtonHeight)</param>
    /// <returns>True if the button was clicked (state was toggled)</returns>
    public static bool DrawToggleButton(string labelWhenOn, string labelWhenOff, ref bool state,
        float width = EditorUIConstants.StandardButtonWidth,
        float height = EditorUIConstants.StandardButtonHeight)
    {
        var label = state ? labelWhenOn : labelWhenOff;

        if (state) 
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.8f, 0.7f));

        var clicked = ImGui.Button(label, new Vector2(width, height));

        if (state) 
            ImGui.PopStyleColor();

        if (clicked) 
            state = !state;

        return clicked;
    }

    /// <summary>
    /// Draws a pair of OK/Cancel modal buttons with standard spacing.
    /// </summary>
    /// <param name="okLabel">Label for OK button (default: "OK")</param>
    /// <param name="cancelLabel">Label for Cancel button (default: "Cancel")</param>
    /// <param name="onOk">Callback when OK is clicked</param>
    /// <param name="onCancel">Callback when Cancel is clicked</param>
    /// <param name="okDisabled">Whether OK button should be disabled</param>
    public static void DrawModalButtonPair(string okLabel = "OK", string cancelLabel = "Cancel",
        Action? onOk = null, Action? onCancel = null, bool okDisabled = false)
    {
        DrawModalButton(okLabel, onOk, disabled: okDisabled);
        ImGui.SameLine();
        DrawModalButton(cancelLabel, onCancel);
    }
}