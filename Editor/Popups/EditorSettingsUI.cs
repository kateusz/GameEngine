using System.Numerics;
using Editor.Core;
using Engine.Core;
using Engine.Renderer.Cameras;
using ImGuiNET;

namespace Editor.Popups;

public class EditorSettingsUI : IEditorPopup
{
    private readonly IEditorPreferences _editorPreferences;

    // IEditorPopup implementation
    public string Id => "EditorSettings";
    public bool IsOpen { get; private set; }

    public EditorSettingsUI(IEditorPreferences editorPreferences)
    {
        _editorPreferences = editorPreferences;
    }

    public void Show()
    {
        IsOpen = true;
    }

    public void OnImGuiRender()
    {
        // Open the popup when IsOpen is set to true
        if (IsOpen)
            ImGui.OpenPopup(Id);

        // Center the popup on first appearance
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        // Use BeginPopupModal for proper popup behavior
        var isOpen = IsOpen;
        if (ImGui.BeginPopupModal(Id, ref isOpen, // Use Id here, not "Editor Settings"
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
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

        // Update state after popup ends (captures X button clicks)
        IsOpen = isOpen;
    }

    public void OnClose()
    {
        // Nothing to clean up
    }

    public void Render()
    {
        OnImGuiRender();
    }

    /// <summary>
    /// Gets the current background color from preferences.
    /// </summary>
    public Vector4 GetBackgroundColor() => _editorPreferences.BackgroundColor;
}