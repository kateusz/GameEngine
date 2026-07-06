using System.Numerics;
using ECS;
using Editor.ComponentEditors;
using Editor.ComponentEditors.Core;
using Editor.Features.Components;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class PropertiesPanel(
    IPrefabManager prefabManager,
    IComponentEditorRegistry componentEditors,
    ISceneContext sceneContext,
    SceneToolbar sceneToolbar,
    GameComponentEditor gameComponentEditor,
    IEditorSelection selection)
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

        ComponentSelector.Draw(selectedEntity, sceneContext.ActiveScene, gameComponentEditor);
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

        var dimension = scene.Dimension;
        if (ImGui.RadioButton("2D", dimension == SceneDimension.TwoD))
        {
            scene.Dimension = SceneDimension.TwoD;
            sceneToolbar.ApplyGridFromScene(scene);
        }

        ImGui.SameLine();
        if (ImGui.RadioButton("3D", dimension == SceneDimension.ThreeD))
        {
            scene.Dimension = SceneDimension.ThreeD;
            sceneToolbar.ApplyGridFromScene(scene);
        }

        var backgroundColor = scene.BackgroundColor;
        if (ImGui.ColorEdit4("Background Color", ref backgroundColor,
                ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                ImGuiColorEditFlags.NoOptions))
        {
            scene.BackgroundColor = backgroundColor;
        }
    }
}
