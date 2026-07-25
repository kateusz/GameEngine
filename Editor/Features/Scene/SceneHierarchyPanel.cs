using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Selection;
using Editor.Panels;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;

namespace Editor.Features.Scene;

public class SceneHierarchyPanel(
    PrefabDropTarget prefabDropTarget,
    IEntityContextMenu entityContextMenu,
    IEditorSelection selection,
    IEditorHistory history)
    : ISceneHierarchyPanel, IEditorPanel
{
    private const string EntityDragPayload = "SCENE_HIERARCHY_ENTITY";

    private IScene _scene = null!;

    private string _searchQuery = string.Empty;
    private readonly HashSet<int> _filterVisibleIds = [];
    private readonly HashSet<int> _filterMatchIds = [];
    private bool _isFilterActive;

    public void SetScene(IScene scene)
    {
        _scene = scene;
        selection.Select(null, SelectionSource.Code);
    }

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Scene Hierarchy");

        RenderSearchInput();

        if (_isFilterActive)
            RenderFilterStatus();

        RenderEntityHierarchy();

        // Drop on empty panel background → promote to root
        if (ImGui.BeginDragDropTarget())
        {
            TryAcceptEntityDrop(parent: null);
            ImGui.EndDragDropTarget();
        }

        if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered())
            selection.Select(null, SelectionSource.Hierarchy);

        entityContextMenu.Render(_scene);

        ImGui.End();
    }

    private void RenderEntityHierarchy()
    {
        var roots = _scene?.GetRootEntities().ToList() ?? [];
        if (_isFilterActive)
        {
            if (_filterVisibleIds.Count == 0)
            {
                ImGui.TextUnformatted("No entities match your search");
                return;
            }

            foreach (var root in roots)
            {
                if (_filterVisibleIds.Contains(root.Id))
                    DrawEntityNode(root, filtered: true);
            }

            return;
        }

        foreach (var root in roots)
            DrawEntityNode(root, filtered: false);
    }

    private void DrawEntityNode(Entity entity, bool filtered)
    {
        var children = _scene.GetChildren(entity)
            .Where(c => !filtered || _filterVisibleIds.Contains(c.Id))
            .ToList();

        var isSelected = selection.SelectedEntity?.Id == entity.Id;
        var isMatch = filtered && _filterMatchIds.Contains(entity.Id);
        var entityDeleted = false;
        var createChild = false;

        var flags = children.Count == 0
            ? ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen
            : ImGuiTreeNodeFlags.OpenOnArrow;

        if (isMatch)
            flags |= ImGuiTreeNodeFlags.DefaultOpen;

        Action onContextMenu = () =>
        {
            if (ImGui.MenuItem("Create Child Entity"))
                createChild = true;
            if (ImGui.MenuItem("Delete Entity"))
                entityDeleted = true;
        };

        var opened = isMatch
            ? TreeDrawer.DrawColoredTreeNode(
                label: entity.Name,
                color: EditorUIConstants.InfoColor,
                isSelected: isSelected,
                onClicked: () => selection.Select(entity, SelectionSource.Hierarchy),
                onContextMenu: onContextMenu,
                flags: flags)
            : TreeDrawer.DrawSelectableTreeNode(
                label: entity.Name,
                isSelected: isSelected,
                onClicked: () => selection.Select(entity, SelectionSource.Hierarchy),
                onContextMenu: onContextMenu,
                flags: flags);

        DragDropDrawer.CreateDragDropSource(EntityDragPayload, entity.Id.ToString(), () => ImGui.TextUnformatted(entity.Name));

        if (ImGui.BeginDragDropTarget())
        {
            TryAcceptEntityDrop(parent: entity);
            ImGui.EndDragDropTarget();
        }

        prefabDropTarget.HandleEntityDrop(entity);

        if (opened && children.Count > 0)
        {
            foreach (var child in children)
                DrawEntityNode(child, filtered);
            ImGui.TreePop();
        }

        if (createChild)
        {
            var child = _scene.CreateEntity("Empty Entity");
            child.AddComponent<TransformComponent>();
            _scene.SetParent(child, entity);
            selection.Select(child, SelectionSource.Hierarchy);
        }

        if (entityDeleted)
        {
            var deletedId = entity.Id;
            history.Execute(new DestroyEntitySubtreeCommand(_scene, deletedId));
            if (selection.SelectedEntity?.Id == deletedId)
                selection.Select(null, SelectionSource.Code);
            ApplyFilter(_searchQuery);
        }
    }

    private unsafe void TryAcceptEntityDrop(Entity? parent)
    {
        var payload = ImGui.AcceptDragDropPayload(EntityDragPayload);
        if (payload.NativePtr == null)
            return;

        var idText = DragDropDrawer.ExtractStringFromPayload(payload.Data);
        if (idText is null || !int.TryParse(idText, out var draggedId))
            return;

        if (!_scene.Context.Contains(draggedId))
            return;

        var dragged = _scene.Context.GetById(draggedId);
        if (!_scene.SetParent(dragged, parent))
            return; // cycle or invalid — silent reject (ImGui shows no drop)

        if (_isFilterActive)
            ApplyFilter(_searchQuery);
    }

    private void RenderSearchInput()
    {
        LayoutDrawer.DrawSearchInput("Search entities...", ref _searchQuery, ApplyFilter);
    }

    private void RenderFilterStatus()
    {
        var totalCount = _scene.Entities.Count();
        var statusText = $"Filtering: {_filterMatchIds.Count} of {totalCount} entities";
        TextDrawer.DrawInfoText(statusText);
        ImGui.Separator();
    }

    private void ApplyFilter(string query)
    {
        _filterVisibleIds.Clear();
        _filterMatchIds.Clear();

        if (string.IsNullOrWhiteSpace(query))
        {
            _isFilterActive = false;
            return;
        }

        _isFilterActive = true;
        var normalizedQuery = query.Trim().ToLowerInvariant();

        foreach (var entity in _scene.Entities)
        {
            if (!MatchesFilter(entity, normalizedQuery))
                continue;

            _filterMatchIds.Add(entity.Id);
            // Include match + ancestors so nested hits stay visible in context
            for (Entity? current = entity; current is not null; current = _scene.GetParent(current))
                _filterVisibleIds.Add(current.Id);
        }
    }

    private static bool MatchesFilter(Entity entity, string query)
    {
        return entity.Name.ToLowerInvariant().Contains(query);
    }
}
