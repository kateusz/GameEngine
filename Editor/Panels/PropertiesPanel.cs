using System.Numerics;
using ECS;
using Editor.ComponentEditors;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using ImGuiNET;

namespace Editor.Panels;

public class PropertiesPanel : IPropertiesPanel
{
    private Entity? _selectedEntity;
    private readonly IComponentEditorRegistry _componentEditors;
    private readonly IPrefabManager _prefabManager;

    public PropertiesPanel(IPrefabManager prefabManager, IComponentEditorRegistry componentEditors)
    {
        _prefabManager = prefabManager;
        _componentEditors = componentEditors;
    }

    public void SetSelectedEntity(Entity? entity)
    {
        if (_selectedEntity?.Id != entity?.Id)
            _selectedEntity = entity;
    }

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(280, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Properties");
        DrawEntityProperties();
        ImGui.End();

        _prefabManager.RenderPopups();
    }

    private void DrawEntityProperties()
    {
        if (_selectedEntity is null) return;

        // Entity name/tag editing
        EntityNameEditor.Draw(_selectedEntity);
        ImGui.Spacing();

        // Add component button and popup
        ComponentSelector.Draw(_selectedEntity);
        ImGui.SameLine();

        // Save as prefab button
        ButtonDrawer.DrawButton("Save as Prefab",
            () => _prefabManager.ShowSavePrefabPopup(_selectedEntity));

        // Render all components
        _componentEditors.DrawAllComponents(_selectedEntity);
    }
}