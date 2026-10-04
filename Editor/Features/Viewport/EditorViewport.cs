using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.Models;
using Editor.Features.History.Commands;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.UI.Elements;
using Editor.Features.Viewport.Gizmos;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Physics;
using Engine.Project;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Cameras;
using ImGuiNET;
using Input;
using Math;
using SceneComponents.Camera;
using Ui.ImGui;

namespace Editor.Features.Viewport;

public sealed class EditorViewport(
    ISceneContext sceneContext,
    EditorSceneLoadService sceneLoadService,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    DebugSettings debugSettings,
    IFrameBufferFactory frameBufferFactory,
    IContentScaleProvider contentScaleProvider,
    IEditorSelection selection,
    IEditorCameraController cameraController,
    IEditorCameraFraming cameraFraming,
    ViewportComponents viewport,
    IPointerSurface pointerSurface,
    CameraGizmoDrawer cameraGizmoDrawer,
    IModelFactory modelFactory,
    EditorModelLoadService modelLoadService,
    IEditorHistory history,
    FxaaPass fxaaPass,
    SelectionOutlinePass selectionOutlinePass,
    IEditorPreferences editorPreferences)
    : IEditorViewport
{
    private readonly Vector2[] _viewportBounds = new Vector2[2];

    private EditorCamera _editorCamera = null!;
    private IFrameBuffer _frameBuffer = null!;
    private float _contentScale = 1.0f;
    private Vector2 _lastPickMousePos = new(float.NaN);
    private Vector2 _viewportSize;
    private readonly Dictionary<int, Entity> _entityById = [];
    private readonly HashSet<int> _pressedMouseButtons = [];
    private readonly HashSet<KeyCodes> _pressedKeys = [];
    private bool _disposed;
    private float _modelLoadSpinnerRotation;

    private Action<IScene> _sceneChangedHandler = null!;

    public EditorCamera Camera => _editorCamera;
    public Entity? HoveredEntity { get; private set; }
    public bool IsHovered { get; private set; }

    public void Initialize()
    {
        _sceneChangedHandler = RebuildEntityLookup;
        sceneContext.SceneChanged += _sceneChangedHandler;

        _editorCamera = new EditorCamera();
        cameraController.SetCamera(_editorCamera);
        cameraFraming.SetCamera(_editorCamera);
        _frameBuffer = frameBufferFactory.Create();
        _contentScale = contentScaleProvider.ContentScale;

        if (sceneContext.ActiveScene is not null)
            RebuildEntityLookup(sceneContext.ActiveScene);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        sceneContext.SceneChanged -= _sceneChangedHandler;
        _frameBuffer?.Dispose();
    }

    public void LayoutAndRender(TimeSpan deltaTime)
    {
        modelLoadService.Pump();
        ImGui.Begin("Viewport");

        IsHovered = ImGui.IsWindowHovered();

        var viewportPanelSize = ImGui.GetContentRegionAvail();

        _viewportBounds[0] = ImGui.GetCursorScreenPos();
        _viewportBounds[1] = _viewportBounds[0] + viewportPanelSize;
        _viewportSize = viewportPanelSize;

        if (sceneContext.State == SceneState.Play)
            pointerSurface.Set(_viewportBounds[0], _viewportSize);

        ResizeFramebufferIfNeeded();
        RenderSceneToFramebuffer(deltaTime);

        var display = editorPreferences.Fxaa ? fxaaPass.Resolve(_frameBuffer) : _frameBuffer;
        var selected = selection.SelectedEntities;
        // Play mode keeps selection for hierarchy/properties, but no edit outline overlay
        if (selected.Count > 0 && sceneContext.State == SceneState.Edit)
        {
            var ids = selected.Count <= 64 ? stackalloc int[selected.Count] : new int[selected.Count];
            for (var i = 0; i < selected.Count; i++)
                ids[i] = selected[i].Id;
            display = selectionOutlinePass.Resolve(display, _frameBuffer, ids);
        }
        var texturePointer = ImGuiNativeTexture.FromColorAttachment(display);
        ImGui.Image(texturePointer, viewportPanelSize, new Vector2(0, 1), new Vector2(1, 0));

        _viewportBounds[0] = ImGui.GetItemRectMin();
        _viewportBounds[1] = ImGui.GetItemRectMax();
        _viewportSize = _viewportBounds[1] - _viewportBounds[0];

        PickHoveredEntity();

        DragDropDrawer.HandleFileDropTarget(
            DragDropDrawer.ContentBrowserItemPayload,
            IsSceneOrModelDrop,
            OnSceneOrModelDropped);

        if (ImGui.IsWindowHovered())
            HandleViewportInput();

        UpdateFly(deltaTime);

        DrawOverlays();

        ImGui.End();
    }

    private static bool IsSceneOrModelDrop(string path) =>
        DragDropDrawer.HasValidExtension(path, ".scene") || ModelDropTarget.IsSupported(path);

    private void OnSceneOrModelDropped(string path)
    {
        if (DragDropDrawer.HasValidExtension(path, ".scene"))
        {
            sceneLoadService.Request(PathBuilder.Resolve(path));
            return;
        }

        SpawnDroppedModel(path);
    }

    private void SpawnDroppedModel(string path)
    {
        if (sceneContext.State != SceneState.Edit || sceneContext.ActiveScene is not { } scene)
            return;

        var resolved = PathBuilder.Resolve(path);
        if (!ModelDropTarget.IsSupported(path) || !File.Exists(resolved))
            return;

        var relative = PathBuilder.ToAssetRelativePath(path);
        var displayName = Path.GetFileNameWithoutExtension(path);
        modelLoadService.Request(resolved, model =>
        {
            if (model == null)
                return;

            var command = new SpawnModelEntityCommand(scene, displayName, model, relative);
            history.Execute(command);

            if (command.EntityId is int id && scene.Context.Contains(id))
                selection.Select(scene.Context.GetById(id), SelectionSource.Viewport);
        });
    }

    public void HandleWindowInput(InputEvent windowEvent)
    {
        if (sceneContext.State != SceneState.Edit)
            return;

        switch (windowEvent)
        {
            case KeyPressedEvent kpe:
                _pressedKeys.Add(kpe.KeyCode);
                break;
            case KeyReleasedEvent kre:
                _pressedKeys.Remove(kre.KeyCode);
                break;
            case MouseButtonPressedEvent mbpe:
                _pressedMouseButtons.Add(mbpe.Button);
                break;
            case MouseButtonReleasedEvent mbre:
                _pressedMouseButtons.Remove(mbre.Button);
                break;
        }

        if (!IsHovered)
            return;

        var leftDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Left);
        var middleDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Middle);
        var rightDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Right);
        var alt = ImGui.GetIO().KeyAlt;

        if (windowEvent is MouseScrolledEvent scrollEvent)
        {
            if (rightDown && !alt)
                _editorCamera.AdjustFlySpeed(scrollEvent.YOffset);
            else
                _editorCamera.OnMouseScroll(scrollEvent.YOffset);
        }

        if (windowEvent is MouseButtonPressedEvent)
            _editorCamera.SetPreviousMousePosition(GetMousePosition());
        else if (windowEvent is MouseMovedEvent moveEvent)
        {
            var currentPos = new Vector2(moveEvent.X, moveEvent.Y);
            var delta = (currentPos - _editorCamera.GetPreviousMousePosition()) * CameraConfig.EditorMouseSensitivity;

            if (alt && (leftDown || middleDown || rightDown))
            {
                _editorCamera.OnMouseMove(currentPos, pan: middleDown, orbit: leftDown, zoomDrag: rightDown);
            }
            else if (leftDown && rightDown)
            {
                _editorCamera.Slide(delta);
                _editorCamera.SetPreviousMousePosition(currentPos);
            }
            else if (rightDown)
            {
                _editorCamera.Look(delta);
                _editorCamera.SetPreviousMousePosition(currentPos);
            }
            else if (middleDown)
            {
                _editorCamera.Pan(delta);
                _editorCamera.SetPreviousMousePosition(currentPos);
            }
        }
    }

    public void DrawOverlays()
    {
        var focalPoint = _editorCamera.FocalPoint;
        var cameraPos = new Vector2(focalPoint.X, focalPoint.Y);
        var distance = _editorCamera.Distance;
        var fovRad = MathHelpers.DegreesToRadians(_editorCamera.FOV);
        var worldHeight = 2.0f * distance * MathF.Tan(fovRad * 0.5f);
        var zoom = _viewportSize.Y / worldHeight;

        if (sceneContext.ActiveScene?.Dimension == SceneDimension.TwoD)
            viewport.ViewportGrid.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);

        viewport.ViewportRuler.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);
        viewport.ViewportToolManager.RenderActiveTool(_viewportBounds, _editorCamera);
        DrawModelLoadToast();
    }

    private void DrawModelLoadToast()
    {
        if (!modelLoadService.IsBusy)
            return;

        var name = modelLoadService.BusyName;
        var text = string.IsNullOrEmpty(name) ? "Loading 3D model..." : $"Loading {name}...";
        var drawList = ImGui.GetWindowDrawList();
        var center = (_viewportBounds[0] + _viewportBounds[1]) * 0.5f;

        const float radius = 8.0f;
        const float gap = 8.0f;
        const float padX = 12.0f;
        const float padY = 8.0f;
        var textSize = ImGui.CalcTextSize(text);
        var contentH = MathF.Max(radius * 2.0f, textSize.Y);
        var contentW = radius * 2.0f + gap + textSize.X;
        var boxMin = new Vector2(center.X - contentW * 0.5f - padX, center.Y - contentH * 0.5f - padY);
        var boxMax = new Vector2(center.X + contentW * 0.5f + padX, center.Y + contentH * 0.5f + padY);
        drawList.AddRectFilled(boxMin, boxMax, ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.75f)), 6.0f);

        var spinnerCenter = new Vector2(boxMin.X + padX + radius, center.Y);
        LoadingOverlayDrawer.DrawSpinner(drawList, ref _modelLoadSpinnerRotation, spinnerCenter, radius, 2.5f);

        drawList.AddText(
            new Vector2(spinnerCenter.X + radius + gap, center.Y - textSize.Y * 0.5f),
            ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
            text);
    }

    private void ResizeFramebufferIfNeeded()
    {
        var spec = _frameBuffer.GetSpecification();
        var fbWidth = (uint)(_viewportSize.X * _contentScale);
        var fbHeight = (uint)(_viewportSize.Y * _contentScale);
        if (_viewportSize is not { X: > 0.0f, Y: > 0.0f } ||
            (spec.Width == fbWidth && spec.Height == fbHeight))
            return;

        _frameBuffer.Resize(fbWidth, fbHeight);
        _editorCamera.SetViewportSize(_viewportSize.X, _viewportSize.Y);
        sceneContext.ActiveScene?.OnViewportResize(fbWidth, fbHeight);
    }

    private void RenderSceneToFramebuffer(TimeSpan deltaTime)
    {
        graphics2D.ResetStats();
        graphics3D.ResetStats();
        _frameBuffer.Bind();

        var clearColor = sceneContext.ActiveScene?.BackgroundColor ?? Vector4.One;
        graphics2D.SetClearColor(clearColor);
        graphics2D.Clear();
        _frameBuffer.ClearAttachment(1, -1);

        switch (sceneContext.State)
        {
            case SceneState.Edit:
                if (sceneContext.ActiveScene is { } scene)
                {
                    scene.UpdateWorldTransforms();
                    var view = new SceneView(
                        _editorCamera.GetViewProjectionMatrix(),
                        _editorCamera.GetPosition(),
                        PointShadows: false);
                    SceneRenderPipeline.RenderScene(scene.Context, graphics2D, graphics3D, textureFactory, modelFactory, view);
                    RenderEditor2DOverlays(scene.Context, view);
                }
                break;
            case SceneState.Play:
                sceneContext.ActiveScene?.OnUpdateRuntime(deltaTime);
                break;
        }

        _frameBuffer.Unbind();
    }

    private void RenderEditor2DOverlays(Context context, in SceneView view)
    {
        var drawColliders = debugSettings.ShowColliderBounds && sceneContext.ActivePhysicsBodyStore is not null;
        var drawGrid3D = viewport.ViewportGrid.Enabled && sceneContext.ActiveScene?.Dimension == SceneDimension.ThreeD;
        var drawCameraGizmos = HasCameraEntities(context);
        if (!drawColliders && !drawGrid3D && !drawCameraGizmos)
            return;

        graphics2D.BeginScene(view);

        if (drawColliders)
            PhysicsDebugDrawer.DrawColliders(context, graphics2D, sceneContext.ActivePhysicsBodyStore!, useTransformFallbackWhenNoBody: true);

        if (drawCameraGizmos)
            cameraGizmoDrawer.Draw(context, graphics2D, _editorCamera);

        if (drawGrid3D)
        {
            ViewportGrid3D.Render(graphics2D, _editorCamera);
            VisibilityZoneDebugDrawer.Draw(context, graphics2D);
        }

        graphics2D.EndScene();
    }

    private static bool HasCameraEntities(Context context)
    {
        foreach (var _ in context.View<CameraComponent>())
            return true;
        return false;
    }

    private void PickHoveredEntity()
    {
        var mousePos = ImGui.GetMousePos();
        // Keep last HoveredEntity when skipping ReadPixel (mouse still) — clearing first broke click-select.
        if (mousePos == _lastPickMousePos)
            return;

        _lastPickMousePos = mousePos;
        HoveredEntity = null;
        var mx = (mousePos.X - _viewportBounds[0].X) * _contentScale;
        var my = (mousePos.Y - _viewportBounds[0].Y) * _contentScale;
        var physicalWidth = (_viewportBounds[1].X - _viewportBounds[0].X) * _contentScale;
        var physicalHeight = (_viewportBounds[1].Y - _viewportBounds[0].Y) * _contentScale;
        my = physicalHeight - my;

        var mouseX = (int)mx;
        var mouseY = (int)my;

        if (mouseX < 0 || mouseY < 0 || mouseX >= (int)physicalWidth || mouseY >= (int)physicalHeight)
            return;

        var entityId = _frameBuffer.ReadPixel(1, mouseX, mouseY);
        Entity? entity = null;
        if (entityId > 0)
        {
            if (!_entityById.TryGetValue(entityId, out entity))
            {
                entity = sceneContext.ActiveScene?.Entities.FirstOrDefault(x => x.Id == entityId);
                if (entity is not null)
                    _entityById[entity.Id] = entity;
            }
        }

        HoveredEntity = entity;
    }

    private void UpdateFly(TimeSpan deltaTime)
    {
        if (!IsHovered || ImGui.GetIO().KeyAlt)
            return;

        if (!_pressedMouseButtons.Contains((int)ImGuiMouseButton.Right))
            return;

        var move = Vector3.Zero;
        if (_pressedKeys.Contains(KeyCodes.W))
            move.Z += 1.0f;
        if (_pressedKeys.Contains(KeyCodes.S))
            move.Z -= 1.0f;
        if (_pressedKeys.Contains(KeyCodes.A))
            move.X -= 1.0f;
        if (_pressedKeys.Contains(KeyCodes.D))
            move.X += 1.0f;
        if (_pressedKeys.Contains(KeyCodes.E))
            move.Y += 1.0f;
        if (_pressedKeys.Contains(KeyCodes.Q))
            move.Y -= 1.0f;

        if (move != Vector3.Zero)
            _editorCamera.Fly(move, (float)deltaTime.TotalSeconds);
    }

    private void HandleViewportInput()
    {
        var currentMode = viewport.SceneToolbar.CurrentMode;
        var globalMousePos = ImGui.GetMousePos();
        var localMousePos = new Vector2(globalMousePos.X - _viewportBounds[0].X, globalMousePos.Y - _viewportBounds[0].Y);

        viewport.ViewportToolManager.SetMode(currentMode);
        viewport.ViewportToolManager.SetHoveredEntity(HoveredEntity);

        if (selection.SelectedEntity is not null)
            viewport.ViewportToolManager.SetTargetEntity(selection.SelectedEntity);

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            viewport.ViewportToolManager.HandleMouseDown(localMousePos, _viewportBounds, _editorCamera);
            if (HoveredEntity != null
                && currentMode != EditorMode.Ruler
                && currentMode != EditorMode.Select
                && !ImGuizmoGizmo.IsOver
                && !ImGuizmoGizmo.IsUsing)
            {
                selection.Select(HoveredEntity, SelectionSource.Viewport);
            }
        }

        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            viewport.ViewportToolManager.HandleMouseMove(localMousePos, _viewportBounds, _editorCamera);

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            viewport.ViewportToolManager.HandleMouseUp(localMousePos, _viewportBounds, _editorCamera);
    }

    private void RebuildEntityLookup(IScene scene)
    {
        _entityById.Clear();
        foreach (var entity in scene.Entities)
            _entityById[entity.Id] = entity;
    }

    private static Vector2 GetMousePosition()
    {
        var pos = ImGui.GetMousePos();
        return new Vector2(pos.X, pos.Y);
    }
}
