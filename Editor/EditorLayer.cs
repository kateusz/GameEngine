using DryIoc;
using Editor.Components;
using Editor.State;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.Renderer;
using Engine.Scene;
using ImGuiNET;
using NLog;

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private EditorState _editorState;
    private IEditorViewport _viewport;
    private IEditorUIRenderer _uiRenderer;
    private IEditorPerformanceMonitor _performanceMonitor;
    private Workspace _workspace;
    private ProjectController _projectController;
    private SceneController _sceneController;
    private EditorInputHandler _inputHandler;

    private readonly IContainer _container;
    
    public EditorLayer(IContainer container) : base("EditorLayer")
    {
        _container = container;
    }

    public override void OnAttach()
    {
        Logger.Debug("EditorLayer OnAttach.");
        
        CurrentScene.Set(new Scene(""));
        
        InitializeComponents();
        InitializeControllers();
        WireUpEvents();
        InitializeAssets();
        
        Console.WriteLine("✅ Editor initialized successfully!");
        Console.WriteLine("Console panel is now capturing output.");
    }

    private void InitializeComponents()
    {
        _editorState = _container.Resolve<EditorState>();
        
        _viewport = _container.Resolve<IEditorViewport>();
        _viewport.Initialize(1200, 720);

        _performanceMonitor = _container.Resolve<IEditorPerformanceMonitor>();
        _uiRenderer = _container.Resolve<IEditorUIRenderer>();
    }

    private void InitializeControllers()
    {
        _workspace = _container.Resolve<Workspace>();
        _projectController = _container.Resolve<ProjectController>();
        _sceneController = _container.Resolve<SceneController>();
        _inputHandler = _container.Resolve<EditorInputHandler>();
        
        _projectController.Initialize();
        UpdateScriptsDirectory();
    }

    private void WireUpEvents()
    {
        // Selection management
        _editorState.SelectionChanged += (entity) => 
        {
            _inputHandler.FocusOnEntity(entity);
            _workspace.SceneHierarchyPanel.SetSelectedEntity(entity);
        };
        
        // Workspace events - direct connections
        _workspace.OnNewProjectRequested += () => _projectController.ShowNewProjectDialog();
        _workspace.OnOpenProjectRequested += () => _projectController.ShowOpenProjectDialog();
        _workspace.OnNewSceneRequested += () => _sceneController.NewScene();
        _workspace.OnOpenSceneRequested += () => _sceneController.OpenScene();
        _workspace.OnSaveSceneRequested += () => _sceneController.SaveScene(_projectController.CurrentProjectDirectory);
        _workspace.OnFocusOnSelectedRequested += FocusOnSelectedEntity;
        _workspace.OnResetCameraRequested += () => _inputHandler.ResetCamera();
        _workspace.OnBuildAndPublishRequested += BuildAndPublish;
        
        // Input handler events - direct connections
        _inputHandler.OnNewSceneRequested += () => _sceneController.NewScene();
        _inputHandler.OnOpenSceneRequested += () => _sceneController.OpenScene();
        _inputHandler.OnSaveSceneRequested += () => _sceneController.SaveScene(_projectController.CurrentProjectDirectory);
        _inputHandler.OnDuplicateEntityRequested += () => _sceneController.DuplicateEntity(_editorState.SelectedEntity);
        _inputHandler.OnFocusOnSelectedRequested += FocusOnSelectedEntity;
        _inputHandler.OnLeftMousePressed += HandleEntitySelection;
        
        // Project manager events
        _projectController.OnProjectCreated += OnProjectChanged;
        _projectController.OnProjectOpened += OnProjectChanged;
        
        // Scene controller events
        _sceneController.OnSceneChanged += () => {
            _sceneController.OnViewportResize((uint)_editorState.ViewportState.ViewportSize.X, (uint)_editorState.ViewportState.ViewportSize.Y);
            _workspace.UpdateSceneContext();
        };
        _sceneController.OnSceneStateChanged += (state) => {
            _workspace.UpdateSceneContext();
        };
        
        // UI entity selection
        _workspace.SetEntitySelectedCallback((entity) => _editorState.SelectEntity(entity));
    }

    private void InitializeAssets()
    {
        
    }

    private void HandleEntitySelection()
    {
        if (_editorState.ViewportState.ViewportHovered && _viewport.HoveredEntity != null && 
            !InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftAlt))
        {
            _editorState.SelectEntity(_viewport.HoveredEntity);
        }
    }

    private void FocusOnSelectedEntity()
    {
        var selectedEntity = _editorState.SelectedEntity;
        _inputHandler.FocusOnEntity(selectedEntity);
    }

    private void OnProjectChanged(string projectDir)
    {
        _workspace.ContentBrowserPanel.SetRootDirectory(Path.Combine(projectDir, "assets"));
        UpdateScriptsDirectory();
    }
    
    private void UpdateScriptsDirectory()
    {
        string scriptsDir = _projectController.GetScriptsDirectory();
        _projectController.SetScriptsDirectory(scriptsDir);
    }

    private void BuildAndPublish()
    {
        throw new NotImplementedException();
    }
    
    private void RenderScene(TimeSpan timeSpan)
    {
        Graphics2D.Instance.ResetStats();
        Graphics3D.Instance.ResetStats();
        
        _viewport.BindFrameBuffer();
        
        Graphics2D.Instance.SetClearColor(_workspace.BackgroundColor);
        Graphics2D.Instance.Clear();
        _viewport.ClearAttachment(1, -1);
        
        CurrentScene.Instance.OnUpdateEditor(timeSpan, _inputHandler.CameraController.Camera);
        
        _viewport.UnbindFrameBuffer();
    }

    public override void OnDetach()
    {
        Logger.Debug("EditorLayer OnDetach.");
        _workspace?.Dispose();
        _viewport?.Dispose();
        _uiRenderer?.Dispose();
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        _performanceMonitor.UpdateFpsTracking(timeSpan);
        _viewport.HandleResize();
        
        _inputHandler.OnUpdate(timeSpan, _sceneController.CurrentState);
        
        _editorState.ViewportState.UpdateMousePosition(ImGui.GetMousePos());
        
        RenderScene(timeSpan);
        
        _viewport.UpdateMousePicking();
        _editorState.HoveredEntity = _viewport.HoveredEntity;
        
        float aspectRatio = _editorState.ViewportState.ViewportSize.X / _editorState.ViewportState.ViewportSize.Y;
        if (aspectRatio > 0)
        {
            _inputHandler.UpdateCameraAspectRatio(aspectRatio);
        }
        
        _sceneController.OnViewportResize((uint)_editorState.ViewportState.ViewportSize.X, (uint)_editorState.ViewportState.ViewportSize.Y);
    }

    public override void HandleEvent(Event @event) => _inputHandler.HandleEvent(@event, _sceneController.CurrentState);

    public override void OnImGuiRender() => _uiRenderer.RenderMainUI();
}