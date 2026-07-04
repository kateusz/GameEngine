using System.Numerics;
using ECS;
using Editor.ComponentEditors;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class PropertiesPanel(
    IPrefabManager prefabManager,
    IComponentEditorRegistry componentEditors,
    ISceneContext sceneContext,
    GameComponentEditor gameComponentEditor)
    : IPropertiesPanel
{
    private IReadOnlyList<Entity> _selectedEntities = [];

    public void SetSelectedEntity(Entity? entity) =>
        _selectedEntities = entity is null ? [] : [entity];

    public void SetSelectedEntities(IReadOnlyList<Entity> entities) =>
        _selectedEntities = entities;

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
        if (_selectedEntities.Count == 0)
            return;

        if (_selectedEntities.Count == 1)
        {
            var entity = _selectedEntities[0];
            EntityNameEditor.Draw(entity);
            ImGui.Spacing();

            ComponentSelector.Draw(entity, sceneContext.ActiveScene, gameComponentEditor);
            ImGui.SameLine();

            ButtonDrawer.DrawButton("Save as Prefab",
                () => prefabManager.ShowSavePrefabPopup(entity));

            componentEditors.DrawAllComponents(entity);
            return;
        }

        ImGui.TextUnformatted($"{_selectedEntities.Count} entities selected");
        ImGui.Spacing();
        componentEditors.DrawCommonComponents(_selectedEntities);
    }
}
