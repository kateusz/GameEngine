using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Panels;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;
using SceneComponents.Audio;
using SceneComponents.Camera;
using SceneComponents.Lighting;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace Editor.Features.Scene;

internal readonly record struct HierarchyRow(Entity Entity, int Depth, bool HasChildren);

public class SceneHierarchyPanel(
    PrefabDropTarget prefabDropTarget,
    IEntityContextMenu entityContextMenu,
    IEditorSelection selection,
    IEditorHistory history,
    IEditorPreferences editorPreferences,
    ISceneManager sceneManager)
    : IEditorPanel
{
    private const string EntityDragPayload = "SCENE_HIERARCHY_ENTITY";
    private const string ComponentFilterPopupId = "##HierarchyComponentFilter";

    // Same built-ins as ComponentSelector — keep in sync when adding component types.
    private static readonly (Type Type, string Name)[] FilterableComponents =
    [
        (typeof(CameraComponent), "Camera"),
        (typeof(TransformComponent), "Transform"),
        (typeof(SpriteRendererComponent), "Sprite Renderer"),
        (typeof(SubTextureRendererComponent), "Sub Texture Renderer"),
        (typeof(RigidBody2DComponent), "Rigidbody 2D"),
        (typeof(BoxCollider2DComponent), "Box Collider 2D"),
        (typeof(ModelRendererComponent), "Model Renderer"),
        (typeof(CircleCollider2DComponent), "Circle Collider 2D"),
        (typeof(EdgeCollider2DComponent), "Edge Collider 2D"),
        (typeof(AudioSourceComponent), "Audio Source"),
        (typeof(AudioListenerComponent), "Audio Listener"),
        (typeof(AmbientLightComponent), "Ambient Light"),
        (typeof(DirectionalLightComponent), "Directional Light"),
        (typeof(PointLightComponent), "Point Light"),
        (typeof(VisibilityZoneComponent), "Visibility Zone"),
    ];

    private IScene _scene = null!;

    private string _searchQuery = string.Empty;
    private readonly HashSet<int> _filterVisibleIds = [];
    private readonly HashSet<int> _filterMatchIds = [];
    private readonly HashSet<int> _expandedIds = [];
    private readonly List<HierarchyRow> _rows = [];
    private readonly HashSet<Type> _selectedComponentTypes = [];
    private bool _isFilterActive;
    private int? _scrollToEntityId;

    public void Initialize() => selection.SelectionChanged += OnSelectionChanged;

    public void SetScene(IScene scene)
    {
        _scene = scene;
        _expandedIds.Clear();
        _scrollToEntityId = null;
        selection.Select(null, SelectionSource.Code);
    }

    public void RequestScrollToEntity(int entityId)
    {
        if (!_scene.Context.Contains(entityId))
            return;

        var entity = _scene.Context.GetById(entityId);
        for (var current = _scene.GetParent(entity); current is not null; current = _scene.GetParent(current))
            _expandedIds.Add(current.Id);
        _scrollToEntityId = entityId;
    }

    private void OnSelectionChanged(Entity? entity, SelectionSource source)
    {
        if (!editorPreferences.FollowViewportSelectionInHierarchy
            || source != SelectionSource.Viewport
            || entity is null
            || !_scene.Context.Contains(entity.Id))
            return;

        RequestScrollToEntity(entity.Id);
    }

    private void DrawCurrentSceneLabel()
    {
        var path = sceneManager.GetCurrentScenePath();
        var label = path is not null
            ? Path.GetFileNameWithoutExtension(path)
            : string.IsNullOrWhiteSpace(_scene.Name) ? "Untitled" : _scene.Name;

        ImGui.TextUnformatted($"Scene: {label}");
        if (path is not null && ImGui.IsItemHovered())
            ImGui.SetTooltip(path);

        ImGui.Spacing();
    }

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Scene Hierarchy");

        DrawCurrentSceneLabel();

        if (ButtonDrawer.DrawSmallButton("+", tooltip: "Add Entity"))
            ImGui.OpenPopup(EntityContextMenu.CreateEntityPopupId);

        ImGui.SameLine();
        LayoutDrawer.DrawSearchInput(
            "Search entities...",
            ref _searchQuery,
            ApplyFilter,
            trailingReservedWidth: EditorUIConstants.SmallButtonSize + EditorUIConstants.SmallPadding);

        ImGui.SameLine();
        if (ButtonDrawer.DrawSmallButton("▼", tooltip: "Filter by component"))
            ImGui.OpenPopup(ComponentFilterPopupId);

        entityContextMenu.RenderCreatePopup(_scene);
        DrawComponentFilterPopup();

        if (_isFilterActive)
            RenderFilterStatus();

        ImGui.BeginChild("HierarchyScroll", Vector2.Zero);

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
                ImGui.EndDragDropTarget();
            }
        }

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered())
            selection.Select(null, SelectionSource.Hierarchy);

        ImGui.EndChild();

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

        ApplyPendingHierarchyScroll();

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

    private void ApplyPendingHierarchyScroll()
    {
        if (_scrollToEntityId is not int entityId)
            return;

        var index = _rows.FindIndex(r => r.Entity.Id == entityId);
        if (index < 0)
        {
            _scrollToEntityId = null;
            return;
        }

        var itemHeight = ImGui.GetTextLineHeightWithSpacing();
        var localY = ImGui.GetCursorStartPos().Y + index * itemHeight;
        ImGui.SetScrollFromPosY(localY, 0.25f);
        _scrollToEntityId = null;
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

        var isSelected = selection.SelectedEntities.Any(e => e.Id == entity.Id);
        var isMatch = _isFilterActive && _filterMatchIds.Contains(entity.Id);
        entity.TryGetComponent<TransformComponent>(out var transform);
        var effectivelyHidden = transform is not null && !transform.EffectiveVisible;

        // No SpanAvailWidth — full-width tree steals clicks from the eye on the right.
        var flags = ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.OpenOnArrow;
        if (!row.HasChildren)
            flags |= ImGuiTreeNodeFlags.Leaf;
        if (isSelected)
            flags |= ImGuiTreeNodeFlags.Selected;

        var depthPad = row.Depth * ImGui.GetTreeNodeToLabelSpacing();
        var lineY = ImGui.GetCursorPosY();
        var lineStartX = ImGui.GetCursorPosX() + depthPad;
        var eyeW = EditorUIConstants.SmallButtonSize;

        ImGui.PushID(entity.Id);
        if (row.HasChildren)
            ImGui.SetNextItemOpen(_expandedIds.Contains(entity.Id));

        var eyeClicked = false;
        if (transform is not null)
        {
            ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - eyeW, lineY));
            eyeClicked = DrawVisibilityEye(entity, transform);
        }

        ImGui.SetCursorPos(new Vector2(lineStartX, lineY));

        if (isSelected)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, EditorUIConstants.HierarchyRowSelectedBackground);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, EditorUIConstants.HierarchyRowSelectedBackground);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, EditorUIConstants.HierarchyRowSelectedBackground);
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.HierarchyRowSelectedText);
        }
        else if (isMatch || effectivelyHidden)
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.InfoColor);

        var opened = ImGui.TreeNodeEx(entity.Name, flags);
        var rowMin = ImGui.GetItemRectMin();
        var rowMax = ImGui.GetItemRectMax();
        var toggledOpen = row.HasChildren && ImGui.IsItemToggledOpen();

        if (isSelected)
        {
            var drawList = ImGui.GetWindowDrawList();
            var accent = ImGui.ColorConvertFloat4ToU32(EditorUIConstants.HierarchyRowSelectedAccent);
            drawList.AddRectFilled(rowMin, new Vector2(rowMin.X + 3f, rowMax.Y), accent);
            ImGui.PopStyleColor(4);
        }
        else if (isMatch || effectivelyHidden)
            ImGui.PopStyleColor();

        if (toggledOpen)
        {
            if (opened)
                _expandedIds.Add(entity.Id);
            else
                _expandedIds.Remove(entity.Id);
        }

        if (!eyeClicked && (toggledOpen || PointerActivatedInRect(rowMin, rowMax)))
        {
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                selection.Select(entity, SelectionSource.Hierarchy);
            else if (ImGui.GetIO().KeyShift)
                selection.SelectRange(_rows.ConvertAll(r => r.Entity), entity);
            else if (ImGui.GetIO().KeyCtrl)
                selection.Toggle(entity);
            else
                selection.Select(entity, SelectionSource.Hierarchy);
        }

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

            if (ImGui.MenuItem("Delete Entity", "Del"))
            {
                var deletedId = entity.Id;
                _expandedIds.Remove(deletedId);
                history.Execute(new DestroyEntitySubtreeCommand(_scene, deletedId));
                ApplyFilter(_searchQuery);
            }

            ImGui.EndPopup();
        }

        DragDropDrawer.CreateDragDropSource(
            EntityDragPayload,
            entity.Id.ToString(),
            () =>
            {
                var count = isSelected ? selection.SelectedEntities.Count : 1;
                ImGui.TextUnformatted(count > 1 ? $"{count} entities" : entity.Name);
            });

        if (ImGui.BeginDragDropTarget())
        {
            TryAcceptEntityDrop(parent: entity);
            ImGui.EndDragDropTarget();
        }

        prefabDropTarget.HandleEntityDrop(entity);
        ImGui.PopID();
    }

    private bool DrawVisibilityEye(Entity entity, TransformComponent transform)
    {
        // ponytail: text stand-in for an eye icon; swap if an icon font lands
        var clicked = ImGui.Button(
            transform.Visible ? "O##vis" : "-##vis",
            new Vector2(EditorUIConstants.SmallButtonSize, EditorUIConstants.SmallButtonSize));
        if (!clicked)
            return false;

        _scene.SetSubtreeVisible(entity, !transform.Visible);
        return true;
    }

    private static bool PointerActivatedInRect(Vector2 min, Vector2 max)
    {
        var mouse = ImGui.GetMousePos();
        if (mouse.X < min.X || mouse.X > max.X || mouse.Y < min.Y || mouse.Y > max.Y)
            return false;

        // One activation per click (release only). Mousedown + mouseup both firing ran Toggle twice.
        if (!ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            return false;

        return ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() < 4f;
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
        var toMove = ResolveEntitiesToReparent(_scene, dragged, selection.SelectedEntities);
        var moved = false;
        foreach (var entity in toMove)
        {
            if (_scene.SetParent(entity, parent))
                moved = true;
        }

        if (!moved)
            return; // cycle or invalid — silent reject (ImGui shows no drop)

        if (_isFilterActive)
            ApplyFilter(_searchQuery);
    }

    internal static List<Entity> ResolveEntitiesToReparent(
        IScene scene,
        Entity dragged,
        IReadOnlyList<Entity> selected)
    {
        if (selected.Count <= 1 || selected.All(e => e.Id != dragged.Id))
            return [dragged];

        var selectedIds = selected.Select(e => e.Id).ToHashSet();
        return selected
            .Where(e => scene.Context.Contains(e.Id))
            .Where(e =>
            {
                var parent = scene.GetParent(e);
                return parent is null || !selectedIds.Contains(parent.Id);
            })
            .ToList();
    }

    private void DrawComponentFilterPopup()
    {
        if (!ImGui.BeginPopup(ComponentFilterPopupId))
            return;

        foreach (var (type, name) in FilterableComponents)
        {
            var selected = _selectedComponentTypes.Contains(type);
            if (!ImGui.Checkbox(name, ref selected))
                continue;

            if (selected)
                _selectedComponentTypes.Add(type);
            else
                _selectedComponentTypes.Remove(type);

            ApplyFilter(_searchQuery);
        }

        ImGui.EndPopup();
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

        var hasSearch = !string.IsNullOrWhiteSpace(query);
        var hasComponentFilter = _selectedComponentTypes.Count > 0;

        if (!hasSearch && !hasComponentFilter)
        {
            _isFilterActive = false;
            return;
        }

        _isFilterActive = true;
        var normalizedQuery = hasSearch ? query.Trim() : null;

        foreach (var entity in _scene.Entities)
        {
            if (hasComponentFilter && !EntityPassesComponentFilter(entity, _selectedComponentTypes))
                continue;

            if (normalizedQuery is not null
                && !entity.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
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

    internal static bool EntityPassesComponentFilter(Entity entity, IReadOnlySet<Type> selectedTypes) =>
        entity.GetAllComponents().Any(c => selectedTypes.Contains(c.GetType()));
}
