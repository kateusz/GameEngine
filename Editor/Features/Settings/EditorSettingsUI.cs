using System.Numerics;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer;
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

        ImGui.Separator();
        ImGui.SeparatorText("Bloom");

        var bloomEnabled = editorPreferences.BloomEnabled;
        if (ImGui.Checkbox("Enable Bloom", ref bloomEnabled))
        {
            editorPreferences.BloomEnabled = bloomEnabled;
            editorPreferences.Save();
        }

        var bloomThreshold = editorPreferences.BloomThreshold;
        if (ImGui.SliderFloat("Bloom Threshold", ref bloomThreshold, 0.1f, 8.0f, "%.2f"))
        {
            editorPreferences.BloomThreshold = bloomThreshold;
            editorPreferences.Save();
        }

        var bloomIntensity = editorPreferences.BloomIntensity;
        if (ImGui.SliderFloat("Bloom Intensity", ref bloomIntensity, 0.0f, 2.0f, "%.2f"))
        {
            editorPreferences.BloomIntensity = bloomIntensity;
            editorPreferences.Save();
        }

        var bloomBlurPasses = editorPreferences.BloomBlurPasses;
        if (ImGui.SliderInt("Bloom Blur Passes", ref bloomBlurPasses, 1, 10))
        {
            editorPreferences.BloomBlurPasses = bloomBlurPasses;
            editorPreferences.Save();
        }

        var bloomSoftKnee = editorPreferences.BloomSoftKnee;
        if (ImGui.SliderFloat("Bloom Soft Knee", ref bloomSoftKnee, 0.0f, 1.0f, "%.2f"))
        {
            editorPreferences.BloomSoftKnee = bloomSoftKnee;
            editorPreferences.Save();
        }

        var bloomDownsampleFactor = editorPreferences.BloomDownsampleFactor;
        if (ImGui.SliderInt("Bloom Downsample", ref bloomDownsampleFactor, 1, 4))
        {
            editorPreferences.BloomDownsampleFactor = bloomDownsampleFactor;
            editorPreferences.Save();
        }

        var bloomExposure = editorPreferences.BloomExposure;
        if (ImGui.SliderFloat("Bloom Exposure", ref bloomExposure, 0.1f, 3.0f, "%.2f"))
        {
            editorPreferences.BloomExposure = bloomExposure;
            editorPreferences.Save();
        }

        var bloomGamma = editorPreferences.BloomGamma;
        if (ImGui.SliderFloat("Bloom Gamma", ref bloomGamma, 1.0f, 3.0f, "%.2f"))
        {
            editorPreferences.BloomGamma = bloomGamma;
            editorPreferences.Save();
        }

        ModalDrawer.EndModal();
    }

    /// <summary>
    /// Gets the current background color from preferences.
    /// </summary>
    public Vector4 GetBackgroundColor() => editorPreferences.BackgroundColor;

    public BloomSettings GetBloomSettings() => new(
        editorPreferences.BloomEnabled,
        editorPreferences.BloomThreshold,
        editorPreferences.BloomSoftKnee,
        editorPreferences.BloomIntensity,
        editorPreferences.BloomBlurPasses,
        editorPreferences.BloomDownsampleFactor,
        editorPreferences.BloomExposure,
        editorPreferences.BloomGamma);
}