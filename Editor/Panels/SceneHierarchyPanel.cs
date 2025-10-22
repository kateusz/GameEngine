using System.Numerics;
using ECS;
using Editor.Panels.Elements;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class SceneHierarchyPanel : ISceneView
{
    private Scene? _context;
    private Entity? _selectionContext;
    private readonly EntityContextMenu _contextMenu;

    public Action<Entity> EntitySelected;

    /// <summary>
    /// Event raised when the selected entity changes.
    /// </summary>
    public event Action<Entity?>? SelectionChanged;

    public SceneHierarchyPanel(Scene context)
    {
        _context = context;
        _contextMenu = new EntityContextMenu();
    }

    public void SetContext(Scene? context)
    {
        _context = context;
        _selectionContext = null;
        // Raise event to notify subscribers that selection was cleared
        SelectionChanged?.Invoke(null);
    }

    public void OnImGuiRender()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Scene Hierarchy");

        if (_context != null)
        {
            foreach (var entity in _context.Entities)
            {
                DrawEntityNode(entity);
            }

            if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered())
            {
                if (_selectionContext != null)
                {
                    _selectionContext = null;
                    SelectionChanged?.Invoke(null);
                }
            }

            EntityContextMenu.Render(_context);
        }

        ImGui.End();
    }

    public void SetSelectedEntity(Entity entity)
    {
        _selectionContext = entity;
        SelectionChanged?.Invoke(entity);
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
            EntitySelected?.Invoke(entity);
            _selectionContext = entity;
            SelectionChanged?.Invoke(entity);
        }

        // TODO: finish dependency injection
        // Prefab drag & drop handling
        //PrefabDropTarget.HandleEntityDrop(entity);

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
            _context?.DestroyEntity(entity);
            if (Equals(_selectionContext, entity))
            {
                _selectionContext = null;
                SelectionChanged?.Invoke(null);
            }
        }
    }
}