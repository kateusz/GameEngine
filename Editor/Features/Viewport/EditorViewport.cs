using System.Numerics;
using ECS;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Features.Viewport.Gizmos;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Physics;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Cameras;
using Engine.Scene.Serializer;
using ImGuiNET;
using Input;
using Math;

namespace Editor.Features.Viewport;

public sealed class EditorViewport(
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IModelFactory modelFactory,
    ISkeletonFactory skeletonFactory,
    IAnim3dFactory anim3dFactory,
    DebugSettings debugSettings,
    EditorSettingsUI editorSettingsUI,
    IEditorPreferences editorPreferences,
    IFrameBufferFactory frameBufferFactory,
    HdrTonemapPass hdrTonemapPass,
    IContentScaleProvider contentScaleProvider,
    IEditorSelection selection,
    IEditorCameraController cameraController,
    ViewportComponents viewport,
    IPointerSurface pointerSurface)
    : IEditorViewport
{
    private readonly Vector2[] _viewportBounds = new Vector2[2];

    private EditorCamera _editorCamera = null!;
    private IFrameBuffer _frameBuffer = null!;
    private IFrameBuffer _sdrFrameBuffer = null!;
    private float _contentScale = 1.0f;
    private Vector2 _viewportSize;
    private readonly Dictionary<int, Entity> _entityById = [];
    private readonly HashSet<int> _pressedMouseButtons = [];
    private readonly HashSet<KeyCodes> _pressedKeys = [];

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
        _frameBuffer = frameBufferFactory.Create();
        _sdrFrameBuffer = frameBufferFactory.Create(new FrameBufferSpecification(
            DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8),
            ])
        });
        _contentScale = contentScaleProvider.ContentScale;

        if (sceneContext.ActiveScene is not null)
            RebuildEntityLookup(sceneContext.ActiveScene);
    }

    public void Dispose()
    {
        sceneContext.SceneChanged -= _sceneChangedHandler;
        _frameBuffer?.Dispose();
        _sdrFrameBuffer?.Dispose();
    }

    public void LayoutAndRender(TimeSpan deltaTime)
    {
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

        hdrTonemapPass.Apply(
            _frameBuffer.GetColorAttachmentRendererId(),
            _sdrFrameBuffer,
            editorPreferences.HdrExposure);

        var texturePointer = new IntPtr(_sdrFrameBuffer.GetColorAttachmentRendererId());
        ImGui.Image(texturePointer, viewportPanelSize, new Vector2(0, 1), new Vector2(1, 0));

        _viewportBounds[0] = ImGui.GetItemRectMin();
        _viewportBounds[1] = ImGui.GetItemRectMax();
        _viewportSize = _viewportBounds[1] - _viewportBounds[0];

        PickHoveredEntity();

        var sceneValidator = DragDropDrawer.CreateExtensionValidator([".scene"], checkFileExists: false);
        DragDropDrawer.HandleFileDropTarget(DragDropDrawer.ContentBrowserItemPayload, sceneValidator,
            onDropped: path => sceneManager.Open(PathBuilder.Build(path)));

        if (ImGui.IsWindowHovered())
            HandleViewportInput();

        UpdateFly(deltaTime);

        DrawOverlays();

        ImGui.End();
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

        if (viewport.SceneToolbar.ShowGrid)
            viewport.ViewportGrid.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);

        viewport.ViewportRuler.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);
        viewport.ViewportToolManager.RenderActiveTool(_viewportBounds, _editorCamera);
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
        _sdrFrameBuffer.Resize(fbWidth, fbHeight);
        _editorCamera.SetViewportSize(_viewportSize.X, _viewportSize.Y);
        sceneContext.ActiveScene?.OnViewportResize(fbWidth, fbHeight);
    }

    public static void ApplyWireframeDuring(IGraphics3D graphics3D, ViewportDisplayMode mode, Action scenePass)
    {
        try
        {
            graphics3D.SetWireframe(mode == ViewportDisplayMode.Wireframe);
            scenePass();
        }
        finally
        {
            graphics3D.SetWireframe(false);
        }
    }

    private void RenderSceneToFramebuffer(TimeSpan deltaTime)
    {
        graphics2D.ResetStats();
        _frameBuffer.Bind();

        var clearColor = sceneContext.ActiveScene?.BackgroundColor
            ?? editorSettingsUI.GetBackgroundColor();
        graphics2D.SetClearColor(clearColor);
        graphics2D.Clear();
        _frameBuffer.ClearAttachment(1, -1);

        var displayMode = viewport.SceneToolbar.ViewportDisplayMode;

        switch (sceneContext.State)
        {
            case SceneState.Edit:
                if (sceneContext.ActiveScene is { } scene)
                {
                    scene.UpdateWorldTransforms();
                    SkeletalPlaybackUpdater.Update(scene.Context, skeletonFactory, anim3dFactory, deltaTime);
                    var camera = SceneRenderPipeline.CameraBinding.FromEditor(_editorCamera);
                    ApplyWireframeDuring(graphics3D, displayMode, () =>
                        SceneRenderPipeline.RenderScene(
                            scene.Context,
                            graphics2D,
                            graphics3D,
                            textureFactory,
                            modelFactory,
                            camera));
                    if (debugSettings.ShowColliderBounds && sceneContext.ActivePhysicsBodyStore is { } bodyStore)
                        PhysicsDebugDrawer.Draw(scene.Context, graphics2D, bodyStore, camera, useTransformFallbackWhenNoBody: true);
                    CameraGizmoDrawer.Draw(scene.Context, graphics2D, _editorCamera, textureFactory);
                }
                break;
            case SceneState.Play:
                ApplyWireframeDuring(graphics3D, displayMode, () =>
                    sceneContext.ActiveScene?.OnUpdateRuntime(deltaTime));
                break;
        }

        if (sceneContext.State == SceneState.Edit && viewport.SceneToolbar.ShowGrid3D)
        {
            graphics2D.BeginScene(_editorCamera);
            viewport.ViewportGrid3D.Render(graphics2D, _editorCamera);
            graphics2D.EndScene();
        }

        _frameBuffer.Unbind();
    }

    private void PickHoveredEntity()
    {
        HoveredEntity = null;

        var mousePos = ImGui.GetMousePos();
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
                && currentMode != EditorMode.Select)
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
