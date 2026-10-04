using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer.Pipeline;
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

        ImGui.SeparatorText("Shadows");

        var current = new ShadowQuality(
            editorPreferences.SunShadowResolution,
            editorPreferences.PointShadowResolution,
            editorPreferences.ShadowDistance,
            editorPreferences.ShadowPcf,
            editorPreferences.PointShadowPcf,
            editorPreferences.ShadowCascades).Sanitized();
        var preset = ShadowQuality.Matching(current);
        var presetIndex = preset switch
        {
            ShadowPreset.Medium => 1,
            ShadowPreset.High => 2,
            ShadowPreset.Low => 0,
            _ => 3
        };
        LayoutDrawer.DrawFormLabel("Quality");
        LayoutDrawer.DrawTooltip("Fills the fields below. Low is the current look. Medium raises the sun map and adds a near cascade. High also raises point maps and widens PCF.");
        ImGui.SetNextItemWidth(600f);
        if (ImGui.Combo("##shadowQuality", ref presetIndex, "Low\0" + "Medium\0" + "High\0" + "Custom\0")
            && presetIndex < 3)
            ApplyShadowQuality(ShadowQuality.For((ShadowPreset)presetIndex));

        LayoutDrawer.DrawFormLabel("Sun distance");
        ImGui.SetNextItemWidth(600f);
        var distance = editorPreferences.ShadowDistance;
        if (ImGui.SliderFloat("##shadowDistance", ref distance, 1f, 200f, "%.0f"))
        {
            editorPreferences.ShadowDistance = distance;
            editorPreferences.Save();
        }

        if (ResolutionCombo("Sun map", current.DirectionalResolution, SunSizes, out var sun))
        {
            editorPreferences.SunShadowResolution = sun;
            editorPreferences.Save();
        }

        if (ResolutionCombo("Point map", current.PointResolution, PointSizes, out var point))
        {
            editorPreferences.PointShadowResolution = point;
            editorPreferences.Save();
        }

        LayoutDrawer.DrawFormLabel("PCF");
        ImGui.SetNextItemWidth(600f);
        var pcfIndex = current.PcfTaps >= 5 ? 2 : current.PcfTaps >= 3 ? 1 : 0;
        if (ImGui.Combo("##shadowPcf", ref pcfIndex, "1x1 (hard)\0" + "3x3\0" + "5x5\0"))
        {
            editorPreferences.ShadowPcf = pcfIndex switch { 2 => 5, 1 => 3, _ => 1 };
            editorPreferences.Save();
        }

        var pointPcf = editorPreferences.PointShadowPcf;
        if (ImGui.Checkbox("PCF on point lights", ref pointPcf))
        {
            editorPreferences.PointShadowPcf = pointPcf;
            editorPreferences.Save();
        }

        LayoutDrawer.DrawFormLabel("Sun cascades");
        ImGui.SetNextItemWidth(600f);
        var cascadeIndex = current.Cascades >= 2 ? 1 : 0;
        if (ImGui.Combo("##shadowCascades", ref cascadeIndex, "1\0" + "2\0"))
        {
            editorPreferences.ShadowCascades = cascadeIndex == 1 ? 2 : 1;
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

    private static readonly int[] SunSizes = [1024, 2048, 4096];
    private static readonly int[] PointSizes = [256, 512, 1024];

    private void ApplyShadowQuality(ShadowQuality quality)
    {
        editorPreferences.SunShadowResolution = quality.DirectionalResolution;
        editorPreferences.PointShadowResolution = quality.PointResolution;
        editorPreferences.ShadowDistance = quality.Distance;
        editorPreferences.ShadowPcf = quality.PcfTaps;
        editorPreferences.PointShadowPcf = quality.PointPcf;
        editorPreferences.ShadowCascades = quality.Cascades;
        editorPreferences.Save();
    }

    private static bool ResolutionCombo(string label, int current, int[] sizes, out int chosen)
    {
        var index = Array.IndexOf(sizes, current);
        if (index < 0)
            index = 0;

        LayoutDrawer.DrawFormLabel(label);
        ImGui.SetNextItemWidth(600f);
        var labels = sizes[0] + "\0" + sizes[1] + "\0" + sizes[2] + "\0";
        chosen = current;
        if (!ImGui.Combo("##" + label, ref index, labels))
            return false;

        chosen = sizes[index];
        return chosen != current;
    }
}