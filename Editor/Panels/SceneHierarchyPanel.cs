using System.Numerics;
using ECS;
using Editor.Panels.Elements;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class SceneHierarchyPanel
{
    private readonly EntityContextMenu _contextMenu;
    private readonly PrefabDropTarget _prefabDropTarget;
    
    private Scene _context;
    private Entity? _selectionContext;
    
    public Action<Entity> EntitySelected;

    public SceneHierarchyPanel(EntityContextMenu contextMenu, PrefabDropTarget prefabDropTarget)
    {
        _contextMenu = contextMenu;
        _prefabDropTarget = prefabDropTarget;
    }

    public void SetContext(Scene context)
    {
        _context = context;
        _selectionContext = null;
    }

    public void OnImGuiRender()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Scene Hierarchy");

        foreach (var entity in _context.Entities)
        {
            DrawEntityNode(entity);
        }

        if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered())
            _selectionContext = null;

        _contextMenu.Render(_context);

        ImGui.End();
    }

    public void SetSelectedEntity(Entity entity)
    {
        _selectionContext = entity;
    }

    public Entity? GetSelectedEntity() => _selectionContext;

    private void DrawEntityNode(Entity entity)
    {
        var tag = entity.Name;
        var flags = (_selectionContext?.Id == entity.Id ? ImGuiTreeNodeFlags.Selected : 0) |
                    ImGuiTreeNodeFlags.OpenOnArrow;
        var opened = ImGui.TreeNodeEx(tag, flags, tag);

        if (ImGui.IsItemClicked())
        {
            EntitySelected.Invoke(entity);
            _selectionContext = entity;
        }
        
        // Prefab drag & drop handling
        _prefabDropTarget.HandleEntityDrop(entity);

        // Entity context menu
        bool entityDeleted = false;
        if (ImGui.BeginPopupContextItem())
        {
            if (ImGui.MenuItem("Delete Entity"))
                entityDeleted = true;
            ImGui.EndPopup();
        }

        if (opened)
        {
            ImGui.TreePop();
        }

        if (entityDeleted)
        {
            _context.DestroyEntity(entity);
            if (Equals(_selectionContext, entity))
                _selectionContext = null;
        }
    }
}