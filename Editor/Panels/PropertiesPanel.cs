using System.Numerics;
using ECS;
using Editor.Panels.ComponentEditors;
using Editor.Panels.Elements;
using ImGuiNET;

namespace Editor.Panels;

public class PropertiesPanel
{
    private Entity? _selectedEntity;
    private readonly ComponentEditorRegistry _componentEditors;
    private readonly PrefabManager _prefabManager;

    public PropertiesPanel()
    {
        _componentEditors = new ComponentEditorRegistry();
        _prefabManager = new PrefabManager();
    }

    public void SetSelectedEntity(Entity? entity)
    {
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
            _prefabManager.ShowSavePrefabDialog(_selectedEntity);
        }

        // Render all components
        _componentEditors.DrawAllComponents(_selectedEntity);
    }
}