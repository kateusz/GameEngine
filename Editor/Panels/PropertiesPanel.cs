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
    }
}
