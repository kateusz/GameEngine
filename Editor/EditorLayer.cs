using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Editor.Panels;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
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

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _orthographicCameraController;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportFocused;
    private bool _viewportHovered;
    private readonly Vector2[] _viewportBounds = new Vector2[2];
    private Scene _activeScene;
    private SceneHierarchyPanel _sceneHierarchyPanel;
    private ContentBrowserPanel _contentBrowserPanel;
    private EditorCamera _editorCamera;
    private Entity? _hoveredEntity;
    private Texture2D _iconPlay;
    private Texture2D _iconStop;
    private SceneState _sceneState;

    public EditorLayer(string name) : base(name)
    {
    }

    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");

        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");
        _sceneState = SceneState.Edit;

        // todo: width and height from window props
        _orthographicCameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        var frameBufferSpec = new FrameBufferSpecification(1200, 720)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth),
            ])
        };
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);

        _activeScene = new Scene();
        _editorCamera = new EditorCamera(30.0f, 1280.0f / 720.0f, 0.1f, 1000.0f);
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
        if (_viewportSize is { X: > 0.0f, Y: > 0.0f } && // zero sized framebuffer is invalid
            (spec.Width != (uint)_viewportSize.X || spec.Height != (uint)_viewportSize.Y))
        {
            _frameBuffer.Resize((uint)_viewportSize.X, (uint)_viewportSize.Y);
            _editorCamera.SetViewportSize(_viewportSize.X, _viewportSize.Y);
            _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        }
        
        Renderer2D.Instance.ResetStats();
        _frameBuffer.Bind();

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        _frameBuffer.ClearAttachment(1, -1);
        var mousePos = ImGui.GetMousePos();

        switch (_sceneState)
        {
            case SceneState.Edit:
            {
                if (_viewportFocused)
                    _orthographicCameraController.OnUpdate(timeSpan);
                _editorCamera.OnUpdate(mousePos);
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
        var mx = mousePos.X - _viewportBounds[0].X;
        var my = mousePos.Y - _viewportBounds[0].Y;

        // Calculate viewport size
        var viewportSize = _viewportBounds[1] - _viewportBounds[0];
        my = viewportSize.Y - my; // Flip the Y-axis

        // Convert to integer mouse coordinates
        var mouseX = (int)mx;
        var mouseY = (int)my;

        // Check if the mouse is within the viewport bounds
        if (mouseX >= 0 && mouseY >= 0 && mouseX < (int)viewportSize.X && mouseY < (int)viewportSize.Y)
        {
            // Read pixel data from the framebuffer (assuming your ReadPixel method is defined)
            var entityId = _frameBuffer.ReadPixel(1, mouseX, mouseY);
            var entity = _activeScene.Entities.FirstOrDefault(x => x.Id == entityId);
            _hoveredEntity = entity;
        }

        _frameBuffer.Unbind();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);

        _orthographicCameraController.OnEvent(@event);
        _editorCamera.OnEvent(@event);

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

        var control = InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftControl) ||
                       InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.RightControl);
        var shift = InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftShift) ||
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

    public override void OnImGuiRender()
    {
        SubmitUI();
    }

    private void UI_Toolbar()
    {
        // Pushing style variables and colors using ImGui.NET syntax
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 2));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));

        var colors = ImGui.GetStyle().Colors;
        var buttonHovered = colors[(int)ImGuiCol.ButtonHovered];
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonHovered with { W = 0.5f });

        var buttonActive = colors[(int)ImGuiCol.ButtonActive];
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonActive with { W = 0.5f });

        // Begin toolbar window
        ImGui.Begin("##toolbar",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        // Calculate button size based on window height
        var size = ImGui.GetWindowHeight() - 4.0f;

        // Set the appropriate icon (m_IconPlay or m_IconStop) based on the scene state
        var icon = _sceneState == SceneState.Edit ? _iconPlay : _iconStop;

        // Center the icon button in the window
        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));

        // Display the icon button and handle button press
        if (ImGui.ImageButton("playstop", (IntPtr)icon.GetRendererId(), new Vector2(size, size), new Vector2(0, 0),
                new Vector2(1, 1)))
        {
            switch (_sceneState)
            {
                case SceneState.Edit:
                    OnScenePlay();
                    break;
                case SceneState.Play:
                    OnSceneStop();
                    break;
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
        const ImGuiDockNodeFlags dockspaceFlags = ImGuiDockNodeFlags.None;
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        
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
            var minWinSizeX = style.WindowMinSize.X;
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

            var name = "None";
            if (_hoveredEntity != null)
            {
                name = _hoveredEntity.Name;
            }

            ImGui.Text($"Hovered Entity: {name}");

            var stats = Renderer2D.Instance.GetStats();
            ImGui.Text("Renderer2D Stats:");
            ImGui.Text($"Draw Calls: {stats.DrawCalls}");
            ImGui.Text($"Quads: {stats.QuadCount}");
            ImGui.Text($"Vertices: {stats.GetTotalVertexCount()}");
            ImGui.Text($"Indices: {stats.GetTotalIndexCount()}");

            ImGui.End();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

            ImGui.Begin("Viewport");
            {
                var viewportMinRegion = ImGui.GetWindowContentRegionMin();
                var viewportMaxRegion = ImGui.GetWindowContentRegionMax();
                var viewportOffset = ImGui.GetWindowPos();
                _viewportBounds[0] = new Vector2(viewportMinRegion.X + viewportOffset.X,
                    viewportMinRegion.Y + viewportOffset.Y);
                _viewportBounds[1] = new Vector2(viewportMaxRegion.X + viewportOffset.X,
                    viewportMaxRegion.Y + viewportOffset.Y);

                _viewportFocused = ImGui.IsWindowFocused();
                _viewportHovered = ImGui.IsWindowHovered();
                Application.ImGuiLayer.BlockEvents = !_viewportFocused && !_viewportHovered;

                var viewportPanelSize = ImGui.GetContentRegionAvail();
                _viewportSize = viewportPanelSize;
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
                            var path = Marshal.PtrToStringUni(payload.Data); // Converting IntPtr to string (wchar_t* in C#)
                            if (path is not null)
                                OpenScene(Path.Combine(AssetsManager.AssetsPath, path)); // Combining paths
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

    private void OnMouseButtonPressed(MouseButtonPressedEvent e)
    {
        if (e.Button != (int)MouseButton.Left) 
            return;
        
        if (_viewportHovered && _hoveredEntity != null && !InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftAlt))
        {
            _sceneHierarchyPanel.SetSelectedEntity(_hoveredEntity);
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
        // TODO: from configuration
        const string filePath = "assets/scenes/Example.scene";


        if (string.IsNullOrWhiteSpace(filePath))
            throw new Exception($"Scene doesnt exists: {filePath}");
        
        _activeScene = new Scene();
        _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_activeScene);

        SceneSerializer.Deserialize(_activeScene, filePath);
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
        // TODO: from config
        var filePath = $"assets/scenes/Example-{DateTime.UtcNow.ToShortDateString()}.scene";
        SceneSerializer.Serialize(_activeScene, filePath);
    }

    private void OnScenePlay()
    {
        _sceneState = SceneState.Play;
    }

    private void OnSceneStop()
    {
        _sceneState = SceneState.Edit;
    }
}