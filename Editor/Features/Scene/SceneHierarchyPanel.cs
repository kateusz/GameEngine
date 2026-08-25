using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Selection;
using Editor.Features.Tiled;
using Editor.Panels;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;

namespace Editor.Features.Scene;

internal readonly record struct HierarchyRow(Entity Entity, int Depth, bool HasChildren);

public class SceneHierarchyPanel(
    PrefabDropTarget prefabDropTarget,
    IEntityContextMenu entityContextMenu,
    IEditorSelection selection,
    IEditorHistory history,
    TiledMapImportService tiledImport)
    : ISceneHierarchyPanel, IEditorPanel
{
    private const string EntityDragPayload = "SCENE_HIERARCHY_ENTITY";

    private IScene _scene = null!;

    private string _searchQuery = string.Empty;
    private readonly HashSet<int> _filterVisibleIds = [];
    private readonly HashSet<int> _filterMatchIds = [];
    private readonly HashSet<int> _expandedIds = [];
    private readonly List<HierarchyRow> _rows = [];
    private bool _isFilterActive;

    public void SetScene(IScene scene)
    {
        _scene = scene;
        _expandedIds.Clear();
        selection.Select(null, SelectionSource.Code);
    }

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Scene Hierarchy");

        LayoutDrawer.DrawSearchInput("Search entities...", ref _searchQuery, ApplyFilter);

        if (_isFilterActive)
            RenderFilterStatus();

        RenderEntityHierarchy();

        // Explicit empty-space target for promote-to-root (not the last tree node).
        // InvisibleButton is an item, so window NoOpenOverItems menu won't fire over it —
        // attach the create-entity menu to the button instead.
        var avail = ImGui.GetContentRegionAvail();
        if (avail.X > 0 && avail.Y > 0)
        {
            ImGui.InvisibleButton("##HierarchyBgDrop", avail);
            entityContextMenu.RenderForLastItem(_scene);
            if (ImGui.BeginDragDropTarget())
            {
                TryAcceptEntityDrop(parent: null);
                TryAcceptTiledMapDrop();
                ImGui.EndDragDropTarget();
            }
        }

        if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered())
            selection.Select(null, SelectionSource.Hierarchy);

        entityContextMenu.Render(_scene);

        ImGui.End();
    }

    private unsafe void RenderEntityHierarchy()
    {
        var roots = _scene.GetRootEntities();
        if (_isFilterActive && _filterVisibleIds.Count == 0)
        {
            ImGui.TextUnformatted("No entities match your search");
            return;
        }

        _rows.Clear();
        var filter = _isFilterActive ? _filterVisibleIds : null;
        foreach (var root in roots)
            CollectVisibleRows(_scene, root, 0, _expandedIds, filter, _rows);

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        try
        {
            clipper.Begin(_rows.Count);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    DrawRow(_rows[i]);
            }
        }
        finally
        {
            clipper.Destroy();
        }
    }

    internal static void CollectVisibleRows(
        IScene scene,
        Entity entity,
        int depth,
        HashSet<int> expandedIds,
        HashSet<int>? filterVisibleIds,
        List<HierarchyRow> dest)
    {
        if (filterVisibleIds is not null && !filterVisibleIds.Contains(entity.Id))
            return;

        var children = scene.GetChildren(entity);
        var hasChildren = filterVisibleIds is null
            ? children.Count > 0
            : children.Any(c => filterVisibleIds.Contains(c.Id));

        dest.Add(new HierarchyRow(entity, depth, hasChildren));

        if (!hasChildren || !expandedIds.Contains(entity.Id))
            return;

        foreach (var child in children)
            CollectVisibleRows(scene, child, depth + 1, expandedIds, filterVisibleIds, dest);
    }

    private void DrawRow(HierarchyRow row)
    {
        var entity = row.Entity;
        if (!_scene.Context.Contains(entity.Id))
            return;

        var isSelected = selection.SelectedEntity?.Id == entity.Id;
        var isMatch = _isFilterActive && _filterMatchIds.Contains(entity.Id);

        var flags = ImGuiTreeNodeFlags.NoTreePushOnOpen
                    | ImGuiTreeNodeFlags.SpanAvailWidth
                    | ImGuiTreeNodeFlags.OpenOnArrow;
        if (!row.HasChildren)
            flags |= ImGuiTreeNodeFlags.Leaf;
        if (isSelected)
            flags |= ImGuiTreeNodeFlags.Selected;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + row.Depth * ImGui.GetTreeNodeToLabelSpacing());
        ImGui.PushID(entity.Id);
        if (row.HasChildren)
            ImGui.SetNextItemOpen(_expandedIds.Contains(entity.Id));

        if (isMatch)
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.InfoColor);

        var opened = ImGui.TreeNodeEx(entity.Name, flags);

        if (isMatch)
            ImGui.PopStyleColor();

        if (row.HasChildren && ImGui.IsItemToggledOpen())
        {
            if (opened)
                _expandedIds.Add(entity.Id);
            else
                _expandedIds.Remove(entity.Id);
        }

        if (ImGui.IsItemClicked())
            selection.Select(entity, SelectionSource.Hierarchy);

        if (ImGui.BeginPopupContextItem())
        {
            if (ImGui.MenuItem("Create Child Entity"))
            {
                var child = _scene.CreateEntity("Empty Entity");
                child.AddComponent<TransformComponent>();
                _scene.SetParent(child, entity);
                _expandedIds.Add(entity.Id);
                selection.Select(child, SelectionSource.Hierarchy);
            }

            if (ImGui.MenuItem("Delete Entity"))
            {
                var deletedId = entity.Id;
                _expandedIds.Remove(deletedId);
                history.Execute(new DestroyEntitySubtreeCommand(_scene, deletedId));
                if (selection.SelectedEntity?.Id == deletedId)
                    selection.Select(null, SelectionSource.Code);
                ApplyFilter(_searchQuery);
            }

            ImGui.EndPopup();
        }

        DragDropDrawer.CreateDragDropSource(
            EntityDragPayload,
            entity.Id.ToString(),
            () => ImGui.TextUnformatted(entity.Name));

        if (ImGui.BeginDragDropTarget())
        {
            TryAcceptEntityDrop(parent: entity);
            TryAcceptTiledMapDrop();
            ImGui.EndDragDropTarget();
        }

        prefabDropTarget.HandleEntityDrop(entity);
        ImGui.PopID();
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

    private unsafe void TryAcceptTiledMapDrop()
    {
        var payload = ImGui.AcceptDragDropPayload(DragDropDrawer.ContentBrowserItemPayload);
        if (payload.NativePtr == null)
            return;

        var path = DragDropDrawer.ExtractStringFromPayload(payload.Data);
        if (path is null || !path.EndsWith(".tmj", StringComparison.OrdinalIgnoreCase))
            return;

        tiledImport.ImportFromContentPath(path);
    }

    private void RenderFilterStatus()
    {
        TextDrawer.DrawInfoText($"Filtering: {_filterMatchIds.Count} of {_scene.Entities.Count()} entities");
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
        var normalizedQuery = query.Trim();

        foreach (var entity in _scene.Entities)
        {
            if (!entity.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                continue;

            _filterMatchIds.Add(entity.Id);
            _expandedIds.Add(entity.Id);
            // Include match + ancestors so nested hits stay visible in context
            for (Entity? current = entity; current is not null; current = _scene.GetParent(current))
            {
                if (!_filterVisibleIds.Add(current.Id))
                    break;
            }
        }
    }
}
