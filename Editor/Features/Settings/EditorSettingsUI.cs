using Editor.UI.Drawers;
using Engine.Core;
using ImGuiNET;

namespace Editor.Features.Settings;

public class EditorSettingsUI(IEditorPreferences editorPreferences, DebugSettings debugSettings)
{
    private const int MinAutosaveIntervalSeconds = 5;
    private bool _open;

    public void Show() => _open = true;

    public void Render()
    {
        if (!ModalDrawer.BeginCenteredModal("Editor Settings", ref _open))
            return;
        
        ImGui.SeparatorText("Debug Visualization");

        var showColliders = editorPreferences.ShowColliderBounds;
        if (ImGui.Checkbox("Show Collider Bounds", ref showColliders))
        {
            editorPreferences.ShowColliderBounds = showColliders;
            debugSettings.ShowColliderBounds = showColliders;
            editorPreferences.Save();
        }

        var showWireframe = editorPreferences.ShowWireframe;
        if (ImGui.Checkbox("Show Wireframe", ref showWireframe))
        {
            editorPreferences.ShowWireframe = showWireframe;
            editorPreferences.Save();
        }

        var showVignette = editorPreferences.ShowVignette;
        if (ImGui.Checkbox("Show Vignette", ref showVignette))
        {
            editorPreferences.ShowVignette = showVignette;
            editorPreferences.Save();
        }

        var vignetteIntensity = editorPreferences.VignetteIntensity;
        if (ImGui.SliderFloat("Vignette Intensity", ref vignetteIntensity, 0f, 1f))
        {
            editorPreferences.VignetteIntensity = vignetteIntensity;
            editorPreferences.Save();
        }

        var vignetteRadius = editorPreferences.VignetteRadius;
        if (ImGui.SliderFloat("Vignette Radius", ref vignetteRadius, 0f, 1f))
        {
            editorPreferences.VignetteRadius = vignetteRadius;
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
        ImGui.SeparatorText("Autosave");

        var autosaveSeconds = editorPreferences.AutosaveIntervalSeconds;
        if (ImGui.DragInt("Interval (seconds, 0 = off)", ref autosaveSeconds, 1, 0, 3600))
        {
            if (autosaveSeconds is > 0 and < MinAutosaveIntervalSeconds)
                autosaveSeconds = MinAutosaveIntervalSeconds;
            editorPreferences.AutosaveIntervalSeconds = autosaveSeconds;
            editorPreferences.Save();
        }

        ModalDrawer.EndModal();
    }
}