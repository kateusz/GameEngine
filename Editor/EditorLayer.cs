using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Editor.Managers;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;
using NLog;
using ZLinq;
using Application = Engine.Core.Application;

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportHovered;
    private readonly Vector2[] _viewportBounds = new Vector2[2];
    private Entity? _hoveredEntity;
    private Texture2D _iconPlay;
    private Texture2D _iconStop;
    
    private Workspace _uiManager;
    private ProjectManager _projectManager;
    private SceneController _sceneController;
    private EditorInputHandler _inputHandler;
    
    // fps rate
    private readonly Queue<float> _frameTimes = new();
    private float _fpsUpdateTimer = 0.0f;
    private float _currentFps = 0.0f;
    private const float FpsUpdateInterval = 0.1f;
    private const int MaxFrameSamples = 60;
    
    public EditorLayer(string name) : base(name)
    {
    }

    public override void OnAttach()
    {
        Logger.Debug("EditorLayer OnAttach.");
        
        CurrentScene.Set(new Scene(""));
        
        _uiManager = new Workspace();
        _projectManager = new ProjectManager();
        _sceneController = new SceneController();
        
        // Create camera controller for input handler
        var cameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        _inputHandler = new EditorInputHandler(cameraController);
        
        WireUpEvents();
        
        _projectManager.Initialize();

        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");
        
        var frameBufferSpec = new FrameBufferSpecification(1200, 720)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth),
            ])
        };
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);
        
        // Set initial scripts directory
        UpdateScriptsDirectory();
        
        Console.WriteLine("✅ Editor initialized successfully!");
        Console.WriteLine("Console panel is now capturing output.");
    }

    private void WireUpEvents()
    {
        // UI Manager events
        _uiManager.OnNewProjectRequested += () => _projectManager.ShowNewProjectDialog();
        _uiManager.OnOpenProjectRequested += () => _projectManager.ShowOpenProjectDialog();
        _uiManager.OnNewSceneRequested += () => _sceneController.NewScene();
        _uiManager.OnOpenSceneRequested += () => _sceneController.OpenScene();
        _uiManager.OnSaveSceneRequested += () => _sceneController.SaveScene(_projectManager.CurrentProjectDirectory);
        _uiManager.OnFocusOnSelectedRequested += FocusOnSelectedEntity;
        _uiManager.OnResetCameraRequested += () => _inputHandler.ResetCamera();
        _uiManager.OnBuildAndPublishRequested += BuildAndPublish;
        
        // Input Handler events
        _inputHandler.OnNewSceneRequested += () => _sceneController.NewScene();
        _inputHandler.OnOpenSceneRequested += () => _sceneController.OpenScene();
        _inputHandler.OnSaveSceneRequested += () => _sceneController.SaveScene(_projectManager.CurrentProjectDirectory);
        _inputHandler.OnDuplicateEntityRequested += () => _sceneController.DuplicateEntity(_uiManager.SceneHierarchyPanel.GetSelectedEntity());
        _inputHandler.OnFocusOnSelectedRequested += FocusOnSelectedEntity;
        _inputHandler.OnLeftMousePressed += HandleEntitySelection;
        _inputHandler.OnCameraControllerUpdated += (newController) => {
            // Handle camera controller updates if needed
        };
        
        // Project Manager events
        _projectManager.OnProjectCreated += OnProjectChanged;
        _projectManager.OnProjectOpened += OnProjectChanged;
        
        // Scene Manager events
        _sceneController.OnSceneChanged += () => {
            _sceneController.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
            _uiManager.UpdateSceneContext();
        };
        _sceneController.OnSceneStateChanged += (state) => {
            _uiManager.UpdateSceneContext();
        };
        
        // UI Manager entity selection
        _uiManager.SetEntitySelectedCallback(EntitySelected);
    }
    
    private void EntitySelected(Entity entity)
    {
        _inputHandler.FocusOnEntity(entity);
    }

    public override void OnDetach()
    {
        Logger.Debug("EditorLayer OnDetach.");
        _uiManager?.Dispose();
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        UpdateFpsTracking(timeSpan);
        HandleViewportResize();
        _inputHandler.OnUpdate(timeSpan, _sceneController.CurrentState);
        RenderScene(timeSpan);
        HandleMousePicking();
    }
    
    private void HandleViewportResize()
    {
        var spec = _frameBuffer.GetSpecification();
        if (_viewportSize is { X: > 0.0f, Y: > 0.0f } && 
            (spec.Width != (uint)_viewportSize.X || spec.Height != (uint)_viewportSize.Y))
        {
            _frameBuffer.Resize((uint)_viewportSize.X, (uint)_viewportSize.Y);
            
            // Update camera aspect ratio
            float aspectRatio = _viewportSize.X / _viewportSize.Y;
            _inputHandler.UpdateCameraAspectRatio(aspectRatio);
            
            _sceneController.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        }
    }
    
    private void RenderScene(TimeSpan timeSpan)
    {
        Graphics2D.Instance.ResetStats();
        Graphics3D.Instance.ResetStats();
        _frameBuffer.Bind();

        Graphics2D.Instance.SetClearColor(_uiManager.BackgroundColor);
        Graphics2D.Instance.Clear();
        _frameBuffer.ClearAttachment(1, -1);

        _sceneController.UpdateScene(timeSpan, _inputHandler.CameraController.Camera);
        _frameBuffer.Unbind();
    }
    
    private void HandleMousePicking()
    {
        var mousePos = ImGui.GetMousePos();
        var mx = mousePos.X - _viewportBounds[0].X;
        var my = mousePos.Y - _viewportBounds[0].Y;
        var viewportSize = _viewportBounds[1] - _viewportBounds[0];
        my = viewportSize.Y - my; // Flip the Y-axis

        var mouseX = (int)mx;
        var mouseY = (int)my;

        if (mouseX >= 0 && mouseY >= 0 && mouseX < (int)viewportSize.X && mouseY < (int)viewportSize.Y)
        {
            var entityId = _frameBuffer.ReadPixel(1, mouseX, mouseY);
            var entity = CurrentScene.Instance.Entities.AsValueEnumerable().FirstOrDefault(x => x.Id == entityId);
            _hoveredEntity = entity;
        }
    }

    public override void HandleEvent(Event @event)
    {
        _inputHandler.HandleEvent(@event, _sceneController.CurrentState);
    }

    private void OnProjectChanged(string projectDir)
    {
        _uiManager.ContentBrowserPanel.SetRootDirectory(Path.Combine(projectDir, "assets"));
        UpdateScriptsDirectory();
    }
    
    private void UpdateScriptsDirectory()
    {
        string scriptsDir = _projectManager.GetScriptsDirectory();
        _projectManager.SetScriptsDirectory(scriptsDir);
    }

    private void FocusOnSelectedEntity()
    {
        var selectedEntity = _uiManager.SceneHierarchyPanel.GetSelectedEntity();
        _inputHandler.FocusOnEntity(selectedEntity);
    }
    
    private void HandleEntitySelection()
    {
        if (_viewportHovered && _hoveredEntity != null && !InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftAlt))
        {
            _uiManager.SceneHierarchyPanel.SetSelectedEntity(_hoveredEntity);
        }
    }

    public override void OnImGuiRender()
    {
        _uiManager.RenderMainUI(RenderToolbar, RenderViewport, (showSettings) => { });
        _projectManager.RenderProjectDialogs();
    }

    private void RenderToolbar()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 2));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));

        var colors = ImGui.GetStyle().Colors;
        var buttonHovered = colors[(int)ImGuiCol.ButtonHovered];
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonHovered with { W = 0.5f });

        var buttonActive = colors[(int)ImGuiCol.ButtonActive];
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonActive with { W = 0.5f });

        ImGui.Begin("##toolbar",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var size = ImGui.GetWindowHeight() - 4.0f;
        var icon = _sceneController.CurrentState == SceneState.Edit ? _iconPlay : _iconStop;

        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));

        if (ImGui.ImageButton("playstop", (IntPtr)icon.GetRendererId(), new Vector2(size, size), new Vector2(0, 0),
                new Vector2(1, 1)))
        {
            switch (_sceneController.CurrentState)
            {
                case SceneState.Edit:
                    _sceneController.PlayScene();
                    break;
                case SceneState.Play:
                    _sceneController.StopScene();
                    break;
            }
        }
        
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        ImGui.End();
    }

    private void RenderViewport()
    {
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

            var viewportFocused = ImGui.IsWindowFocused();
            _viewportHovered = ImGui.IsWindowHovered();
            _inputHandler.ViewportFocused = viewportFocused;
            Application.ImGuiLayer.BlockEvents = !viewportFocused && !_viewportHovered;

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
                        var path = Marshal.PtrToStringUni(payload.Data);
                        if (path is not null)
                            _sceneController.OpenScene(Path.Combine(AssetsManager.AssetsPath, path));
                    }
                    ImGui.EndDragDropTarget();
                }
            }

            ImGui.End();
        }

        ImGui.PopStyleVar();
        
        RenderStatsWindow();
    }
    
    private void RenderStatsWindow()
    {
        ImGui.Begin("Stats");

        var name = "None";
        if (_hoveredEntity != null)
        {
            name = _hoveredEntity.Name;
        }

        ImGui.Text($"Hovered Entity: {name}");
        
        RenderPerformanceStats();

        ImGui.Separator();
        var stats = Graphics2D.Instance.GetStats();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats.DrawCalls}");
        ImGui.Text($"Quads: {stats.QuadCount}");
        ImGui.Text($"Vertices: {stats.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats.GetTotalIndexCount()}");
        
        // Camera info
        ImGui.Text("Camera:");
        var camPos = _inputHandler.CameraController.Camera.Position;
        ImGui.Text($"Position: ({camPos.X:F2}, {camPos.Y:F2}, {camPos.Z:F2})");
        ImGui.Text($"Rotation: {_inputHandler.CameraController.Camera.Rotation:F1}°");
        
        // 3D Stats
        var stats3D = Graphics3D.Instance.GetStats();
        ImGui.Text("Renderer3D Stats:");
        ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");

        ImGui.End();
    }

    private void BuildAndPublish()
    {
        throw new NotImplementedException();
    }
    
    private void UpdateFpsTracking(TimeSpan timeSpan)
    {
        float deltaTime = (float)timeSpan.TotalSeconds;
        
        if (deltaTime <= 0) return;
        
        _frameTimes.Enqueue(deltaTime);
        
        while (_frameTimes.Count > MaxFrameSamples)
        {
            _frameTimes.Dequeue();
        }
        
        _fpsUpdateTimer += deltaTime;
        if (_fpsUpdateTimer >= FpsUpdateInterval)
        {
            CalculateFps();
            _fpsUpdateTimer = 0.0f;
        }
    }

    private void CalculateFps()
    {
        if (_frameTimes.Count == 0) return;
        
        float averageFrameTime = _frameTimes.Average();
        _currentFps = 1.0f / averageFrameTime;
    }

    private void RenderPerformanceStats()
    {
        ImGui.Separator();
        ImGui.Text("Performance:");
        
        var fpsColor = _currentFps >= 60.0f ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) :  
                       _currentFps >= 30.0f ? new Vector4(1.0f, 1.0f, 0.0f, 1.0f) :  
                                             new Vector4(1.0f, 0.0f, 0.0f, 1.0f);    
        
        ImGui.PushStyleColor(ImGuiCol.Text, fpsColor);
        ImGui.Text($"FPS: {_currentFps:F1}");
        ImGui.PopStyleColor();
        
        float currentFrameTime = _frameTimes.Count > 0 ? _frameTimes.Last() * 1000 : 0;
        ImGui.Text($"Frame Time: {currentFrameTime:F2} ms");
        
        ImGui.Text($"Frame Samples: {_frameTimes.Count}/{MaxFrameSamples}");
        
        if (_frameTimes.Count > 1)
        {
            float minFrameTime = _frameTimes.Min() * 1000;
            float maxFrameTime = _frameTimes.Max() * 1000;
            ImGui.Text($"Min/Max Frame Time: {minFrameTime:F2}/{maxFrameTime:F2} ms");
        }
    }
}