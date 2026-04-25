using System.Numerics;
using Editor.UI.Drawers;
using Engine.Core;
using ImGuiNET;

namespace Editor.Features.Settings;

public class EditorSettingsUI(IEditorPreferences editorPreferences, DebugSettings debugSettings)
{
    private bool _open;

    public void Show() => _open = true;

    public void Render()
    {
        if (!ModalDrawer.BeginCenteredModal("Editor Settings", ref _open))
            return;
        
        ImGui.Text("Editor Background Color");
        var backgroundColor = editorPreferences.BackgroundColor;
        if (ImGui.ColorEdit4("Background Color", ref backgroundColor,
                ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                ImGuiColorEditFlags.NoOptions))
        {
            editorPreferences.BackgroundColor = backgroundColor;
            editorPreferences.Save();
        }

        ImGui.Separator();
        ImGui.SeparatorText("Debug Visualization");

        var showColliders = editorPreferences.ShowColliderBounds;
        if (ImGui.Checkbox("Show Collider Bounds", ref showColliders))
        {
            editorPreferences.ShowColliderBounds = showColliders;
            debugSettings.ShowColliderBounds = showColliders;
            editorPreferences.Save();
        }

        var showFps = editorPreferences.ShowFPS;
        if (ImGui.Checkbox("Show FPS Counter", ref showFps))
        {
            editorPreferences.ShowFPS = showFps;
            debugSettings.ShowFPS = showFps;
            editorPreferences.Save();
        }

        ImGui.Separator();
        ImGui.SeparatorText("HDR");

        var hdrExposure = editorPreferences.HdrExposure;
        if (ImGui.DragFloat("Exposure", ref hdrExposure, 0.01f, 0.1f, 8.0f))
        {
            editorPreferences.HdrExposure = hdrExposure;
            editorPreferences.Save();
        }

        ModalDrawer.EndModal();
    }

    /// <summary>
    /// Gets the current background color from preferences.
    /// </summary>
    public Vector4 GetBackgroundColor() => editorPreferences.BackgroundColor;

    public float GetHdrExposure() => editorPreferences.HdrExposure;
}