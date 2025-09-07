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
using Engine.Scripting;
using ImGuiNET;
using NLog;
using Silk.NET.GLFW;
using ZLinq;
using Application = Engine.Core.Application;

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _cameraController;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportFocused;
    private bool _viewportHovered;
    private readonly Vector2[] _viewportBounds = new Vector2[2];
    private SceneHierarchyPanel _sceneHierarchyPanel;
    private ContentBrowserPanel _contentBrowserPanel;
    private PropertiesPanel _propertiesPanel;
    private ConsolePanel _consolePanel;
    private Entity? _hoveredEntity;
    private Texture2D _iconPlay;
    private Texture2D _iconStop;
    private SceneState _sceneState;
    private string? _editorScenePath;
    private Vector4 _backgroundColor = new Vector4(232.0f, 232.0f, 232.0f, 1.0f);
    private bool _showSettings = false;
    private bool _showNewProjectPopup = false;
    private string _newProjectName = string.Empty;
    private string _newProjectError = string.Empty;
    private string? _currentProjectDirectory = null;
    private bool _showOpenProjectPopup = false;
    private string _openProjectPath = string.Empty;
    
    // fps rate
    private readonly Queue<float> _frameTimes = new();
    private float _fpsUpdateTimer = 0.0f;
    private float _currentFps = 0.0f;
    private const float FpsUpdateInterval = 0.1f;
    private const int MaxFrameSamples = 60;
    
    public EditorLayer() : base("EditorLayer")
    {
        _showOpenProjectPopup = true;
    }

    public override void OnAttach()
    {
        Logger.Debug("EditorLayer OnAttach.");

        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");
        _sceneState = SceneState.Edit;

        // Initialize 2D camera controller with reasonable settings for editor
        _cameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        
        var frameBufferSpec = new FrameBufferSpecification(1200, 720)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth),
            ])
        };
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);
        
        Graphics3D.Instance.Init();

        CurrentScene.Set(new Scene(""));
        _sceneHierarchyPanel = new SceneHierarchyPanel(CurrentScene.Instance);
        _sceneHierarchyPanel.EntitySelected = EntitySelected;
        _contentBrowserPanel = new ContentBrowserPanel();
        _consolePanel = new ConsolePanel();
        _propertiesPanel = new PropertiesPanel();
        
        // Set scripts directory based on current project directory
        string projectRoot = _currentProjectDirectory ?? Environment.CurrentDirectory;
        string scriptsDir = Path.Combine(projectRoot, "assets", "scripts");
        ScriptEngine.Instance.SetScriptsDirectory(scriptsDir);
        
        Console.WriteLine("✅ Editor initialized successfully!");
        Console.WriteLine("Console panel is now capturing output.");
    }

    private void EntitySelected(Entity entity)
    {
        // Center camera on selected entity
        var transformComponent = entity.GetComponent<TransformComponent>();
        if (transformComponent != default)
        {
            var camera = _cameraController.Camera;
            camera.SetPosition(transformComponent.Translation);
        }
    }

    public override void OnDetach()
    {
        Logger.Debug("EditorLayer OnDetach.");
        _consolePanel?.Dispose();
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        UpdateFpsTracking(timeSpan);
        
        // Resize
        var spec = _frameBuffer.GetSpecification();
        if (_viewportSize is { X: > 0.0f, Y: > 0.0f } && 
            (spec.Width != (uint)_viewportSize.X || spec.Height != (uint)_viewportSize.Y))
        {
            _frameBuffer.Resize((uint)_viewportSize.X, (uint)_viewportSize.Y);
            
            // Update camera aspect ratio when viewport changes
            float aspectRatio = _viewportSize.X / _viewportSize.Y;
            _cameraController = new OrthographicCameraController(_cameraController.Camera, aspectRatio, true);
            
            CurrentScene.Instance.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        }
        
        Graphics2D.Instance.ResetStats();
        Graphics3D.Instance.ResetStats();
        _frameBuffer.Bind();

        Graphics2D.Instance.SetClearColor(_backgroundColor);
        Graphics2D.Instance.Clear();

        _frameBuffer.ClearAttachment(1, -1);

        switch (_sceneState)
        {
            case SceneState.Edit:
            {
                // Update camera controller when viewport is focused
                if (_viewportFocused)
                    _cameraController.OnUpdate(timeSpan);
                
                // Use 2D camera for editor scene rendering
                CurrentScene.Instance.OnUpdateEditor(timeSpan, _cameraController.Camera);
                break;
            }
            case SceneState.Play:
            {
                CurrentScene.Instance.OnUpdateRuntime(timeSpan);
                break;
            }
        }

        // Mouse picking logic
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

        _frameBuffer.Unbind();
    }

    public override void HandleEvent(Event @event)
    {
        //Logger.Debug("EditorLayer OnEvent: {0}", @event);

        // Always handle camera controller events in edit mode
        if (_sceneState == SceneState.Edit)
        {
            _cameraController.OnEvent(@event);
        }
        else
        {
            ScriptEngine.Instance.ProcessEvent(@event);
        }

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
        if (!keyPressedEvent.IsRepeat)
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
                if (control)
                    SaveScene();
                break;
            }
            case (int)KeyCodes.D:
            {
                if (control) 
                    OnDuplicateEntity();
                break;
            }
            case (int)KeyCodes.F:
            {
                if (control)
                    FocusOnSelectedEntity();
                break;
            }
        }
    }

    private void FocusOnSelectedEntity()
    {
        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity != null && selectedEntity.HasComponent<TransformComponent>())
        {
            var transform = selectedEntity.GetComponent<TransformComponent>();
            _cameraController.Camera.SetPosition(transform.Translation);
        }
    }

    public override void OnImGuiRender()
    {
        SubmitUI();
    }

    private void UI_Toolbar()
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
        var icon = _sceneState == SceneState.Edit ? _iconPlay : _iconStop;

        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));

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
        
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
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
            var dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new Vector2(0.0f, 0.0f), dockspaceFlags);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New Project"))
                        _showNewProjectPopup = true;
                    if (ImGui.MenuItem("Open Project"))
                        _showOpenProjectPopup = true;
                    if (ImGui.MenuItem("Exit"))
                        Environment.Exit(0);
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Scene..."))
                {
                    if (ImGui.MenuItem("New", "Ctrl+N"))
                        NewScene();
                    if (ImGui.MenuItem("Open...", "Ctrl+O"))
                        OpenScene();
                    if (ImGui.MenuItem("Save", "Ctrl+S"))
                        SaveScene();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("View"))
                {
                    if (ImGui.MenuItem("Focus on Selected", "Ctrl+F"))
                        FocusOnSelectedEntity();
                    if (ImGui.MenuItem("Reset Camera"))
                        ResetCamera();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Settings"))
                {
                    if (ImGui.MenuItem("Editor Settings"))
                        _showSettings = true;
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Publish"))
                {
                    if (ImGui.MenuItem("Build & Publish"))
                        BuildAndPublish();
                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            if (_showSettings)
            {
                ImGui.Begin("Editor Settings", ref _showSettings, ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Text("Editor Background Color");
                ImGui.ColorEdit4("Background Color", ref _backgroundColor);
                
                // Camera settings
                ImGui.Separator();
                ImGui.Text("Camera Settings");
                
                var cameraPos = _cameraController.Camera.Position;
                if (ImGui.DragFloat3("Camera Position", ref cameraPos, 0.1f))
                {
                    _cameraController.Camera.SetPosition(cameraPos);
                }
                
                var cameraRot = _cameraController.Camera.Rotation;
                if (ImGui.DragFloat("Camera Rotation", ref cameraRot, 1.0f))
                {
                    _cameraController.Camera.SetRotation(cameraRot);
                }
                
                ImGui.End();
            }

            _sceneHierarchyPanel.OnImGuiRender();
            _propertiesPanel.OnImGuiRender();
            _contentBrowserPanel.OnImGuiRender();
            _consolePanel.OnImGuiRender();
            
            ScriptComponentUI.OnImGuiRender();
            
            var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
            _propertiesPanel.SetSelectedEntity(selectedEntity);
            
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
            var camPos = _cameraController.Camera.Position;
            ImGui.Text($"Position: ({camPos.X:F2}, {camPos.Y:F2}, {camPos.Z:F2})");
            ImGui.Text($"Rotation: {_cameraController.Camera.Rotation:F1}°");
            
            // 3D Stats
            var stats3D = Graphics3D.Instance.GetStats();
            ImGui.Text("Renderer3D Stats:");
            ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");

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
                            var path = Marshal.PtrToStringUni(payload.Data);
                            if (path is not null)
                                OpenScene(Path.Combine(AssetsManager.AssetsPath, path));
                        }
                        ImGui.EndDragDropTarget();
                    }
                }

                ImGui.End();
            }

            ImGui.End();
            ImGui.PopStyleVar();

            UI_Toolbar();
            ImGui.End();
        }
        RenderNewProjectPopup();
        RenderOpenProjectPopup();
    }

    private void BuildAndPublish()
    {
        throw new NotImplementedException();
    }

    private void ResetCamera()
    {
        _cameraController.Camera.SetPosition(Vector3.Zero);
        _cameraController.Camera.SetRotation(0.0f);
    }
    
    private void RenderNewProjectPopup()
    {
        if (_showNewProjectPopup)
        {
            ImGui.OpenPopup("New Project");
        }
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        if (ImGui.BeginPopupModal("New Project", ref _showNewProjectPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Project Name:");
            ImGui.InputText("##ProjectName", ref _newProjectName, 100);
            ImGui.Separator();
            bool isValid = !string.IsNullOrWhiteSpace(_newProjectName) && System.Text.RegularExpressions.Regex.IsMatch(_newProjectName, @"^[a-zA-Z0-9_\- ]+$");
            if (!isValid && !string.IsNullOrEmpty(_newProjectName))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped("Project name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.");
                ImGui.PopStyleColor();
            }
            if (!string.IsNullOrEmpty(_newProjectError))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped(_newProjectError);
                ImGui.PopStyleColor();
            }
            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Create", new Vector2(120, 0)))
            {
                var result = CreateNewProject(_newProjectName.Trim());
                if (result)
                {
                    _showNewProjectPopup = false;
                    _newProjectName = string.Empty;
                    _newProjectError = string.Empty;
                }
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showNewProjectPopup = false;
                _newProjectName = string.Empty;
                _newProjectError = string.Empty;
            }
            ImGui.EndPopup();
        }
    }

    private bool CreateNewProject(string projectName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                _newProjectError = "Project name cannot be empty.";
                return false;
            }
        
            var projectDir = Path.Combine(Environment.CurrentDirectory, projectName);
            if (Directory.Exists(projectDir))
            {
                _newProjectError = "A directory with this name already exists.";
                return false;
            }
        
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(Path.Combine(projectDir, "assets"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "scenes"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "textures"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "scripts"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "prefabs")); // Add this line
        
            _currentProjectDirectory = projectDir;
            AssetsManager.SetAssetsPath(Path.Combine(projectDir, "assets"));
            _contentBrowserPanel.SetRootDirectory(AssetsManager.AssetsPath);
            Console.WriteLine($"🆕 Project '{projectName}' created at {projectDir}");
            return true;
        }
        catch (Exception ex)
        {
            _newProjectError = $"Failed to create project: {ex.Message}";
            return false;
        }
    }

    private void RenderOpenProjectPopup()
    {
        if (_showOpenProjectPopup)
        {
            ImGui.OpenPopup("Open Project");
        }
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        if (ImGui.BeginPopupModal("Open Project", ref _showOpenProjectPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter project name:");
            ImGui.InputText("##OpenProjectName", ref _openProjectPath, 100);
            ImGui.Separator();
            bool isValid = !string.IsNullOrWhiteSpace(_openProjectPath) && Directory.Exists(Path.Combine(Environment.CurrentDirectory, _openProjectPath));
            if (!isValid && !string.IsNullOrEmpty(_openProjectPath))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped("Project directory does not exist.");
                ImGui.PopStyleColor();
            }
            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Open", new Vector2(120, 0)))
            {
                var projectDir = Path.Combine(Environment.CurrentDirectory, _openProjectPath.Trim());
                OpenProject(projectDir);
                _showOpenProjectPopup = false;
                _openProjectPath = string.Empty;
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showOpenProjectPopup = false;
                _openProjectPath = string.Empty;
            }
            ImGui.EndPopup();
        }
    }

    private void OpenProject(string projectDir)
    {
        _currentProjectDirectory = projectDir;
        var assetsDir = Path.Combine(projectDir, "assets");
        if (Directory.Exists(assetsDir))
        {
            AssetsManager.SetAssetsPath(assetsDir);
            _contentBrowserPanel.SetRootDirectory(assetsDir);
        }
        else
        {
            AssetsManager.SetAssetsPath(projectDir);
            _contentBrowserPanel.SetRootDirectory(projectDir);
        }
        string scriptsDir = Path.Combine(_currentProjectDirectory, "assets", "scripts");
        ScriptEngine.Instance.SetScriptsDirectory(scriptsDir);
        Console.WriteLine($"📂 Project opened: {projectDir}");
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
        CurrentScene.Set(new Scene(""));
        CurrentScene.Instance.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        ResetCamera();
        Console.WriteLine("📄 New scene created");
    }

    private void OpenScene()
    {
        if (_sceneState != SceneState.Edit)
            OnSceneStop();
        
        const string filePath = "assets/scenes/Example.scene";
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new Exception($"Scene doesnt exists: {filePath}");
        
        CurrentScene.Set(new Scene(filePath));
        CurrentScene.Instance.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);

        SceneSerializer.Deserialize(CurrentScene.Instance, filePath);
        Console.WriteLine($"📂 Scene opened: {filePath}");
    }

    private void OpenScene(string path)
    {
        if (_sceneState != SceneState.Edit)
            OnSceneStop();

        _editorScenePath = path;
        CurrentScene.Set(new Scene(path));
        CurrentScene.Instance.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);

        SceneSerializer.Deserialize(CurrentScene.Instance, path);
        Console.WriteLine($"📂 Scene opened: {path}");
    }
    
    private void SaveScene()
    {
        string sceneDir;
        if (!string.IsNullOrEmpty(_currentProjectDirectory))
        {
            sceneDir = Path.Combine(_currentProjectDirectory, "assets", "scenes");
        }
        else
        {
            sceneDir = Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        }
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);
        _editorScenePath = Path.Combine(sceneDir, "scene.scene");
        if (!string.IsNullOrWhiteSpace(_editorScenePath))
        {
            SceneSerializer.Serialize(CurrentScene.Instance, _editorScenePath);
            Console.WriteLine($"💾 Scene saved: {_editorScenePath}");
        }
    }

    private void OnScenePlay()
    {
        _sceneState = SceneState.Play;
        CurrentScene.Instance.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Console.WriteLine("▶️ Scene play started");
    }

    private void OnSceneStop()
    {
        _sceneState = SceneState.Edit;
        CurrentScene.Instance.OnRuntimeStop();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        
        // Reset camera to reasonable editor position
        ResetCamera();
        
        Console.WriteLine("⏹️ Scene play stopped");
    }

    private void OnDuplicateEntity()
    {
        if (_sceneState != SceneState.Edit)
            return;
        
        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity is not null)
        {
            CurrentScene.Instance.DuplicateEntity(selectedEntity);
            Console.WriteLine($"📋 Entity duplicated: {selectedEntity.Name}");
        }
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