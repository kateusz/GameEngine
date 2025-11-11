using System.Numerics;
using ECS;
using Editor.Core;
using Editor.Panels.ComponentEditors;
using Editor.Panels.Elements;
using ImGuiNET;

namespace Editor.Panels;

public class PropertiesPanel : IPropertiesPanel, IEditorPanel
{
    private Entity? _selectedEntity;
    private readonly IComponentEditorRegistry _componentEditors;
    private readonly IPrefabManager _prefabManager;
    private readonly EditorEventBus _eventBus;

    // IEditorPanel implementation
    public string Id => "Properties";
    public string Title => "Properties";
    public bool IsVisible { get; set; } = true;

    public PropertiesPanel(IPrefabManager prefabManager, IComponentEditorRegistry componentEditors, EditorEventBus eventBus)
    {
        _prefabManager = prefabManager;
        _componentEditors = componentEditors;
        _eventBus = eventBus;

        // Subscribe to entity selection events
        _eventBus.Subscribe<EntitySelectedEvent>(OnEntitySelected);
        _eventBus.Subscribe<EntityDeselectedEvent>(OnEntityDeselected);
    }

    private void OnEntitySelected(EntitySelectedEvent evt)
    {
        SetSelectedEntity(evt.Entity);
    }

    private void OnEntityDeselected(EntityDeselectedEvent evt)
    {
        SetSelectedEntity(null);
    }

    public void SetSelectedEntity(Entity? entity)
    {
        if (_selectedEntity?.Id != entity?.Id)
            _selectedEntity = entity;
    }

    public void Draw()
    {
        OnImGuiRender();
        _prefabManager.RenderPopups();
    }

    public void OnImGuiRender()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(280, 400), ImGuiCond.FirstUseEver);

        var isVisible = IsVisible;
        if (ImGui.Begin(Title, ref isVisible))
        {
            if (_selectedEntity != null)
            {
                DrawEntityProperties();
            }
            else
            {
                ImGui.TextDisabled("No entity selected");
            }
        }
        ImGui.End();

        IsVisible = isVisible;
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