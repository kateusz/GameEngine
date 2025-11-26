using System.Numerics;
using ECS;
using Editor.ComponentEditors;
using Editor.Features;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Input;
using Editor.Panels;
using Editor.Systems;
using Editor.UI.Drawers;
using Editor.Utilities;
using Editor.Windows;
using Engine;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scripting;
using ImGuiNET;
using Serilog;
using ZLinq;

namespace Editor;

public class EditorLayer : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<EditorLayer>();

    private readonly Vector2[] _viewportBounds = new Vector2[2];
    private readonly ISceneHierarchyPanel _sceneHierarchyPanel;
    private readonly IContentBrowserPanel _contentBrowserPanel;
    private readonly IPropertiesPanel _propertiesPanel;
    private readonly IConsolePanel _consolePanel;
    private readonly NewProjectPopup _newProjectPopup;
    private readonly SceneSettingsPopup _sceneSettingsPopup;
    private readonly IProjectManager _projectManager;
    private readonly ISceneContext _sceneContext;
    private readonly ISceneManager _sceneManager;
    private readonly IEditorPreferences _editorPreferences;
    private readonly RendererStatsPanel _rendererStatsPanel;
    private readonly EditorToolbar _editorToolbar;
    private readonly PerformanceMonitorPanel _performanceMonitor;
    private readonly EditorSettingsUI _editorSettingsUI;
    private readonly IGraphics2D _graphics2D;
    private readonly AnimationTimelineWindow _animationTimeline;
    private readonly RecentProjectsWindow _recentProjectsWindow;
    private readonly ViewportRuler _viewportRuler;
    private readonly TileMapPanel _tileMapPanel;
    private readonly ShortcutManager _shortcutManager;
    private readonly KeyboardShortcutsPanel _keyboardShortcutsPanel;
    private readonly IScriptEngine _scriptEngine;
    private readonly ScriptComponentEditor _scriptComponentEditor;
    private readonly DebugSettings _debugSettings;
    private readonly IAssetsManager _assetsManager;

    // TODO: check concurrency
    private readonly HashSet<KeyCodes> _pressedKeys = [];
    private readonly ObjectManipulator _objectManipulator;
    private readonly RulerTool _rulerTool;

    private IOrthographicCameraController _cameraController;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportFocused;
    private Entity? _hoveredEntity;
    private ISystemManager _editorSystems;
    private EditorCameraSystem _editorCameraSystem;
    private Entity _selectedEntity;

    public EditorLayer(IProjectManager projectManager,
        IEditorPreferences editorPreferences, IConsolePanel consolePanel, EditorSettingsUI editorSettingsUI,
        IPropertiesPanel propertiesPanel, ISceneHierarchyPanel sceneHierarchyPanel,
        ISceneContext sceneContext, ISceneManager sceneManager,
        IContentBrowserPanel contentBrowserPanel, EditorToolbar editorToolbar, NewProjectPopup newProjectPopup,
        SceneSettingsPopup sceneSettingsPopup, IGraphics2D graphics2D, RendererStatsPanel rendererStatsPanel,
        AnimationTimelineWindow animationTimeline, RecentProjectsWindow recentProjectsWindow,
        TileMapPanel tileMapPanel, ShortcutManager shortcutManager, KeyboardShortcutsPanel keyboardShortcutsPanel,
        IScriptEngine scriptEngine, DebugSettings debugSettings, PerformanceMonitorPanel performanceMonitor,
        IAssetsManager assetsManager, ObjectManipulator objectManipulator, RulerTool rulerTool,
        ViewportRuler viewportRuler)
    {
        _projectManager = projectManager;
        _consolePanel = consolePanel;
        _editorPreferences = editorPreferences;
        _editorSettingsUI = editorSettingsUI;
        _propertiesPanel = propertiesPanel;
        _sceneHierarchyPanel = sceneHierarchyPanel;
        _sceneContext = sceneContext;
        _sceneManager = sceneManager;
        _contentBrowserPanel = contentBrowserPanel;
        _editorToolbar = editorToolbar;
        _newProjectPopup = newProjectPopup;
        _sceneSettingsPopup = sceneSettingsPopup;
        _graphics2D = graphics2D;
        _rendererStatsPanel = rendererStatsPanel;
        _animationTimeline = animationTimeline;
        _recentProjectsWindow = recentProjectsWindow;
        _tileMapPanel = tileMapPanel;
        _shortcutManager = shortcutManager;
        _keyboardShortcutsPanel = keyboardShortcutsPanel;
        _scriptEngine = scriptEngine;
        _scriptComponentEditor = new ScriptComponentEditor(scriptEngine);
        _debugSettings = debugSettings;
        _performanceMonitor = performanceMonitor;
        _assetsManager = assetsManager;
        _objectManipulator = objectManipulator;
        _rulerTool = rulerTool;
        _viewportRuler = viewportRuler;

        _sceneContext.SceneChanged += newScene => _sceneHierarchyPanel.SetScene(newScene);
        _editorToolbar.OnPlayScene += () => _sceneManager.Play();
        _editorToolbar.OnStopScene += () => _sceneManager.Stop();
        _editorToolbar.OnRestartScene += () => _sceneManager.Restart();
    }

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Debug("EditorLayer OnAttach.");

        // Initialize 2D camera controller with default aspect ratio for editor
        _cameraController = new OrthographicCameraController(DisplayConfig.DefaultAspectRatio);

        var frameBufferSpec = new FrameBufferSpecification(DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth),
            ])
        };
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);

        _sceneManager.New();

        _sceneHierarchyPanel.EntitySelected = EntitySelected;

        _contentBrowserPanel.Init();
        _editorToolbar.Init();

        // Apply settings from preferences
        ApplyEditorSettings();

        // Prefer current project; otherwise default to CWD/assets/scripts
        var scriptsDir = _projectManager.ScriptsDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
        _scriptEngine.SetScriptsDirectory(scriptsDir);

        // Initialize editor systems
        _editorSystems = new SystemManager();
        _editorCameraSystem = new EditorCameraSystem(_cameraController);
        _editorSystems.RegisterSystem(_editorCameraSystem);
        _editorSystems.Initialize();

        // Register keyboard shortcuts
        RegisterShortcuts();

        Logger.Information("✅ Editor initialized successfully!");
        Logger.Information("Console panel is now capturing output.");
    }

    /// <summary>
    /// Applies editor settings from preferences to the editor components.
    /// </summary>
    private void ApplyEditorSettings()
    {
        // Apply debug settings
        _debugSettings.ShowColliderBounds = _editorPreferences.ShowColliderBounds;
        _debugSettings.ShowFPS = _editorPreferences.ShowFPS;

        Logger.Debug("Applied editor settings from preferences");
    }

    /// <summary>
    /// Registers all keyboard shortcuts for the editor.
    /// </summary>
    private void RegisterShortcuts()
    {
        // Editor mode shortcuts (Godot-style)
        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Q, KeyModifiers.ShiftOnly,
            () => _editorToolbar.CurrentMode = EditorMode.Select,
            "Select tool", "Tools"));

        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.W, KeyModifiers.ShiftOnly,
            () => _editorToolbar.CurrentMode = EditorMode.Move,
            "Move tool", "Tools"));

        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.R, KeyModifiers.ShiftOnly,
            () => _editorToolbar.CurrentMode = EditorMode.Scale,
            "Scale tool", "Tools"));

        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.E, KeyModifiers.ShiftOnly,
            () => _editorToolbar.CurrentMode = EditorMode.Ruler,
            "Ruler tool", "Tools"));

        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Escape, KeyModifiers.None,
            () =>
            {
                if (_editorToolbar.CurrentMode == EditorMode.Ruler) _rulerTool.ClearMeasurement();
            },
            "Clear ruler measurement", "Tools"));

        // File operations
        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.N, KeyModifiers.CtrlOnly,
            () => _sceneSettingsPopup.ShowNewScenePopup(),
            "New scene", "File"));

        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.S, KeyModifiers.CtrlOnly,
            () => _sceneManager.Save(_projectManager.ScenesDir),
            "Save scene", "File"));

        _shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.D, KeyModifiers.CtrlOnly,
            () => _sceneManager.DuplicateEntity(_selectedEntity),
            "Duplicate entity", "Edit"));

        Logger.Debug("Registered {Count} keyboard shortcuts", _shortcutManager.Shortcuts.Count);
    }

    private void EntitySelected(Entity entity)
    {
        _selectedEntity = entity;
        // Center camera on selected entity
        if (entity.TryGetComponent<TransformComponent>(out var transformComponent))
        {
            _cameraController.SetPosition(transformComponent.Translation);
        }
    }

    public void OnDetach()
    {
        Logger.Debug("EditorLayer OnDetach.");

        // Shutdown editor systems
        _editorSystems?.ShutdownAll();
        _editorSystems?.Dispose();

        // Dispose current scene to cleanup resources
        _sceneContext.ActiveScene?.Dispose();

        _frameBuffer?.Dispose();
        _consolePanel?.Dispose();
        Log.CloseAndFlush();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _performanceMonitor.Update(timeSpan);
        _animationTimeline.Update((float)timeSpan.TotalSeconds);

        // Resize
        var spec = _frameBuffer.GetSpecification();
        if (_viewportSize is { X: > 0.0f, Y: > 0.0f } &&
            (spec.Width != (uint)_viewportSize.X || spec.Height != (uint)_viewportSize.Y))
        {
            _frameBuffer.Resize((uint)_viewportSize.X, (uint)_viewportSize.Y);

            // Update camera aspect ratio when viewport changes
            float aspectRatio = _viewportSize.X / _viewportSize.Y;
            _cameraController = new OrthographicCameraController(_cameraController.Camera, aspectRatio, true);

            // Update the camera system with the new controller instance
            _editorCameraSystem.SetCameraController(_cameraController);

            _sceneContext.ActiveScene?.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        }

        _graphics2D.ResetStats();
        _frameBuffer.Bind();

        _graphics2D.SetClearColor(_editorSettingsUI.GetBackgroundColor());
        _graphics2D.Clear();

        _frameBuffer.ClearAttachment(1, -1);

        switch (_sceneContext.State)
        {
            case SceneState.Edit:
            {
                // Update viewport focus state for the camera system
                _editorCameraSystem.SetViewportFocused(_viewportFocused);

                // Update editor systems (camera controller, etc.)
                _editorSystems.Update(timeSpan);

                // Use 2D camera for editor scene rendering
                _sceneContext.ActiveScene?.OnUpdateEditor(timeSpan, _cameraController.Camera);
                break;
            }
            case SceneState.Play:
            {
                _sceneContext.ActiveScene?.OnUpdateRuntime(timeSpan);
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
            var entity = _sceneContext.ActiveScene?.Entities.AsValueEnumerable().FirstOrDefault(x => x.Id == entityId);
            _hoveredEntity = entity;
        }

        _frameBuffer.Unbind();
    }

    public void HandleWindowEvent(WindowEvent @event)
    {
        if (_sceneContext.State == SceneState.Edit)
        {
            _cameraController.OnEvent(@event);
        }
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        // Track key state for shortcuts
        switch (windowEvent)
        {
            case KeyPressedEvent kpe:
                _pressedKeys.Add(kpe.KeyCode);
                OnKeyPressed(kpe);
                break;
            case KeyReleasedEvent kre:
                _pressedKeys.Remove(kre.KeyCode);
                break;
        }

        if (_sceneContext.State == SceneState.Edit)
        {
            _cameraController.OnEvent(windowEvent);
        }
        else
        {
            _scriptEngine.ProcessEvent(windowEvent);
        }
    }

    private void OnKeyPressed(KeyPressedEvent keyPressedEvent)
    {
        // Shortcuts - ignore key repeats
        if (keyPressedEvent.IsRepeat)
            return;

        var control = _pressedKeys.Contains(KeyCodes.LeftControl) ||
                      _pressedKeys.Contains(KeyCodes.RightControl);
        var shift = _pressedKeys.Contains(KeyCodes.LeftShift) ||
                    _pressedKeys.Contains(KeyCodes.RightShift);
        var alt = _pressedKeys.Contains(KeyCodes.LeftAlt) ||
                  _pressedKeys.Contains(KeyCodes.RightAlt);

        // Delegate to shortcut manager
        var handled = _shortcutManager.HandleKeyPress(
            keyPressedEvent.KeyCode,
            control,
            shift,
            alt);

        if (handled)
        {
            keyPressedEvent.IsHandled = true;
        }
    }

    public void Draw()
    {
        SubmitUI();
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
                        _newProjectPopup.ShowNewProjectPopup();
                    if (ImGui.MenuItem("Open Project"))
                        _newProjectPopup.ShowOpenProjectPopup();

                    ImGui.Separator();

                    if (ImGui.MenuItem("Show Recent Projects"))
                        _recentProjectsWindow.Show();

                    // Add Recent Projects submenu
                    if (ImGui.BeginMenu("Recent Projects"))
                    {
                        var recentProjects = _editorPreferences.GetRecentProjects();

                        if (recentProjects.Count == 0)
                        {
                            ImGui.MenuItem("(No recent projects)", false);
                        }
                        else
                        {
                            foreach (var recent in recentProjects)
                            {
                                var displayName = $"{recent.Name}";
                                if (ImGui.MenuItem(displayName))
                                {
                                    if (_projectManager.TryOpenProject(recent.Path, out var error))
                                    {
                                        _contentBrowserPanel.SetRootDirectory(_assetsManager.AssetsPath);
                                    }
                                    else
                                    {
                                        Logger.Warning("Failed to open recent project {Path}: {Error}", recent.Path,
                                            error);
                                    }
                                }

                                // Show tooltip with full path
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text(recent.Path);
                                    ImGui.Text($"Last opened: {recent.LastOpened:yyyy-MM-dd HH:mm}");
                                    ImGui.EndTooltip();
                                }
                            }

                            ImGui.Separator();
                            if (ImGui.MenuItem("Clear Recent Projects"))
                            {
                                _editorPreferences.ClearRecentProjects();
                            }
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.Separator();
                    if (ImGui.MenuItem("Exit"))
                        Environment.Exit(0);
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Scene..."))
                {
                    if (ImGui.MenuItem("New", "Ctrl+N"))
                        _sceneSettingsPopup.ShowNewScenePopup();
                    if (ImGui.MenuItem("Save", "Ctrl+S"))
                        _sceneManager.Save(_projectManager.ScenesDir);
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("View"))
                {
                    if (ImGui.MenuItem("Reset Camera"))
                        ResetCamera();

                    ImGui.Separator();

                    if (ImGui.MenuItem("Show Rulers", null, _viewportRuler.Enabled))
                        _viewportRuler.Enabled = !_viewportRuler.Enabled;
                    if (ImGui.MenuItem("Show Stats", null, _rendererStatsPanel.IsVisible))
                        _rendererStatsPanel.IsVisible = !_rendererStatsPanel.IsVisible;

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Settings"))
                {
                    if (ImGui.MenuItem("Editor Settings"))
                        _editorSettingsUI.Show();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Help"))
                {
                    if (ImGui.MenuItem("Keyboard Shortcuts"))
                        _keyboardShortcutsPanel.Show();
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

            _sceneHierarchyPanel.Draw();
            _propertiesPanel.Draw();
            _contentBrowserPanel.Draw();
            _consolePanel.Draw();

            _scriptComponentEditor.Draw();
            _recentProjectsWindow.Draw();
            _keyboardShortcutsPanel.Draw();

            var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
            _propertiesPanel.SetSelectedEntity(selectedEntity);

            // Render Stats window if visible
            var hoveredEntityName = _hoveredEntity?.Name ?? "None";
            var camPos = _cameraController.Camera.Position;
            var camRotation = _cameraController.Camera.Rotation;
            _rendererStatsPanel.Draw(hoveredEntityName, camPos, camRotation, () => _performanceMonitor.RenderUI());

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

            ImGui.Begin("Viewport");
            {
                _viewportFocused = ImGui.IsWindowFocused();

                var viewportPanelSize = ImGui.GetContentRegionAvail();

                var textureId = _frameBuffer.GetColorAttachmentRendererId();
                var texturePointer = new IntPtr(textureId);
                ImGui.Image(texturePointer, new Vector2(viewportPanelSize.X, viewportPanelSize.Y), new Vector2(0, 1),
                    new Vector2(1, 0));

                // Get the actual screen position and size of the rendered image
                _viewportBounds[0] = ImGui.GetItemRectMin();
                _viewportBounds[1] = ImGui.GetItemRectMax();
                _viewportSize = _viewportBounds[1] - _viewportBounds[0];

                // Handle scene file drops
                var sceneValidator = DragDropDrawer.CreateExtensionValidator(
                    [".scene"],
                    checkFileExists: false);

                DragDropDrawer.HandleFileDropTarget(
                    DragDropDrawer.ContentBrowserItemPayload,
                    sceneValidator,
                    onDropped: path => { _sceneManager.Open(Path.Combine(_assetsManager.AssetsPath, path)); });

                // Handle entity selection on mouse click in viewport
                if (ImGui.IsWindowHovered())
                {
                    var currentMode = _editorToolbar.CurrentMode;

                    // Prepare local mouse coordinates relative to the viewport
                    var globalMousePos = ImGui.GetMousePos();
                    var localMousePos = new Vector2(globalMousePos.X - _viewportBounds[0].X,
                        globalMousePos.Y - _viewportBounds[0].Y);

                    // Handle Ruler mode
                    if (currentMode == EditorMode.Ruler)
                    {
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            _rulerTool.StartMeasurement(localMousePos, _viewportBounds, _cameraController.Camera);
                        }

                        if (_rulerTool.IsMeasuring)
                        {
                            _rulerTool.UpdateMeasurement(localMousePos, _viewportBounds, _cameraController.Camera);
                        }

                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            _rulerTool.EndMeasurement();
                        }
                    }
                    else
                    {
                        // Start dragging
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            if (_hoveredEntity != null)
                            {
                                _sceneHierarchyPanel.SetSelectedEntity(_hoveredEntity);

                                if (currentMode == EditorMode.Move || currentMode == EditorMode.Scale)
                                {
                                    // Start manipulation
                                    _objectManipulator.StartDrag(_hoveredEntity, localMousePos, _viewportBounds,
                                        _cameraController.Camera);
                                }
                                else
                                {
                                    // Select mode - just focus on entity
                                    EntitySelected(_hoveredEntity);
                                }
                            }
                        }

                        // Update dragging
                        if (_objectManipulator.IsDragging && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            _objectManipulator.UpdateDrag(currentMode, localMousePos, _viewportBounds,
                                _cameraController.Camera);
                        }

                        // End dragging
                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            _objectManipulator.EndDrag();
                        }
                    }
                }

                // Render viewport rulers
                var cameraPos = new Vector2(_cameraController.Camera.Position.X, _cameraController.Camera.Position.Y);
                var orthoSize = _cameraController.ZoomLevel;
                var zoom = _viewportSize.Y / (orthoSize * 2.0f); // pixels per unit
                _viewportRuler.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);

                // Render ruler tool measurements
                _rulerTool.Render(_viewportBounds, _cameraController.Camera);
            }

            // Render windows that should dock to Viewport
            var viewportDockId = ImGui.GetWindowDockID();
            _animationTimeline.OnImGuiRender(viewportDockId);
            _tileMapPanel.OnImGuiRender(viewportDockId);

            ImGui.End();

            ImGui.End();
            ImGui.PopStyleVar();

            _editorToolbar.Render();
            ImGui.End();
        }

        // Render popups outside the dockspace window
        _editorSettingsUI.Render();
        _newProjectPopup.Render();
        _sceneSettingsPopup.Render();
    }

    private void BuildAndPublish()
    {
        throw new NotImplementedException();
    }

    private void ResetCamera()
    {
        _cameraController.SetPosition(Vector3.Zero);
        _cameraController.SetRotation(0.0f);
        // Reset zoom to default
        _cameraController.SetZoom(CameraConfig.DefaultZoomLevel);
    }
}