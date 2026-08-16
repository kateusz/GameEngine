using System.Numerics;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.Selection;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;
using GameComponentEditor = Editor.Features.Components.GameComponentEditor;

namespace Editor.Panels;

public class PropertiesPanel(
    IPrefabManager prefabManager,
    IComponentEditorRegistry componentEditors,
    ISceneContext sceneContext,
    GameComponentEditor gameComponentEditor,
    IEditorSelection selection,
    IEditorHistory history)
    : IPropertiesPanel, IEditorPanel
{
    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(280, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Properties");
        DrawEntityProperties();
        ImGui.End();

        prefabManager.RenderPopups();
    }

    private void DrawEntityProperties()
    {
        if (selection.SelectedEntity is not { } selectedEntity)
        {
            DrawSceneProperties();
            return;
        }

        EntityNameEditor.Draw(selectedEntity);
        ImGui.Spacing();

        ComponentSelector.Draw(selectedEntity, sceneContext.ActiveScene!, gameComponentEditor, history);
        ImGui.SameLine();

        ButtonDrawer.DrawButton("Save as Prefab",
            () => prefabManager.ShowSavePrefabPopup(selectedEntity));

        componentEditors.DrawAllComponents(selectedEntity);
    }

    private void DrawSceneProperties()
    {
        if (sceneContext.ActiveScene is not { } scene)
            return;

        ImGui.SeparatorText("Scene");

        var backgroundColor = scene.BackgroundColor;
        if (ImGui.ColorEdit4("Background Color", ref backgroundColor,
                ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                ImGuiColorEditFlags.NoOptions))
        {
            scene.BackgroundColor = backgroundColor;
        }

        ImGui.Spacing();
        ImGui.SeparatorText("Post Process");

        var exposure = scene.PostProcess.Exposure;
        if (ImGui.DragFloat("Exposure", ref exposure, 0.01f, 0.1f, 8f))
            scene.PostProcess = scene.PostProcess with { Exposure = exposure };

        var bloomEnabled = scene.PostProcess.BloomEnabled;
        if (ImGui.Checkbox("Bloom", ref bloomEnabled))
            scene.PostProcess = scene.PostProcess with { BloomEnabled = bloomEnabled };

        var bloomThreshold = scene.PostProcess.BloomThreshold;
        if (ImGui.DragFloat("Bloom Threshold", ref bloomThreshold, 0.01f, 0f, 10f))
            scene.PostProcess = scene.PostProcess with { BloomThreshold = bloomThreshold };

        var bloomIntensity = scene.PostProcess.BloomIntensity;
        if (ImGui.DragFloat("Bloom Intensity", ref bloomIntensity, 0.01f, 0f, 8f))
            scene.PostProcess = scene.PostProcess with { BloomIntensity = bloomIntensity };
    }
}
