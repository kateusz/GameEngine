using System.Numerics;
using ECS;
using Editor.ComponentEditors;
using Editor.ComponentEditors.Core;
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
            return;

        EntityNameEditor.Draw(selectedEntity);
        ImGui.Spacing();

        ComponentSelector.Draw(selectedEntity, sceneContext.ActiveScene, gameComponentEditor);
        ImGui.SameLine();

        ButtonDrawer.DrawButton("Save as Prefab",
            () => prefabManager.ShowSavePrefabPopup(selectedEntity));

        componentEditors.DrawAllComponents(selectedEntity);
    }
}
