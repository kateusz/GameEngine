using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Features.Scene;

public class SceneHierarchyPanel : ISceneHierarchyPanel
{
    private readonly EntityContextMenu _contextMenu;
    private readonly PrefabDropTarget _prefabDropTarget;

    private IScene _scene;
    private Entity? _selectionContext;

    // Search/Filter state
    private string _searchQuery = string.Empty;
    private readonly List<Entity> _filteredEntities = [];
    private bool _isFilterActive;

    public Action<Entity> EntitySelected { get; set; } = null!;

    public SceneHierarchyPanel(EntityContextMenu contextMenu, PrefabDropTarget prefabDropTarget)
    {
        _contextMenu = contextMenu;
        _prefabDropTarget = prefabDropTarget;
    }
    
    public void SetScene(IScene scene)
    {
        _scene = scene;
        _selectionContext = null;
    }

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Scene Hierarchy");

        RenderSearchInput();

        if (_isFilterActive)
            RenderFilterStatus();

        RenderEntityHierarchy();

        if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered())
            _selectionContext = null;

        _contextMenu.Render(_scene);

        ImGui.End();
    }

    public void SetSelectedEntity(Entity entity) => _selectionContext = entity;

    public Entity? GetSelectedEntity() => _selectionContext;

    private void DrawEntityNode(Entity entity)
    {
        var tag = entity.Name;
        bool isSelected = _selectionContext?.Id == entity.Id;
        bool entityDeleted = false;

        bool opened = TreeDrawer.DrawSelectableTreeNode(
            label: tag,
            isSelected: isSelected,
            onClicked: () =>
            {
                EntitySelected.Invoke(entity);
                _selectionContext = entity;
            },
            onContextMenu: () =>
            {
                if (ImGui.MenuItem("Delete Entity"))
                    entityDeleted = true;
            },
            flags: ImGuiTreeNodeFlags.OpenOnArrow
        );

        // Prefab drag & drop handling
        _prefabDropTarget.HandleEntityDrop(entity);

        if (opened)
        {
            ImGui.TreePop();
        }

        if (entityDeleted)
        {
            _scene.DestroyEntity(entity);
            if (Equals(_selectionContext, entity))
                _selectionContext = null;
        }
    }

    private void RenderSearchInput()
    {
        LayoutDrawer.DrawSearchInput("Search entities...", ref _searchQuery, ApplyFilter);
    }

    private void RenderFilterStatus()
    {
        var matchCount = CountDirectMatches();
        var totalCount = _scene.Entities.Count();

        var statusText = $"🔍 Filtering: {matchCount} of {totalCount} entities";

        TextDrawer.DrawInfoText(statusText);

        ImGui.Separator();
    }

    private void RenderEntityHierarchy()
    {
        if (_isFilterActive)
        {
            if (_filteredEntities.Count == 0)
            {
                ImGui.TextUnformatted("No entities match your search");
                return;
            }

            foreach (var entity in _filteredEntities)
            {
                DrawEntityNodeFiltered(entity);
            }
        }
        else
        {
            foreach (var entity in _scene?.Entities ?? [])
            {
                DrawEntityNode(entity);
            }
        }
    }

    private void DrawEntityNodeFiltered(Entity entity)
    {
        var isDirectMatch = MatchesFilter(entity, _searchQuery);
        var tag = entity.Name;
        bool isSelected = _selectionContext?.Id == entity.Id;
        bool entityDeleted = false;

        // Highlight matched entities with colored tree node
        bool opened;
        if (isDirectMatch)
        {
            opened = TreeDrawer.DrawColoredTreeNode(
                label: tag,
                color: EditorUIConstants.InfoColor,
                isSelected: isSelected,
                onClicked: () =>
                {
                    EntitySelected.Invoke(entity);
                    _selectionContext = entity;
                },
                onContextMenu: () =>
                {
                    if (ImGui.MenuItem("Delete Entity"))
                        entityDeleted = true;
                },
                flags: ImGuiTreeNodeFlags.OpenOnArrow
            );
        }
        else
        {
            opened = TreeDrawer.DrawSelectableTreeNode(
                label: tag,
                isSelected: isSelected,
                onClicked: () =>
                {
                    EntitySelected.Invoke(entity);
                    _selectionContext = entity;
                },
                onContextMenu: () =>
                {
                    if (ImGui.MenuItem("Delete Entity"))
                        entityDeleted = true;
                },
                flags: ImGuiTreeNodeFlags.OpenOnArrow
            );
        }

        // Prefab drag & drop handling
        _prefabDropTarget.HandleEntityDrop(entity);

        if (opened)
        {
            ImGui.TreePop();
        }

        if (entityDeleted)
        {
            _scene.DestroyEntity(entity);
            if (Equals(_selectionContext, entity))
                _selectionContext = null;
        }
    }

    private void ApplyFilter(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _isFilterActive = false;
            _filteredEntities.Clear();
            return;
        }

        _isFilterActive = true;
        _filteredEntities.Clear();

        var normalizedQuery = query.Trim().ToLowerInvariant();

        foreach (var entity in _scene.Entities)
        {
            if (MatchesFilter(entity, normalizedQuery))
            {
                _filteredEntities.Add(entity);
            }
        }
    }

    private static bool MatchesFilter(Entity entity, string query)
    {
        var entityName = entity.Name.ToLowerInvariant();
        var normalizedQuery = query.ToLowerInvariant();
        return entityName.Contains(normalizedQuery);
    }

    private void ClearFilter()
    {
        _searchQuery = string.Empty;
        _isFilterActive = false;
        _filteredEntities.Clear();
    }

    private int CountDirectMatches()
    {
        var normalizedQuery = _searchQuery.Trim().ToLowerInvariant();
        return _filteredEntities.Count(entity => MatchesFilter(entity, normalizedQuery));
    }
}