using System.Numerics;
using ECS;
using Editor.Panels;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
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
    private Texture2D _checkerboardTexture;
    private Scene _activeScene;
    private Entity _squareEntity;
    private Vector4 _squareColor;
    private Entity _cameraEntity;
    private Entity _secondCamera;
    private bool _primaryCamera = true;
    private Vector3 _translation;
    private SceneHierarchyPanel _sceneHierarchyPanel; 

    public EditorLayer(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        if (_viewportFocused)
            _orthographicCameraController.OnUpdate(timeSpan);

        _frameBuffer.Bind();

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();
        
        _activeScene.OnUpdate(timeSpan);

        _frameBuffer.Unbind();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);

        _orthographicCameraController.OnEvent(@event);
    }

    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");

        _checkerboardTexture = TextureFactory.Create("assets/textures/Checkerboard.png");

        _orthographicCameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        var frameBufferSpec = new FrameBufferSpecification(852, 701);
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);

        _activeScene = new Scene();
        
        // todo: why is it not centered after run?
        //_translation = new Vector3(1.2f, 1.2f, 0.0f);
        _translation = new Vector3();

        /*
        var squareColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        var square = _activeScene.CreateEntity("Square");
        square.AddComponent(new TransformComponent());
        square.AddComponent(new SpriteRendererComponent(squareColor));
        Context.Instance.Register(square);

        _squareEntity = square;
        _squareColor = squareColor;
        
        var redSquare = _activeScene.CreateEntity("Red Square");
        redSquare.AddComponent(new SpriteRendererComponent(new Vector4(1.0f, 0.0f, 0.0f, 1.0f)));
        
        _cameraEntity = _activeScene.CreateEntity("Camera Entity");

        _cameraEntity.AddComponent(new TransformComponent());

        var cameraComponent = new CameraComponent();
        _cameraEntity.AddComponent(cameraComponent);
        Context.Instance.Register(_cameraEntity);

        _secondCamera = _activeScene.CreateEntity("Clip-Space Entity");
        _secondCamera.AddComponent(new TransformComponent());
        var secondCameraComponent = new CameraComponent
        {
            Primary = false
        };
        
        _secondCamera.AddComponent(secondCameraComponent);
        Context.Instance.Register(_secondCamera);

        _cameraEntity.AddComponent(new NativeScriptComponent
        {
            ScriptableEntity = new CameraController()
        });
        */

        _sceneHierarchyPanel = new SceneHierarchyPanel(_activeScene);
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
                    if (ImGui.MenuItem("Serialize"))
                    {
                        SceneSerializer.Serialize(_activeScene, "assets/scenes/Example.scene");
                    }

                    if (ImGui.MenuItem("Deserialize"))
                    {
                        SceneSerializer.Deserialize(_activeScene, "assets/scenes/Example.scene");
                    }
                    
                    if (ImGui.MenuItem("Exit"))
                    {
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }
            
            _sceneHierarchyPanel.OnImGuiRender();

            ImGui.Begin("Settings");
            
            var oldValue = _translation;
            ImGui.DragFloat3("Camera Transform", ref _translation, 0.1f);

            if (_translation != oldValue)
            {
                var cameraEntity = Context.Instance.Entities.First(x => x.Name == "Camera Entity");
                cameraEntity.GetComponent<TransformComponent>().Translation = _translation;
            }
            
            if (ImGui.Checkbox("Camera A", ref _primaryCamera))
            {
                var cameraEntity = Context.Instance.Entities.First(x => x.Name == "Camera Entity");
                var secondCamera = Context.Instance.Entities.First(x => x.Name == "Clip-Space Entity");
                
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
}