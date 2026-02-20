using System.Numerics;
using ECS;
using ECS.Systems;
using Editor.ComponentEditors;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Input;
using Editor.Panels;
using Editor.Systems;
using Editor.UI.Drawers;
using Editor.Features.Viewport;
using Editor.Features.Viewport.Tools;
using Editor.Publisher;
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

public class EditorLayer(
    IProjectManager projectManager,
    IEditorPreferences editorPreferences,
    IConsolePanel consolePanel,
    EditorSettingsUI editorSettingsUI,
    IPropertiesPanel propertiesPanel,
    ISceneHierarchyPanel sceneHierarchyPanel,
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    IContentBrowserPanel contentBrowserPanel,
    SceneToolbar sceneToolbar,
    NewProjectPopup newProjectPopup,
    SceneSettingsPopup sceneSettingsPopup,
    IGraphics2D graphics2D,
    RendererStatsPanel rendererStatsPanel,
    IAnimationTimelinePanel animationTimeline,
    RecentProjectsPanel recentProjectsPanel,
    ShortcutManager shortcutManager,
    KeyboardShortcutsPanel keyboardShortcutsPanel,
    IScriptEngine scriptEngine,
    ScriptComponentEditor scriptComponentEditor,
    DebugSettings debugSettings,
    PerformanceMonitorPanel performanceMonitor,
    IAssetsManager assetsManager,
    ViewportToolManager viewportToolManager,
    ViewportRuler viewportRuler,
    IFrameBufferFactory frameBufferFactory,
    PublishSettingsUI publishSettingsUI) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<EditorLayer>();

    private readonly Vector2[] _viewportBounds = new Vector2[2];

    // TODO: check concurrency
    private readonly HashSet<KeyCodes> _pressedKeys = [];

    private IOrthographicCameraController _cameraController;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportFocused;
    private Entity? _hoveredEntity;
    private ISystemManager _editorSystems;
    private EditorCameraSystem _editorCameraSystem;
    private Entity _selectedEntity;

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Debug("EditorLayer OnAttach.");

        sceneContext.SceneChanged += newScene => sceneHierarchyPanel.SetScene(newScene);
        sceneToolbar.OnPlayScene += () => sceneManager.Play();
        sceneToolbar.OnStopScene += () => sceneManager.Stop();
        sceneToolbar.OnRestartScene += () => sceneManager.Restart();

        // Initialize 2D camera controller with default aspect ratio for editor
        _cameraController = new OrthographicCameraController(DisplayConfig.DefaultAspectRatio);
        _frameBuffer = frameBufferFactory.Create();

        sceneManager.New();

        sceneHierarchyPanel.EntitySelected = EntitySelected;
        // Viewport selection only updates selection state - don't move camera since entity is already visible
        viewportToolManager.SubscribeToEntitySelection(entity => _selectedEntity = entity);

        contentBrowserPanel.Init();
        sceneToolbar.Init();

        // Apply settings from preferences
        ApplyEditorSettings();

        // Prefer current project; otherwise default to CWD/assets/scripts
        var scriptsDir = projectManager.ScriptsDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
        scriptEngine.SetScriptsDirectory(scriptsDir);

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
        debugSettings.ShowColliderBounds = editorPreferences.ShowColliderBounds;
        debugSettings.ShowFPS = editorPreferences.ShowFPS;

        Logger.Debug("Applied editor settings from preferences");
    }

    /// <summary>
    /// Registers all keyboard shortcuts for the editor.
    /// </summary>
    private void RegisterShortcuts()
    {
        // Editor mode shortcuts (Godot-style)
        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Q, KeyModifiers.ShiftOnly,
            () => sceneToolbar.CurrentMode = EditorMode.Select,
            "Select tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.W, KeyModifiers.ShiftOnly,
            () => sceneToolbar.CurrentMode = EditorMode.Move,
            "Move tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.R, KeyModifiers.ShiftOnly,
            () => sceneToolbar.CurrentMode = EditorMode.Scale,
            "Scale tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.E, KeyModifiers.ShiftOnly,
            () => sceneToolbar.CurrentMode = EditorMode.Ruler,
            "Ruler tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Escape, KeyModifiers.None,
            () =>
            {
                if (sceneToolbar.CurrentMode == EditorMode.Ruler)
                {
                    var rulerTool = viewportToolManager.GetTool<RulerTool>();
                    rulerTool?.ClearMeasurement();
                }
            },
            "Clear ruler measurement", "Tools"));

        // File operations
        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.N, KeyModifiers.CtrlOnly,
            () => sceneSettingsPopup.ShowNewScenePopup(),
            "New scene", "File"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.S, KeyModifiers.CtrlOnly,
            () => sceneManager.Save(projectManager.ScenesDir),
            "Save scene", "File"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.D, KeyModifiers.CtrlOnly,
            () => sceneManager.DuplicateEntity(_selectedEntity),
            "Duplicate entity", "Edit"));

        // Navigation
        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.R, KeyModifiers.CtrlOnly,
            ResetCamera,
            "Reset camera", "Navigation"));

        Logger.Debug("Registered {Count} keyboard shortcuts", shortcutManager.Shortcuts.Count);
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
        sceneContext.ActiveScene?.Dispose();

        _frameBuffer?.Dispose();
        consolePanel?.Dispose();
        Log.CloseAndFlush();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        performanceMonitor.Update(timeSpan);
        animationTimeline.Update((float)timeSpan.TotalSeconds);

        // Resize
        var spec = _frameBuffer.GetSpecification();
        if (_viewportSize is { X: > 0.0f, Y: > 0.0f } &&
            (spec.Width != (uint)_viewportSize.X || spec.Height != (uint)_viewportSize.Y))
        {
            _frameBuffer.Resize((uint)_viewportSize.X, (uint)_viewportSize.Y);

            // Update camera aspect ratio when viewport changes
            var aspectRatio = _viewportSize.X / _viewportSize.Y;
            _cameraController = new OrthographicCameraController(_cameraController.Camera, aspectRatio, true);

            // Update the camera system with the new controller instance
            _editorCameraSystem.SetCameraController(_cameraController);

            sceneContext.ActiveScene?.OnViewportResize((uint)_viewportSize.X, (uint)_viewportSize.Y);
        }

        graphics2D.ResetStats();
        _frameBuffer.Bind();

        graphics2D.SetClearColor(editorSettingsUI.GetBackgroundColor());
        graphics2D.Clear();

        _frameBuffer.ClearAttachment(1, -1);

        switch (sceneContext.State)
        {
            case SceneState.Edit:
            {
                // Update viewport focus state for the camera system
                _editorCameraSystem.SetViewportFocused(_viewportFocused);

                // Update editor systems (camera controller, etc.)
                _editorSystems.Update(timeSpan);

                // Use 2D camera for editor scene rendering
                sceneContext.ActiveScene?.OnUpdateEditor(timeSpan, _cameraController.Camera);
                break;
            }
            case SceneState.Play:
            {
                sceneContext.ActiveScene?.OnUpdateRuntime(timeSpan);
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
            var entity = sceneContext.ActiveScene?.Entities.AsValueEnumerable().FirstOrDefault(x => x.Id == entityId);
            _hoveredEntity = entity;
        }

        _frameBuffer.Unbind();
    }

    public void HandleWindowEvent(WindowEvent @event)
    {
        if (sceneContext.State == SceneState.Edit)
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

        if (sceneContext.State == SceneState.Edit)
        {
            _cameraController.OnEvent(windowEvent);
        }
        else
        {
            scriptEngine.ProcessEvent(windowEvent);
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
        var handled = shortcutManager.HandleKeyPress(
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
                        newProjectPopup.ShowNewProjectPopup();
                    if (ImGui.MenuItem("Open Project"))
                        newProjectPopup.ShowOpenProjectPopup();

                    ImGui.Separator();

                    if (ImGui.MenuItem("Show Recent Projects"))
                        recentProjectsPanel.Show();

                    // Add Recent Projects submenu
                    if (ImGui.BeginMenu("Recent Projects"))
                    {
                        var recentProjects = editorPreferences.GetRecentProjects();

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
                                    if (projectManager.TryOpenProject(recent.Path, out var error))
                                    {
                                        contentBrowserPanel.SetRootDirectory(assetsManager.AssetsPath);
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
                                editorPreferences.ClearRecentProjects();
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
                        sceneSettingsPopup.ShowNewScenePopup();
                    if (ImGui.MenuItem("Save", "Ctrl+S"))
                        sceneManager.Save(projectManager.ScenesDir);
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("View"))
                {
                    if (ImGui.MenuItem("Reset Camera"))
                        ResetCamera();

                    ImGui.Separator();

                    if (ImGui.MenuItem("Show Rulers", null, viewportRuler.Enabled))
                        viewportRuler.Enabled = !viewportRuler.Enabled;
                    if (ImGui.MenuItem("Show Stats", null, rendererStatsPanel.IsVisible))
                        rendererStatsPanel.IsVisible = !rendererStatsPanel.IsVisible;

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Settings"))
                {
                    if (ImGui.MenuItem("Editor Settings"))
                        editorSettingsUI.Show();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Help"))
                {
                    if (ImGui.MenuItem("Keyboard Shortcuts"))
                        keyboardShortcutsPanel.Show();
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

            sceneHierarchyPanel.Draw();
            propertiesPanel.Draw();
            contentBrowserPanel.Draw();
            consolePanel.Draw();

            scriptComponentEditor.Draw();
            recentProjectsPanel.Draw();
            keyboardShortcutsPanel.Draw();

            var selectedEntity = sceneHierarchyPanel.GetSelectedEntity();
            propertiesPanel.SetSelectedEntity(selectedEntity);

            // Render Stats window if visible
            var hoveredEntityName = _hoveredEntity?.Name ?? "None";
            var camPos = _cameraController.Camera.Position;
            var camRotation = _cameraController.Camera.Rotation;
            rendererStatsPanel.Draw(hoveredEntityName, camPos, camRotation, () => performanceMonitor.RenderUI());

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
                    onDropped: path => { sceneManager.Open(Path.Combine(assetsManager.AssetsPath, path)); });

                // Handle viewport interactions via ViewportToolManager
                if (ImGui.IsWindowHovered())
                {
                    var currentMode = sceneToolbar.CurrentMode;

                    // Prepare local mouse coordinates relative to the viewport
                    var globalMousePos = ImGui.GetMousePos();
                    var localMousePos = new Vector2(globalMousePos.X - _viewportBounds[0].X,
                        globalMousePos.Y - _viewportBounds[0].Y);

                    // Update active tool based on toolbar mode
                    viewportToolManager.SetMode(currentMode);

                    // Update hovered entity for selection tool
                    viewportToolManager.SetHoveredEntity(_hoveredEntity);

                    // Update target entity for manipulation tools
                    var currentSelection = sceneHierarchyPanel.GetSelectedEntity();
                    if (currentSelection != null)
                    {
                        viewportToolManager.SetTargetEntity(currentSelection);
                    }

                    // Handle mouse events through tool manager
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        viewportToolManager.HandleMouseDown(localMousePos, _viewportBounds, _cameraController.Camera);

                        // Update hierarchy panel selection when entity is clicked
                        if (_hoveredEntity != null && currentMode != EditorMode.Ruler)
                        {
                            sceneHierarchyPanel.SetSelectedEntity(_hoveredEntity);
                        }
                    }

                    if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        viewportToolManager.HandleMouseMove(localMousePos, _viewportBounds, _cameraController.Camera);
                    }

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        viewportToolManager.HandleMouseUp(localMousePos, _viewportBounds, _cameraController.Camera);
                    }
                }

                // Render viewport rulers
                var cameraPos = new Vector2(_cameraController.Camera.Position.X, _cameraController.Camera.Position.Y);
                var orthoSize = _cameraController.ZoomLevel;
                var zoom = _viewportSize.Y / (orthoSize * 2.0f); // pixels per unit
                viewportRuler.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);

                // Render active tool overlays (measurements, gizmos, etc.)
                viewportToolManager.RenderActiveTool(_viewportBounds, _cameraController.Camera);
            }

            // Render windows that should dock to Viewport
            var viewportDockId = ImGui.GetWindowDockID();
            animationTimeline.OnImGuiRender(viewportDockId);

            ImGui.End();

            ImGui.End();
            ImGui.PopStyleVar();

            sceneToolbar.Render();
            ImGui.End();
        }

        // Render popups outside the dockspace window
        editorSettingsUI.Render();
        newProjectPopup.Render();
        sceneSettingsPopup.Render();
        publishSettingsUI.Render();
    }

    private void BuildAndPublish() => publishSettingsUI.ShowPublishModal();

    private void ResetCamera()
    {
        _cameraController.SetPosition(Vector3.Zero);
        _cameraController.SetRotation(0.0f);
        // Reset zoom to default
        _cameraController.SetZoom(CameraConfig.DefaultZoomLevel);
    }
}
