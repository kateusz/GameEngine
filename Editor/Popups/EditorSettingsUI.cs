using System.Numerics;
using Engine.Core;
using Engine.Renderer.Cameras;
using ImGuiNET;

namespace Editor.Popups;

public class EditorSettingsUI
{
    private bool _open;
    
    private readonly EditorPreferences _editorPreferences;

    public EditorSettingsUI(EditorPreferences editorPreferences)
    {
        _editorPreferences = editorPreferences;
    }

    public void Show() => _open = true;

    public void Render()
    {
        // Open the popup when _open is set to true
        if (_open)
            ImGui.OpenPopup("Editor Settings");

        // Center the popup on first appearance
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        // Use BeginPopupModal for proper popup behavior
        if (!ImGui.BeginPopupModal("Editor Settings", ref _open,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
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
            DebugSettings.Instance.ShowColliderBounds = showColliders;
            _editorPreferences.Save();
        }

        bool showFps = _editorPreferences.ShowFPS;
        if (ImGui.Checkbox("Show FPS Counter", ref showFps))
        {
            _editorPreferences.ShowFPS = showFps;
            DebugSettings.Instance.ShowFPS = showFps;
            _editorPreferences.Save();
        }

        ImGui.EndPopup();
    }

    /// <summary>
    /// Gets the current background color from preferences.
    /// </summary>
    public Vector4 GetBackgroundColor() => _editorPreferences.BackgroundColor;
}