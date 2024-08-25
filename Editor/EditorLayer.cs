using System.Numerics;
using ECS;
using Editor.Panels;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using ImGuiNET;
using NLog;
using Application = Engine.Core.Application;

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _orthographicCameraController;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportFocused;
    private bool _viewportHovered;
    private Scene _activeScene;
    private Entity _secondCamera;
    private bool _primaryCamera = true;
    private Vector3 _translation;
    private SceneHierarchyPanel _sceneHierarchyPanel;
    private ContentBrowserPanel _contentBrowserPanel;
    private EditorCamera _editorCamera;

    public EditorLayer(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        // Resize
        // TODO: is it needed?
        // var spec = _frameBuffer.GetSpecification();
        // if (_viewportSize.X > 0.0f && _viewportSize.Y > 0.0f && // zero sized framebuffer is invalid
        //     (spec.Width != _viewportSize.X || spec.Height != _viewportSize.Y))
        // {
        //     _frameBuffer.Resize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        //     //_cameraController.OnResize(_viewportSize.X, _viewportSize.y);
        //     _editorCamera.SetViewportSize(_viewportSize.X, _viewportSize.Y);
        //     _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        // }
        
        if (_viewportFocused)
            _orthographicCameraController.OnUpdate(timeSpan);
        
        //_editorCamera.OnUpdate(timeSpan);
        _frameBuffer.Bind();

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        _activeScene.OnUpdateRuntime(timeSpan);
        //_activeScene.OnUpdateEditor(timeSpan, _editorCamera);

        _frameBuffer.Unbind();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);
        
        _orthographicCameraController.OnEvent(@event);
        //_editorCamera.OnEvent(@event);

        if (@event is KeyPressedEvent keyPressedEvent)
        {
            OnKeyPressed(keyPressedEvent);
        }
    }

    private void OnKeyPressed(KeyPressedEvent keyPressedEvent)
    {
        // Shortcuts
        if (keyPressedEvent.RepeatCount > 0)
            return;

        bool control = InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftControl) ||
                       InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.RightControl);
        bool shift = InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftShift) ||
                     InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.RightShift);
        switch (keyPressedEvent.KeyCode)
        {
            case (int)KeyCodes.N:
            {
                if (control)
                    NewScene();

                break;
            }
            case (int)KeyCodes.O:
            {
                if (control)
                    OpenScene();

                break;
            }
            case (int)KeyCodes.S:
            {
                if (control && shift)
                    SaveSceneAs();

                break;
            }
        }
    }

    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");

        _orthographicCameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        var frameBufferSpec = new FrameBufferSpecification(852, 701);
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);

        _activeScene = new Scene();
        _editorCamera = new EditorCamera(30.0f, 1.778f, 0.1f, 1000.0f);
        _translation = new Vector3();
        _sceneHierarchyPanel = new SceneHierarchyPanel(_activeScene);
        _contentBrowserPanel = new ContentBrowserPanel();
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }

    public override void OnImGuiRender()
    {
        SubmitUI();
    }

    private void SubmitUI()
    {
        var dockspaceOpen = true;
        const bool fullscreenPersistant = true;
        var dockspaceFlags = ImGuiDockNodeFlags.None;

        var windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        if (fullscreenPersistant)
        {
            var viewPort = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewPort.Pos);
            ImGui.SetNextWindowSize(viewPort.Size);
            ImGui.SetNextWindowViewport(viewPort.ID);
        }

        ImGui.Begin("DockSpace Demo", ref dockspaceOpen, windowFlags);
        {
            var style = ImGui.GetStyle();
            float minWinSizeX = style.WindowMinSize.X;
            style.WindowMinSize.X = 370.0f;

            var dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new Vector2(0.0f, 0.0f), dockspaceFlags);

            style.WindowMinSize.X = minWinSizeX;

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New", "Ctrl+N"))
                        NewScene();

                    if (ImGui.MenuItem("Open...", "Ctrl+O"))
                        OpenScene();

                    if (ImGui.MenuItem("Save As...", "Ctrl+Shift+S"))
                        SaveSceneAs();

                    if (ImGui.MenuItem("Exit"))
                    {
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            _sceneHierarchyPanel.OnImGuiRender();
            _contentBrowserPanel.OnImGuiRender();

            ImGui.Begin("Settings");

            var oldValue = _translation;
            ImGui.DragFloat3("Camera Transform", ref _translation, 0.1f);

            if (_translation != oldValue)
            {
                var cameraEntity = _activeScene.Entities.First(x => x.Name == "Camera Entity");
                cameraEntity.GetComponent<TransformComponent>().Translation = _translation;
            }

            if (ImGui.Checkbox("Camera A", ref _primaryCamera))
            {
                var cameraEntity = _activeScene.Entities.First(x => x.Name == "Camera Entity");
                var secondCamera = _activeScene.Entities.First(x => x.Name == "Clip-Space Entity");

                cameraEntity.GetComponent<CameraComponent>().Primary = _primaryCamera;
                secondCamera.GetComponent<CameraComponent>().Primary = !_primaryCamera;
            }

            // TODO: this value is null before deserialization - make it more flexible
            if (_secondCamera is not null)
            {
                var camera = _secondCamera.GetComponent<CameraComponent>().Camera;
                var val = camera.OrthographicSize;
                if (ImGui.DragFloat("Second Camera Ortho Size", ref val))
                {
                    camera.SetOrthographicSize(val);
                }
            }

            ImGui.Begin("Viewport");
            {
                _viewportFocused = ImGui.IsWindowFocused();
                _viewportHovered = ImGui.IsWindowHovered();
                Application.ImGuiLayer.BlockEvents = !_viewportFocused || !_viewportHovered;

                var viewportPanelSize = ImGui.GetContentRegionAvail();
                if (_viewportSize != viewportPanelSize && viewportPanelSize.X > 0 && viewportPanelSize.Y > 0)
                {
                    _frameBuffer.Resize((uint)viewportPanelSize.X, (uint)viewportPanelSize.Y);
                    _viewportSize = new Vector2(viewportPanelSize.X, viewportPanelSize.Y);

                    var @resizeEvent = new WindowResizeEvent((int)viewportPanelSize.X, (int)viewportPanelSize.Y);
                    _orthographicCameraController.OnEvent(@resizeEvent);

                    _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
                }

                var textureId = _frameBuffer.GetColorAttachmentRendererId();
                var texturePointer = new IntPtr(textureId);
                ImGui.Image(texturePointer, new Vector2(_viewportSize.X, _viewportSize.Y), new Vector2(0, 1),
                    new Vector2(1, 0));
                ImGui.End();
            }
        }
    }

    private void NewScene()
    {
        _activeScene = new Scene();
        _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_activeScene);
    }

    private void OpenScene()
    {
        var filePath = "assets/scenes/Example.scene";

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _activeScene = new Scene();
            _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
            _sceneHierarchyPanel.SetContext(_activeScene);

            SceneSerializer.Deserialize(_activeScene, filePath);
        }
    }

    private void SaveSceneAs()
    {
        var filePath = $"assets/scenes/Example-{DateTime.UtcNow.ToShortDateString()}.scene";
        SceneSerializer.Serialize(_activeScene, filePath);
    }
}