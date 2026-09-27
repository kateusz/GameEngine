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

        var followHierarchy = editorPreferences.FollowViewportSelectionInHierarchy;
        if (ImGui.Checkbox("Follow viewport selection in Scene Hierarchy", ref followHierarchy))
        {
            editorPreferences.FollowViewportSelectionInHierarchy = followHierarchy;
            editorPreferences.Save();
        }

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

        ImGui.SeparatorText("Rendering");

        var fxaa = editorPreferences.Fxaa;
        if (ImGui.Checkbox("FXAA", ref fxaa))
        {
            editorPreferences.Fxaa = fxaa;
            editorPreferences.Save();
        }

        ImGui.Separator();
        ImGui.SeparatorText("Autosave");

        LayoutDrawer.DrawFormLabel("Interval");
        LayoutDrawer.DrawTooltip("Seconds (0 = off)");
        ImGui.SetNextItemWidth(600f);
        var autosaveSeconds = editorPreferences.AutosaveIntervalSeconds;
        if (ImGui.InputInt("##autosaveInterval", ref autosaveSeconds))
        {
            autosaveSeconds = System.Math.Clamp(autosaveSeconds, 0, 3600);
            if (autosaveSeconds is > 0 and < MinAutosaveIntervalSeconds)
                autosaveSeconds = MinAutosaveIntervalSeconds;
            editorPreferences.AutosaveIntervalSeconds = autosaveSeconds;
            editorPreferences.Save();
        }

        ModalDrawer.EndModal();
    }
}