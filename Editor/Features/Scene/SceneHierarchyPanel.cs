using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Features.Scene;

public class SceneHierarchyPanel(PrefabDropTarget prefabDropTarget, IEntityContextMenu entityContextMenu)
    : ISceneHierarchyPanel
{
    private const string EntityReparentPayload = "ENTITY_REPARENT";

    private IScene _scene;
    private Entity? _selectionContext;

    // Search/Filter state
    private string _searchQuery = string.Empty;
    private readonly List<Entity> _filteredEntities = [];
    private bool _isFilterActive;

    public Action<Entity> EntitySelected { get; set; } = null!;

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

        if (_scene is not null)
        {
            ImGui.Dummy(ImGui.GetContentRegionAvail());
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(EntityReparentPayload);
                unsafe
                {
                    if (payload.NativePtr != null && payload.DataSize == sizeof(int))
                    {
                        var droppedId = *(int*)payload.Data;
                        var dropped = _scene.Entities.FirstOrDefault(e => e.Id == droppedId);
                        if (dropped is not null)
                            _scene.SetParent(dropped, null);
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered())
            _selectionContext = null;

        entityContextMenu.Render(_scene);

        ImGui.End();
    }

    public void SetSelectedEntity(Entity entity) => _selectionContext = entity;

    public Entity? GetSelectedEntity() => _selectionContext;

    private void DrawEntityNode(Entity entity)
    {
        var tag = entity.Name;
        var isSelected = _selectionContext?.Id == entity.Id;
        var entityDeleted = false;

        var children = _scene.GetChildren(entity).ToList();
        var nodeFlags = children.Count == 0
            ? ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen
            : ImGuiTreeNodeFlags.OpenOnArrow;

        var opened = TreeDrawer.DrawSelectableTreeNode(
            label: tag,
            isSelected: isSelected,
            onClicked: () =>
            {
                EntitySelected.Invoke(entity);
                _selectionContext = entity;
            },
            onContextMenu: () =>
            {
                if (ImGui.MenuItem("Create Empty Child"))
                {
                    var child = _scene.CreateEntity("Empty Entity");
                    child.AddComponent<TransformComponent>();
                    _scene.SetParent(child, entity);
                }

                var canUnparent = entity.TryGetComponent<TransformComponent>(out var tc) && tc.ParentId is not null;
                if (canUnparent && ImGui.MenuItem("Unparent"))
                    _scene.SetParent(entity, null);

                var deleteLabel = children.Count > 0 ? "Delete Entity (and children)" : "Delete Entity";
                if (ImGui.MenuItem(deleteLabel))
                    entityDeleted = true;
            },
            flags: nodeFlags
        );

        prefabDropTarget.HandleEntityDrop(entity);

        if (ImGui.BeginDragDropSource())
        {
            var idCopy = entity.Id;
            unsafe
            {
                ImGui.SetDragDropPayload(EntityReparentPayload, (IntPtr)(&idCopy), sizeof(int));
            }

            ImGui.TextUnformatted(entity.Name);
            ImGui.EndDragDropSource();
        }

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(EntityReparentPayload);
            unsafe
            {
                if (payload.NativePtr != null && payload.DataSize == sizeof(int))
                {
                    var droppedId = *(int*)payload.Data;
                    if (droppedId != entity.Id)
                    {
                        var dropped = _scene.Entities.FirstOrDefault(e => e.Id == droppedId);
                        if (dropped is not null)
                        {
                            try
                            {
                                _scene.SetParent(dropped, entity);
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }
                }
            }

            ImGui.EndDragDropTarget();
        }

        if (opened && children.Count > 0)
        {
            foreach (var child in children)
                DrawEntityNode(child);
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

            foreach (var entity in _filteredEntities.ToList())
            {
                DrawEntityNodeFiltered(entity);
            }
        }
        else
        {
            foreach (var entity in _scene?.GetRootEntities().ToList() ?? [])
            {
                DrawEntityNode(entity);
            }
        }
    }

    private void DrawEntityNodeFiltered(Entity entity)
    {
        var isDirectMatch = MatchesFilter(entity, _searchQuery);
        var tag = entity.Name;
        var isSelected = _selectionContext?.Id == entity.Id;
        var entityDeleted = false;

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
        prefabDropTarget.HandleEntityDrop(entity);

        if (opened)
        {
            ImGui.TreePop();
        }

        if (entityDeleted)
        {
            _scene.DestroyEntity(entity);
            _filteredEntities.Remove(entity);
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