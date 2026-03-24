using System.Numerics;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer.Profiling;
using ImGuiNET;

namespace Editor.Features.Settings;

public class EditorSettingsUI(IEditorPreferences editorPreferences, DebugSettings debugSettings, IPerformanceProfiler profiler)
{
    private bool _open;

    public void Show() => _open = true;

    public void Render()
    {
        if (!ModalDrawer.BeginCenteredModal("Editor Settings", ref _open))
            return;
        
        ImGui.Text("Editor Background Color");
        var backgroundColor = editorPreferences.BackgroundColor;
        if (ImGui.ColorEdit4("Background Color", ref backgroundColor))
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

        var showOverlay = editorPreferences.ShowPerformanceOverlay;
        if (ImGui.Checkbox("Show Performance Overlay", ref showOverlay))
        {
            editorPreferences.ShowPerformanceOverlay = showOverlay;
            debugSettings.ShowPerformanceOverlay = showOverlay;
            editorPreferences.Save();
        }

        var profilerEnabled = editorPreferences.ProfilerEnabled;
        if (ImGui.Checkbox("Enable Performance Profiler", ref profilerEnabled))
        {
            editorPreferences.ProfilerEnabled = profilerEnabled;
            profiler.Enabled = profilerEnabled;
            editorPreferences.Save();
        }

        ModalDrawer.EndModal();
    }

    /// <summary>
    /// Gets the current background color from preferences.
    /// </summary>
    public Vector4 GetBackgroundColor() => editorPreferences.BackgroundColor;
}