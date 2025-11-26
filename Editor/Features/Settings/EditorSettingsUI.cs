using System.Numerics;
using Editor.UI.Drawers;
using Engine.Core;
using ImGuiNET;

namespace Editor.Features.Settings;

public class EditorSettingsUI
{
    private bool _open;

    private readonly IEditorPreferences _editorPreferences;
    private readonly DebugSettings _debugSettings;

    public EditorSettingsUI(IEditorPreferences editorPreferences, DebugSettings debugSettings)
    {
        _editorPreferences = editorPreferences;
        _debugSettings = debugSettings;
    }

    public void Show() => _open = true;

    public void Render()
    {
        // Use ModalDrawer for consistent modal handling and centering
        if (!ModalDrawer.BeginCenteredModal("Editor Settings", ref _open))
            return;

        // --- Background Color ---
        ImGui.Text("Editor Background Color");
        var backgroundColor = _editorPreferences.BackgroundColor;
        if (ImGui.ColorEdit4("Background Color", ref backgroundColor))
        {
            _editorPreferences.BackgroundColor = backgroundColor;
            _editorPreferences.Save();
        }

        ImGui.Separator();

        // --- Debug Visualization Settings ---
        ImGui.SeparatorText("Debug Visualization");

        bool showColliders = _editorPreferences.ShowColliderBounds;
        if (ImGui.Checkbox("Show Collider Bounds", ref showColliders))
        {
            _editorPreferences.ShowColliderBounds = showColliders;
            _debugSettings.ShowColliderBounds = showColliders;
            _editorPreferences.Save();
        }

        bool showFps = _editorPreferences.ShowFPS;
        if (ImGui.Checkbox("Show FPS Counter", ref showFps))
        {
            _editorPreferences.ShowFPS = showFps;
            _debugSettings.ShowFPS = showFps;
            _editorPreferences.Save();
        }

        ModalDrawer.EndModal();
    }

    /// <summary>
    /// Gets the current background color from preferences.
    /// </summary>
    public Vector4 GetBackgroundColor() => _editorPreferences.BackgroundColor;
}