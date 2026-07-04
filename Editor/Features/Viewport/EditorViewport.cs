using System.Numerics;
using ECS;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Core.Window;
using Engine.Physics;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Serializer;
using ImGuiNET;
using Math;

namespace Editor.Features.Viewport;

public sealed class EditorViewport(
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    DebugSettings debugSettings,
    EditorSettingsUI editorSettingsUI,
    IFrameBufferFactory frameBufferFactory,
    IContentScaleProvider contentScaleProvider,
    IEditorSelection selection,
    IEditorCameraController cameraController,
    ViewportComponents viewport)
    : IEditorViewport
{
    private readonly Vector2[] _viewportBounds = new Vector2[2];

    private EditorCamera _editorCamera = null!;
    private IFrameBuffer _frameBuffer = null!;
    private float _contentScale = 1.0f;
    private Vector2 _viewportSize;
    private readonly Dictionary<int, Entity> _entityById = [];

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
        _contentScale = contentScaleProvider.ContentScale;

        if (sceneContext.ActiveScene is not null)
            RebuildEntityLookup(sceneContext.ActiveScene);
    }

    public void Dispose()
    {
        sceneContext.SceneChanged -= _sceneChangedHandler;
        _frameBuffer?.Dispose();
    }

    public void LayoutAndRender(TimeSpan deltaTime)
    {
        ImGui.Begin("Viewport");

        IsHovered = ImGui.IsWindowHovered();

        var viewportPanelSize = ImGui.GetContentRegionAvail();

        _viewportBounds[0] = ImGui.GetCursorScreenPos();
        _viewportBounds[1] = _viewportBounds[0] + viewportPanelSize;
        _viewportSize = viewportPanelSize;

        ResizeFramebufferIfNeeded();
        RenderSceneToFramebuffer(deltaTime);

        var texturePointer = new IntPtr(_frameBuffer.GetColorAttachmentRendererId());
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

        DrawOverlays();

        ImGui.End();
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
        _editorCamera.SetViewportSize(_viewportSize.X, _viewportSize.Y);
        sceneContext.ActiveScene?.OnViewportResize(fbWidth, fbHeight);
    }

    private void RenderSceneToFramebuffer(TimeSpan deltaTime)
    {
        graphics2D.ResetStats();
        _frameBuffer.Bind();

        graphics2D.SetClearColor(editorSettingsUI.GetBackgroundColor());
        graphics2D.Clear();
        _frameBuffer.ClearAttachment(1, -1);

        switch (sceneContext.State)
        {
            case SceneState.Edit:
                if (sceneContext.ActiveScene is { } scene)
                {
                    var camera = SceneRenderPipeline.CameraBinding.FromEditor(_editorCamera);
                    SceneRenderPipeline.RenderScene(
                        scene.Context,
                        graphics2D,
                        graphics3D,
                        textureFactory,
                        camera);
                    if (debugSettings.ShowColliderBounds && sceneContext.ActivePhysicsBodyStore is { } bodyStore)
                        PhysicsDebugDrawer.Draw(scene.Context, graphics2D, bodyStore, camera, useTransformFallbackWhenNoBody: true);
                }
                break;
            case SceneState.Play:
                sceneContext.ActiveScene?.OnUpdateRuntime(deltaTime);
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
}
