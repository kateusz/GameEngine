using System.Numerics;
using ECS;
using Editor.Panels.ComponentEditors;
using Editor.Panels.Elements;
using ImGuiNET;

namespace Editor.Panels;

public class PropertiesPanel : IPropertiesPanel
{
    private Entity? _selectedEntity;
    private readonly ComponentEditorRegistry _componentEditors;
    private readonly IPrefabManager _prefabManager;

    public PropertiesPanel(IPrefabManager prefabManager, ComponentEditorRegistry componentEditors)
    {
        _prefabManager = prefabManager;
        _componentEditors = componentEditors;
    }

    public void SetSelectedEntity(Entity? entity)
    {
        if (_selectedEntity?.Id != entity?.Id)
            _selectedEntity = entity;
    }

    public void OnImGuiRender()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Properties");
        
        if (_selectedEntity != null)
        {
            DrawEntityProperties();
        }
        
        ImGui.End();
        
        _prefabManager.RenderPopups();
    }

    private void DrawEntityProperties()
    {
        // Entity name/tag editing
        EntityNameEditor.Draw(_selectedEntity);
        ImGui.Spacing();

        // Add component button and popup
        ComponentSelector.Draw(_selectedEntity);
        ImGui.SameLine();
        
        // Save as prefab button
        if (ImGui.Button("Save as Prefab"))
        {
            _prefabManager?.ShowSavePrefabDialog(_selectedEntity);
        }

        // Render all components
        _componentEditors.DrawAllComponents(_selectedEntity);
    }
}