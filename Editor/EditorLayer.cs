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
    private ConsolePanel _consolePanel; // Added console panel
    private EditorCamera _editorCamera;
    private Entity? _hoveredEntity;
    private Texture2D _iconPlay;
    private Texture2D _iconStop;
    private SceneState _sceneState;
    private string? _editorScenePath;
    private Vector4 _backgroundColor = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
    private bool _showSettings = false;
    private bool _showNewProjectPopup = false;
    private string _newProjectName = string.Empty;
    private string _newProjectError = string.Empty;
    private string? _currentProjectDirectory = null;
    private bool _showOpenProjectPopup = false;
    private string _openProjectPath = string.Empty;
    
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
        
        Renderer3D.Instance.Init();

        _activeScene = new Scene("");
        _editorCamera = new EditorCamera(30.0f, 1280.0f / 720.0f, 0.1f, 1000.0f);
        _sceneHierarchyPanel = new SceneHierarchyPanel(_activeScene);
        _sceneHierarchyPanel.EntitySelected = EntitySelected;
        _contentBrowserPanel = new ContentBrowserPanel();
        _consolePanel = new ConsolePanel(); // Initialize console panel
        
        // Set scripts directory based on current project directory
        string projectRoot = _currentProjectDirectory ?? Environment.CurrentDirectory;
        string scriptsDir = Path.Combine(projectRoot, "assets", "scripts");
        ScriptEngine.Instance.SetScriptsDirectory(scriptsDir);
        
        ScriptEngine.Instance.Initialize(_activeScene);
        
        // Add some initial console messages to demonstrate functionality
        Console.WriteLine("✅ Editor initialized successfully!");
        Console.WriteLine("Console panel is now capturing output.");
    }

    private void EntitySelected(Entity entity)
    {
        // center camera
        var transformComponent = entity.GetComponent<TransformComponent>();
        //_editorCamera.CenterToPos(transformComponent.Translation);
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
        _consolePanel?.Dispose(); // Clean up console panel
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

        RendererCommand.SetClearColor(_backgroundColor);
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
                
                // should it be called here?
                //ScriptEngine.Instance.Update(timeSpan);
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
        
        if (_sceneState == SceneState.Edit)
        {
            _editorCamera.OnEvent(@event);
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
                    {
                        Environment.Exit(0); // Exit the application
                    }
                    ImGui.EndMenu();
                }

                // New Scene menu for scene operations
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

                if (ImGui.BeginMenu("Settings"))
                {
                    if (ImGui.MenuItem("Editor Settings"))
                        _showSettings = true;
                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            if (_showSettings)
            {
                ImGui.Begin("Editor Settings", ref _showSettings, ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Text("Editor Background Color");
                ImGui.ColorEdit4("Background Color", ref _backgroundColor);
                ImGui.End();
            }

            _sceneHierarchyPanel.OnImGuiRender();
            _contentBrowserPanel.OnImGuiRender();
            _consolePanel.OnImGuiRender(); // Render console panel
            
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
            ImGui.Text("Editor Camera:");
            ImGui.Text($"X: {stats.EditorCameraX}");
            ImGui.Text($"Y: {stats.EditorCameraY}");
            ImGui.Text($"Z: {stats.EditorCameraZ}");
            
            // 3D Stats
            var stats3D = Renderer3D.Instance.GetStats();
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
                if (cameraEntity != null)
                {
                    var cameraComponent = cameraEntity.GetComponent<CameraComponent>();
                    var camera = cameraComponent.Camera;
                    var cameraProjection = camera.Projection;

                    Matrix4x4.Invert(cameraEntity.GetComponent<TransformComponent>().GetTransform(),
                        out var cameraView);

                    // Entity transform
                    var transformComponent = selectedEntity.GetComponent<TransformComponent>();
                    var transform = transformComponent.GetTransform();
                }
            }

            ImGui.End();
            ImGui.PopStyleVar();

            UI_Toolbar();

            ImGui.End();
        }
        RenderNewProjectPopup();
        RenderOpenProjectPopup();
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
            var currentDir = Environment.CurrentDirectory;
            var projectDir = Path.Combine(currentDir, projectName);
            if (Directory.Exists(projectDir))
            {
                _newProjectError = $"A directory named '{projectName}' already exists.";
                return false;
            }
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(Path.Combine(projectDir, "assets"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "scenes"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "textures"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "scripts"));
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
        _activeScene = new Scene("");
        _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_activeScene);
        Console.WriteLine("📄 New scene created");
    }

    private void OpenScene()
    {
        if (_sceneState != SceneState.Edit)
            OnSceneStop();
        
        // TODO: from configuration
        const string filePath = "assets/scenes/Example.scene";
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new Exception($"Scene doesnt exists: {filePath}");
        
        _activeScene = new Scene(filePath);
        _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_activeScene);

        SceneSerializer.Deserialize(_activeScene, filePath);
        Console.WriteLine($"📂 Scene opened: {filePath}");
    }

    private void OpenScene(string path)
    {
        if (_sceneState != SceneState.Edit)
            OnSceneStop();

        _editorScenePath = path;
        _activeScene = new Scene(path);
        _activeScene.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_activeScene);

        SceneSerializer.Deserialize(_activeScene, path);
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
            SceneSerializer.Serialize(_activeScene, _editorScenePath);
            Console.WriteLine($"💾 Scene saved: {_editorScenePath}");
        }
    }

    private void OnScenePlay()
    {
        _sceneState = SceneState.Play;
        _activeScene.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(_activeScene);
        Console.WriteLine("▶️ Scene play started");
    }

    private void OnSceneStop()
    {
        _sceneState = SceneState.Edit;
        _activeScene.OnRuntimeStop();
        _sceneHierarchyPanel.SetContext(_activeScene);
        
        // Reinitialize ScriptEngine with current scene
        ScriptEngine.Instance.Initialize(_activeScene);
        Console.WriteLine("⏹️ Scene play stopped");
    }

    private void OnDuplicateEntity()
    {
        if (_sceneState != SceneState.Edit)
            return;
        
        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity is not null)
        {
            _activeScene.DuplicateEntity(selectedEntity);
            Console.WriteLine($"📋 Entity duplicated: {selectedEntity.Name}");
        }
    }
}