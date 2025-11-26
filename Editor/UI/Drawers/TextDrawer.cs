using System.Numerics;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.UI.Drawers;

/// <summary>
/// Helper class for rendering colored text with ImGui.
/// </summary>
public static class TextDrawer
{
    /// <summary>
    /// Renders text with a specific color.
    /// </summary>
    /// <param name="text">Text to render</param>
    /// <param name="color">Color to apply</param>
    public static void DrawColoredText(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text(text);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Renders error text (bright red).
    /// </summary>
    /// <param name="text">Error message to display</param>
    public static void DrawErrorText(string text) => DrawColoredText(text, EditorUIConstants.ErrorColor);

    /// <summary>
    /// Renders warning text (bright yellow).
    /// </summary>
    /// <param name="text">Warning message to display</param>
    public static void DrawWarningText(string text) => DrawColoredText(text, EditorUIConstants.WarningColor);

    /// <summary>
    /// Renders success text (bright green).
    /// </summary>
    /// <param name="text">Success message to display</param>
    public static void DrawSuccessText(string text) => DrawColoredText(text, EditorUIConstants.SuccessColor);

    /// <summary>
    /// Renders info text (light gray).
    /// </summary>
    /// <param name="text">Info message to display</param>
    public static void DrawInfoText(string text) => DrawColoredText(text, EditorUIConstants.InfoColor);
}
