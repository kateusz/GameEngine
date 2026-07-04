using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Features.Scene;

public class SceneHierarchyPanel(PrefabDropTarget prefabDropTarget, IEntityContextMenu entityContextMenu)
    : ISceneHierarchyPanel
{
    private IScene _scene;
    private readonly List<Entity> _selectedEntities = [];

    // Search/Filter state
    private string _searchQuery = string.Empty;
    private readonly List<Entity> _filteredEntities = [];
    private bool _isFilterActive;

    public Action<Entity> EntitySelected { get; set; } = null!;

    public void SetScene(IScene scene)
    {
        _scene = scene;
        _selectedEntities.Clear();
    }

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Scene Hierarchy");

        RenderSearchInput();

        if (_isFilterActive)
            RenderFilterStatus();

        RenderEntityHierarchy();

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered())
            ClearSelection();

        entityContextMenu.Render(_scene);

        ImGui.End();
    }

    public void SetSelectedEntity(Entity entity)
    {
        _selectedEntities.Clear();
        _selectedEntities.Add(entity);
    }

    public Entity? GetSelectedEntity() => _selectedEntities.Count > 0 ? _selectedEntities[^1] : null;

    public IReadOnlyList<Entity> GetSelectedEntities() => _selectedEntities;

    public void ClearSelection() => _selectedEntities.Clear();

    private bool IsSelected(Entity entity) => _selectedEntities.Any(e => e.Id == entity.Id);

    private void HandleEntityClick(Entity entity)
    {
        if (IsCtrlHeld())
        {
            var index = _selectedEntities.FindIndex(e => e.Id == entity.Id);
            if (index >= 0)
                _selectedEntities.RemoveAt(index);
            else
                _selectedEntities.Add(entity);
            return;
        }

        SelectOnly(entity);
        EntitySelected.Invoke(entity);
    }

    private static bool IsCtrlHeld() =>
        ImGui.GetIO().KeyCtrl || ImGui.IsKeyDown(ImGuiKey.ModCtrl);

    private void SelectOnly(Entity entity)
    {
        _selectedEntities.Clear();
        _selectedEntities.Add(entity);
    }

    private void RemoveFromSelection(Entity entity) =>
        _selectedEntities.RemoveAll(e => e.Id == entity.Id);

    private void DrawEntityNode(Entity entity)
    {
        var tag = entity.Name;
        var isSelected = IsSelected(entity);
        var entityDeleted = false;

        var opened = TreeDrawer.DrawSelectableTreeNode(
            label: tag,
            isSelected: isSelected,
            onClicked: () => HandleEntityClick(entity),
            onContextMenu: () =>
            {
                if (ImGui.MenuItem("Delete Entity"))
                    entityDeleted = true;
            },
            flags: ImGuiTreeNodeFlags.OpenOnArrow
        );

        prefabDropTarget.HandleEntityDrop(entity);

        if (opened)
            ImGui.TreePop();

        if (entityDeleted)
        {
            _scene.DestroyEntity(entity);
            RemoveFromSelection(entity);
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
                DrawEntityNodeFiltered(entity);
        }
        else
        {
            foreach (var entity in _scene?.Entities.ToList() ?? [])
                DrawEntityNode(entity);
        }
    }

    private void DrawEntityNodeFiltered(Entity entity)
    {
        var isDirectMatch = MatchesFilter(entity, _searchQuery);
        var tag = entity.Name;
        var isSelected = IsSelected(entity);
        var entityDeleted = false;

        bool opened;
        if (isDirectMatch)
        {
            opened = TreeDrawer.DrawColoredTreeNode(
                label: tag,
                color: EditorUIConstants.InfoColor,
                isSelected: isSelected,
                onClicked: () => HandleEntityClick(entity),
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
                onClicked: () => HandleEntityClick(entity),
                onContextMenu: () =>
                {
                    if (ImGui.MenuItem("Delete Entity"))
                        entityDeleted = true;
                },
                flags: ImGuiTreeNodeFlags.OpenOnArrow
            );
        }

        prefabDropTarget.HandleEntityDrop(entity);

        if (opened)
            ImGui.TreePop();

        if (entityDeleted)
        {
            _scene.DestroyEntity(entity);
            _filteredEntities.Remove(entity);
            RemoveFromSelection(entity);
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
                _filteredEntities.Add(entity);
        }
    }

    private static bool MatchesFilter(Entity entity, string query)
    {
        var entityName = entity.Name.ToLowerInvariant();
        var normalizedQuery = query.ToLowerInvariant();
        return entityName.Contains(normalizedQuery);
    }

    private int CountDirectMatches()
    {
        var normalizedQuery = _searchQuery.Trim().ToLowerInvariant();
        return _filteredEntities.Count(entity => MatchesFilter(entity, normalizedQuery));
    }
}
