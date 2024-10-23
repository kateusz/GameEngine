using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Editor.Panels;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.ImGuiNet;
using Engine.Math;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using ImGuiNET;
using NLog;
using Silk.NET.GLFW;
using Application = Engine.Core.Application;
using ImGuiGizmoOperation = Engine.ImGuiNet.ImGuiGizmoOperation;

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _orthographicCameraController;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportFocused;
    private bool _viewportHovered;
    private Vector2[] _viewportBounds = new Vector2[2];
    private Scene _activeScene;
    private Entity _secondCamera;
    private bool _primaryCamera = true;
    private Vector3 _translation;
    private SceneHierarchyPanel _sceneHierarchyPanel;
    private ContentBrowserPanel _contentBrowserPanel;
    private EditorCamera _editorCamera;
    private ImGuiGizmoOperation _gizmoType = ImGuiGizmoOperation.NONE;
    private Entity _hoveredEntity;
    private Texture2D _iconPlay;
    private Texture2D _iconStop;
    private SceneState _sceneState;
    private string _assetPath = Path.Combine(Environment.CurrentDirectory, "assets");

    public EditorLayer(string name) : base(name)
    {
    }
    
    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");
        
        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");
        _sceneState = SceneState.Edit;

        _orthographicCameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        var frameBufferSpec = new FrameBufferSpecification(1200, 720);
        frameBufferSpec.AttachmentsSpec = new ([
            new(FramebufferTextureFormat.RGBA8),
            new(FramebufferTextureFormat.RED_INTEGER),
            new(FramebufferTextureFormat.Depth),
        ]);
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

    public override void OnUpdate(TimeSpan timeSpan)
    {
        // Resize
        // TODO: is it needed?
        var spec = _frameBuffer.GetSpecification();
        if (_viewportSize.X > 0.0f && _viewportSize.Y > 0.0f && // zero sized framebuffer is invalid
            (spec.Width != _viewportSize.X || spec.Height != _viewportSize.Y))
        {
            _frameBuffer.Resize((uint)_viewportSize.X, (uint)_viewportSize.Y);
            //_cameraController.OnResize(_viewportSize.X, _viewportSize.y);
            _editorCamera.SetViewportSize(_viewportSize.X, _viewportSize.Y);
            _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        }
        
        // todo: stats
        //Renderer2D.ResetStats();
        _frameBuffer.Bind();

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        _frameBuffer.ClearAttachment(1, -1);
        
        switch (_sceneState)
        {
            case SceneState.Edit:
            {
                if (_viewportFocused)
                    _orthographicCameraController.OnUpdate(timeSpan);
                _editorCamera.OnUpdate(timeSpan);
                _activeScene.OnUpdateEditor(timeSpan, _editorCamera);
                break;
            }
            case SceneState.Play:
            {
                _activeScene.OnUpdateRuntime(timeSpan);
                break;
            }
        }
        
        // Get mouse position from ImGui
        var mousePos = ImGui.GetMousePos();
        var mx = mousePos.X - _viewportBounds[0].X;
        var my = mousePos.Y - _viewportBounds[0].Y;

        // Calculate viewport size
        var viewportSize = _viewportBounds[1] - _viewportBounds[0];
        my = viewportSize.Y - my; // Flip the Y-axis

        // Convert to integer mouse coordinates
        int mouseX = (int)mx;
        int mouseY = (int)my;

        Console.WriteLine($"X: {mouseX}, Y: {mouseY}");

        // Check if the mouse is within the viewport bounds
        if (mouseX >= 0 && mouseY >= 0 && mouseX < (int)viewportSize.X && mouseY < (int)viewportSize.Y)
        //if (mouseX >= 0 && mouseY >= 0)
        {
            // Read pixel data from the framebuffer (assuming your ReadPixel method is defined)
            int pixelData = _frameBuffer.ReadPixel(1, mouseX, mouseY);

            // Log or warn about the pixel data
            Console.WriteLine($"Pixel data = {pixelData}");
            
            // TODO: access based on entityId
            //_hoveredEntity = pixelData == -1 ? Entity() : Entity((entt::entity)pixelData, _activeScene);
        }
        
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
        else if (@event is MouseButtonPressedEvent mouseButtonPressedEvent)
        {
            OnMouseButtonPressed(mouseButtonPressedEvent);
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
            // Gizmos
            case (int)KeyCodes.Q:
                _gizmoType = ImGuiGizmoOperation.NONE;
                break;
            case (int)KeyCodes.W:
                _gizmoType = ImGuiGizmoOperation.TRANSLATE;
                break;
            case (int)KeyCodes.E:
                _gizmoType = ImGuiGizmoOperation.ROTATE;
                break;
            case (int)KeyCodes.R:
                _gizmoType = ImGuiGizmoOperation.SCALE;
                break;
        }
    }

    public override void OnImGuiRender()
    {
        SubmitUI();
    }

    private void UI_Toolbar()
    {
        // Pushing style variables and colors using ImGui.NET syntax
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 2));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new System.Numerics.Vector2(0, 0));

        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0, 0, 0, 0));

        var colors = ImGui.GetStyle().Colors;
        var buttonHovered = colors[(int)ImGuiCol.ButtonHovered];
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(buttonHovered.X, buttonHovered.Y, buttonHovered.Z, 0.5f));

        var buttonActive = colors[(int)ImGuiCol.ButtonActive];
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(buttonActive.X, buttonActive.Y, buttonActive.Z, 0.5f));

        // Begin toolbar window
        ImGui.Begin("##toolbar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        // Calculate button size based on window height
        float size = ImGui.GetWindowHeight() - 4.0f;

        // Set the appropriate icon (m_IconPlay or m_IconStop) based on the scene state
        var icon = _sceneState == SceneState.Edit ? _iconPlay : _iconStop;

        // Center the icon button in the window
        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));

        // Display the icon button and handle button press
        if (ImGui.ImageButton("playstop", (IntPtr)icon.GetRendererId(), new Vector2(size, size), new Vector2(0, 0), new Vector2(1, 1)))
        {
            if (_sceneState == SceneState.Edit)
            {
                OnScenePlay();
            }
            else if (_sceneState == SceneState.Play)
            {
                OnSceneStop();
            }
        }

        // Pop the style and color variables
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);

        // End the toolbar window
        ImGui.End();
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
            
            ImGui.Begin("Stats");

            string name = "None";
            if (_hoveredEntity != null)
            {
                name = _hoveredEntity.GetComponent<TagComponent>().Tag;
            }
            ImGui.Text($"Hovered Entity: {name}");

            //var stats = Renderer2D.GetStats();
            ImGui.Text("Renderer2D Stats:");
            // ImGui.Text($"Draw Calls: {stats.DrawCalls}");
            // ImGui.Text($"Quads: {stats.QuadCount}");
            // ImGui.Text($"Vertices: {stats.GetTotalVertexCount()}");
            // ImGui.Text($"Indices: {stats.GetTotalIndexCount()}");

            ImGui.End();
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

            ImGui.Begin("Viewport");
            {
                var viewportMinRegion = ImGui.GetWindowContentRegionMin();
                var viewportMaxRegion = ImGui.GetWindowContentRegionMax();
                var viewportOffset = ImGui.GetWindowPos();
                _viewportBounds[0] = new Vector2(viewportMinRegion.X + viewportOffset.X, viewportMinRegion.Y + viewportOffset.Y);
                _viewportBounds[1] = new Vector2(viewportMaxRegion.X + viewportOffset.X, viewportMaxRegion.Y + viewportOffset.Y);
                
                _viewportFocused = ImGui.IsWindowFocused();
                _viewportHovered = ImGui.IsWindowHovered();
                Application.ImGuiLayer.BlockEvents = !_viewportFocused && !_viewportHovered;

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
                
                if (ImGui.BeginDragDropTarget())
                {
                    unsafe
                    {
                        ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                        if (payload.NativePtr != null)
                        {
                            string path = Marshal.PtrToStringUni(payload.Data); // Converting IntPtr to string (wchar_t* in C#)
                            OpenScene(System.IO.Path.Combine(_assetPath, path)); // Combining paths
                        }
                        ImGui.EndDragDropTarget();
                    }
                }
                
                ImGui.End();
            }
            
            // Gizmo
            var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
            if (selectedEntity != null)
            {
                // Camera
                var cameraEntity = _activeScene.GetPrimaryCameraEntity();
                if (cameraEntity is null)
                    return;
                
                var cameraComponent = cameraEntity.GetComponent<CameraComponent>();
                var camera = cameraComponent.Camera;
                var cameraProjection = camera.Projection;
                
                Matrix4x4.Invert(cameraEntity.GetComponent<TransformComponent>().GetTransform(), out var cameraView);
                
                // Entity transform
                var transformComponent = selectedEntity.GetComponent<TransformComponent>();
                var transform = transformComponent.GetTransform();
        

            }
            
            ImGui.End();
            ImGui.PopStyleVar();

            UI_Toolbar();

            ImGui.End();
        }
    }
    
    private bool OnMouseButtonPressed(MouseButtonPressedEvent e)
    {
        if (e.Button == (int)MouseButton.Left)
        {
            if (_viewportHovered && !InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftAlt))
            {
                //_sceneHierarchyPanel.SetSelectedEntity(_hoveredEntity);
            }
            
        }
        
        return false;
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
    
    private void OpenScene(string path)
    {
        _activeScene = new Scene();
        _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_activeScene);

        SceneSerializer.Deserialize(_activeScene, path);
    }

    private void SaveSceneAs()
    {
        var filePath = $"assets/scenes/Example-{DateTime.UtcNow.ToShortDateString()}.scene";
        SceneSerializer.Serialize(_activeScene, filePath);
    }
    
    void OnScenePlay()
    {
        _sceneState = SceneState.Play;
    }
    void OnSceneStop()
    {
        _sceneState = SceneState.Edit;
    }
}