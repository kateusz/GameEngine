using System.Numerics;
using ECS;
using Editor.Panels.Elements;
using Editor.UI;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class SceneHierarchyPanel : ISceneHierarchyPanel
{
    private readonly EntityContextMenu _contextMenu;
    private readonly PrefabDropTarget _prefabDropTarget;

    private IScene _context = null!;
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

    public void SetContext(IScene context)
    {
        _context = context;
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

        _contextMenu.Render(_context);

        ImGui.End();
    }

    public void SetSelectedEntity(Entity entity) => _selectionContext = entity;

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

    private void RenderSearchInput()
    {
        var contentWidth = ImGui.GetContentRegionAvail().X;
        var inputWidth = contentWidth;

        // Calculate width for clear button if search query is not empty
        if (!string.IsNullOrEmpty(_searchQuery))
        {
            inputWidth = contentWidth - EditorUIConstants.SmallButtonSize - EditorUIConstants.SmallPadding;
        }

        ImGui.SetNextItemWidth(inputWidth);

        if (ImGui.InputTextWithHint("##searchInput", "Search entities...", ref _searchQuery,
                EditorUIConstants.MaxNameLength))
        {
            ApplyFilter(_searchQuery);
        }

        // Clear button
        if (!string.IsNullOrEmpty(_searchQuery))
        {
            ImGui.SameLine();
            if (ImGui.Button("×", new Vector2(EditorUIConstants.SmallButtonSize, EditorUIConstants.SmallButtonSize)))
            {
                ClearFilter();
            }
        }
    }

    private void RenderFilterStatus()
    {
        var matchCount = CountDirectMatches();
        var totalCount = _context.Entities.Count();

        var statusText = $"🔍 Filtering: {matchCount} of {totalCount} entities";

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.InfoColor);
        ImGui.TextUnformatted(statusText);
        ImGui.PopStyleColor();

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
            foreach (var entity in _context.Entities)
            {
                DrawEntityNode(entity);
            }
        }
    }

    private void DrawEntityNodeFiltered(Entity entity)
    {
        var isDirectMatch = MatchesFilter(entity, _searchQuery);

        // Highlight matched entities
        if (isDirectMatch)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.InfoColor);
        }

        DrawEntityNode(entity);

        if (isDirectMatch)
        {
            ImGui.PopStyleColor();
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

        foreach (var entity in _context.Entities)
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