using System.Numerics;
using ECS;
using Editor.ComponentEditors;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Input;
using Editor.Panels;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Editor.Features.Viewport;
using Editor.Features.Viewport.Tools;
using Editor.Publisher;
using Engine.Core;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Math;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using Engine.Scripting;
using ImGuiNET;
using Serilog;
using ZLinq;

namespace Editor;

public class EditorLayer(
    IProjectManager projectManager,
    IEditorPreferences editorPreferences,
    EditorSettingsUI editorSettingsUI,
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    NewProjectPopup newProjectPopup,
    SceneSettingsPopup sceneSettingsPopup,
    IGraphics2D graphics2D,
    ShortcutManager shortcutManager,
    IScriptEngine scriptEngine,
    ScriptComponentEditor scriptComponentEditor,
    DebugSettings debugSettings,
    IFrameBufferFactory frameBufferFactory,
    IHdrToneMapper hdrToneMapper,
    PublishSettingsUI publishSettingsUI,
    IContentScaleProvider contentScaleProvider,
    EditorPanels panels,
    ViewportComponents viewport) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<EditorLayer>();

    private readonly Vector2[] _viewportBounds = new Vector2[2];

    // TODO: check concurrency
    private readonly HashSet<KeyCodes> _pressedKeys = [];
    private readonly HashSet<int> _pressedMouseButtons = [];

    private EditorCamera _editorCamera = null!;
    private IFrameBuffer _frameBuffer = null!;
    private IFrameBuffer _toneMappedFrameBuffer = null!;
    private float _contentScale = 1.0f;
    private Vector2 _viewportSize;
    private bool _viewportHovered;
    private Entity? _hoveredEntity;
    private Entity _selectedEntity;

    // Named delegates kept so they can be unsubscribed in OnDetach
    private Action<IScene> _sceneChangedHandler = null!;
    private Action _playSceneHandler = null!;
    private Action _stopSceneHandler = null!;
    private Action _restartSceneHandler = null!;
    private Action<Entity> _entitySelectionHandler = null!;

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Debug("EditorLayer OnAttach.");

        _sceneChangedHandler = newScene => panels.SceneHierarchyPanel.SetScene(newScene);
        _playSceneHandler = () => sceneManager.Play();
        _stopSceneHandler = () => sceneManager.Stop();
        _restartSceneHandler = () => sceneManager.Restart();
        _entitySelectionHandler = entity => _selectedEntity = entity;

        sceneContext.SceneChanged += _sceneChangedHandler;
        viewport.SceneToolbar.OnPlayScene += _playSceneHandler;
        viewport.SceneToolbar.OnStopScene += _stopSceneHandler;
        viewport.SceneToolbar.OnRestartScene += _restartSceneHandler;

        _editorCamera = new EditorCamera();
        _frameBuffer = frameBufferFactory.Create();
        _toneMappedFrameBuffer = frameBufferFactory.Create(new FrameBufferSpecification(
            DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8)
            ])
        });
        _contentScale = contentScaleProvider.ContentScale;

        hdrToneMapper.Init();

        sceneManager.New("");

        panels.SceneHierarchyPanel.EntitySelected = EntitySelected;
        // Viewport selection only updates selection state - don't move camera since entity is already visible
        viewport.ViewportToolManager.SubscribeToEntitySelection(_entitySelectionHandler);

        panels.ContentBrowserPanel.Init();
        viewport.SceneToolbar.Init();

        // Apply settings from preferences
        ApplyEditorSettings();

        // Prefer current project; otherwise default to CWD/assets/scripts
        var scriptsDir = projectManager.ScriptsDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
        scriptEngine.SetScriptsDirectory(scriptsDir);

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
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Select,
            "Select tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.W, KeyModifiers.ShiftOnly,
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Move,
            "Move tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.R, KeyModifiers.ShiftOnly,
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Scale,
            "Scale tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.E, KeyModifiers.ShiftOnly,
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Ruler,
            "Ruler tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Escape, KeyModifiers.None,
            () =>
            {
                if (viewport.SceneToolbar.CurrentMode == EditorMode.Ruler)
                {
                    var rulerTool = viewport.ViewportToolManager.GetTool<RulerTool>();
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
            () => sceneManager.Save(),
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
            _editorCamera.SetFocalPoint(transformComponent.Translation);
        }
    }

    public void OnDetach()
    {
        Logger.Debug("EditorLayer OnDetach.");

        // Unsubscribe event handlers to allow GC of this layer
        sceneContext.SceneChanged -= _sceneChangedHandler;
        viewport.SceneToolbar.OnPlayScene -= _playSceneHandler;
        viewport.SceneToolbar.OnStopScene -= _stopSceneHandler;
        viewport.SceneToolbar.OnRestartScene -= _restartSceneHandler;
        viewport.ViewportToolManager.UnsubscribeFromEntitySelection(_entitySelectionHandler);

        // Dispose current scene to cleanup resources
        sceneContext.ActiveScene?.Dispose();

        _frameBuffer?.Dispose();
        _toneMappedFrameBuffer?.Dispose();
        panels.ConsolePanel?.Dispose();
        Log.CloseAndFlush();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        panels.PerformanceMonitor.Update(timeSpan);

        // Resize (logical viewport → physical framebuffer)
        var spec = _frameBuffer.GetSpecification();
        var fbWidth = (uint)(_viewportSize.X * _contentScale);
        var fbHeight = (uint)(_viewportSize.Y * _contentScale);
        if (_viewportSize is { X: > 0.0f, Y: > 0.0f } &&
            (spec.Width != fbWidth || spec.Height != fbHeight))
        {
            _frameBuffer.Resize(fbWidth, fbHeight);
            _toneMappedFrameBuffer.Resize(fbWidth, fbHeight);
            _editorCamera.SetViewportSize(_viewportSize.X, _viewportSize.Y);
            sceneContext.ActiveScene?.OnViewportResize(fbWidth, fbHeight);
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
                sceneContext.ActiveScene?.OnUpdateEditor(timeSpan, _editorCamera);
                break;
            }
            case SceneState.Play:
            {
                sceneContext.ActiveScene?.OnUpdateRuntime(timeSpan);
                break;
            }
        }

        if (sceneContext.State == SceneState.Edit && viewport.SceneToolbar.ShowGrid3D)
        {
            graphics2D.BeginScene(_editorCamera);
            viewport.ViewportGrid3D.Render(graphics2D, _editorCamera);
            graphics2D.EndScene();
        }

        // Mouse picking (logical mouse position → physical framebuffer coordinates)
        var mousePos = ImGui.GetMousePos();
        var mx = (mousePos.X - _viewportBounds[0].X) * _contentScale;
        var my = (mousePos.Y - _viewportBounds[0].Y) * _contentScale;
        var physicalWidth = (_viewportBounds[1].X - _viewportBounds[0].X) * _contentScale;
        var physicalHeight = (_viewportBounds[1].Y - _viewportBounds[0].Y) * _contentScale;
        my = physicalHeight - my; // Flip the Y-axis

        var mouseX = (int)mx;
        var mouseY = (int)my;

        if (mouseX >= 0 && mouseY >= 0 && mouseX < (int)physicalWidth && mouseY < (int)physicalHeight)
        {
            var entityId = _frameBuffer.ReadPixel(1, mouseX, mouseY);
            var entity = sceneContext.ActiveScene?.Entities.AsValueEnumerable().FirstOrDefault(x => x.Id == entityId);
            _hoveredEntity = entity;
        }

        _frameBuffer.Unbind();
        hdrToneMapper.RenderToFramebuffer(
            _frameBuffer.GetColorAttachmentRendererId(),
            _toneMappedFrameBuffer,
            editorSettingsUI.GetHdrExposure());
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
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
            case MouseButtonPressedEvent mbpe:
                _pressedMouseButtons.Add(mbpe.Button);
                break;
            case MouseButtonReleasedEvent mbre:
                _pressedMouseButtons.Remove(mbre.Button);
                break;
        }

        if (sceneContext.State == SceneState.Edit && _viewportHovered)
        {
            // Scroll zoom
            if (windowEvent is MouseScrolledEvent scrollEvent)
            {
                _editorCamera.OnMouseScroll(scrollEvent.YOffset);
            }

            // Alt+mouse controls for the editor camera
            var alt = _pressedKeys.Contains(KeyCodes.LeftAlt) || _pressedKeys.Contains(KeyCodes.RightAlt);

            if (windowEvent is MouseButtonPressedEvent)
            {
                _editorCamera.SetPreviousMousePosition(GetMousePosition());
            }
            else if (windowEvent is MouseMovedEvent moveEvent && alt)
            {
                var currentPos = new Vector2(moveEvent.X, moveEvent.Y);
                var leftDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Left);
                var middleDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Middle);
                var rightDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Right);

                _editorCamera.OnMouseMove(currentPos, pan: middleDown, orbit: leftDown, zoomDrag: rightDown);
            }
        }
        else if (sceneContext.State == SceneState.Play)
        {
            scriptEngine.ProcessEvent(windowEvent);
        }
    }

    private static Vector2 GetMousePosition()
    {
        var pos = ImGui.GetMousePos();
        return new Vector2(pos.X, pos.Y);
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
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;

        var viewPort = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewPort.Pos);
        ImGui.SetNextWindowSize(viewPort.Size);
        ImGui.SetNextWindowViewport(viewPort.ID);

        ImGui.Begin("DockSpace Demo", ref dockspaceOpen, windowFlags);
        {
            var dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new Vector2(0.0f, 0.0f), ImGuiDockNodeFlags.None);

            RenderMenuBar();
            RenderPanels();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            RenderViewport();
            
            ImGui.PopStyleVar();

            viewport.SceneToolbar.Render();
            ImGui.End();
        }

        editorSettingsUI.Render();
        newProjectPopup.Render();
        sceneSettingsPopup.Render();
        publishSettingsUI.Render();
    }

    private void RenderMenuBar()
    {
        if (!ImGui.BeginMenuBar()) return;

        RenderFileMenu();
        RenderSceneMenu();
        RenderViewMenu();
        RenderSettingsMenu();
        RenderHelpMenu();
        RenderPublishMenu();

        ImGui.EndMenuBar();
    }

    private void RenderFileMenu()
    {
        if (!ImGui.BeginMenu("File")) return;

        if (ImGui.MenuItem("New Project"))
            newProjectPopup.ShowNewProjectPopup();
        if (ImGui.MenuItem("Open Project"))
            newProjectPopup.ShowOpenProjectPopup();

        ImGui.Separator();

        if (ImGui.MenuItem("Show Recent Projects"))
            panels.RecentProjectsPanel.Show();

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
                    if (ImGui.MenuItem($"{recent.Name}"))
                    {
                        if (projectManager.TryOpenProject(recent.Path, out var error))
                            panels.ContentBrowserPanel.SetRootDirectory(AssetsManager.AssetsPath);
                        else
                            Logger.Warning("Failed to open recent project {Path}: {Error}", recent.Path, error);
                    }

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
                    editorPreferences.ClearRecentProjects();
            }
            ImGui.EndMenu();
        }

        ImGui.Separator();
        if (ImGui.MenuItem("Exit"))
            Environment.Exit(0);
        ImGui.EndMenu();
    }

    private void RenderSceneMenu()
    {
        if (!ImGui.BeginMenu("Scene...")) return;

        if (ImGui.MenuItem("New", "Ctrl+N"))
            sceneSettingsPopup.ShowNewScenePopup();
        if (ImGui.MenuItem("Save", "Ctrl+S"))
            sceneManager.Save();
        ImGui.EndMenu();
    }

    private void RenderViewMenu()
    {
        if (!ImGui.BeginMenu("View")) return;

        if (ImGui.MenuItem("Reset Camera"))
            ResetCamera();
        ImGui.Separator();
        if (ImGui.MenuItem("Show Rulers", null, viewport.ViewportRuler.Enabled))
            viewport.ViewportRuler.Enabled = !viewport.ViewportRuler.Enabled;
        if (ImGui.MenuItem("Show 2D Grid", null, viewport.SceneToolbar.ShowGrid))
            viewport.SceneToolbar.ShowGrid = !viewport.SceneToolbar.ShowGrid;
        if (ImGui.MenuItem("Show 3D Grid", null, viewport.SceneToolbar.ShowGrid3D))
            viewport.SceneToolbar.ShowGrid3D = !viewport.SceneToolbar.ShowGrid3D;
        if (ImGui.MenuItem("Show Stats", null, panels.RendererStatsPanel.IsVisible))
            panels.RendererStatsPanel.IsVisible = !panels.RendererStatsPanel.IsVisible;
        ImGui.EndMenu();
    }

    private void RenderSettingsMenu()
    {
        if (!ImGui.BeginMenu("Settings")) return;

        if (ImGui.MenuItem("Editor Settings"))
            editorSettingsUI.Show();
        ImGui.EndMenu();
    }

    private void RenderHelpMenu()
    {
        if (!ImGui.BeginMenu("Help")) return;

        if (ImGui.MenuItem("Keyboard Shortcuts"))
            panels.KeyboardShortcutsPanel.Show();
        ImGui.EndMenu();
    }

    private void RenderPublishMenu()
    {
        if (!ImGui.BeginMenu("Publish")) return;

        if (ImGui.MenuItem("Build & Publish"))
            BuildAndPublish();
        ImGui.EndMenu();
    }

    private void RenderPanels()
    {
        panels.SceneHierarchyPanel.Draw();
        panels.PropertiesPanel.Draw();
        panels.ContentBrowserPanel.Draw();
        panels.ConsolePanel.Draw();

        scriptComponentEditor.Draw();
        panels.RecentProjectsPanel.Draw();
        panels.KeyboardShortcutsPanel.Draw();

        var selectedEntity = panels.SceneHierarchyPanel.GetSelectedEntity();
        panels.PropertiesPanel.SetSelectedEntity(selectedEntity);

        var hoveredEntityName = _hoveredEntity?.Name ?? "None";
        var camPos = _editorCamera.GetPosition();
        panels.RendererStatsPanel.Draw(hoveredEntityName, camPos, _editorCamera.Yaw, () => panels.PerformanceMonitor.RenderUI());
    }

    private void RenderViewport()
    {
        ImGui.Begin("Viewport");

        _viewportHovered = ImGui.IsWindowHovered();

        var viewportPanelSize = ImGui.GetContentRegionAvail();
        var texturePointer = new IntPtr(_toneMappedFrameBuffer.GetColorAttachmentRendererId());
        ImGui.Image(texturePointer, new Vector2(viewportPanelSize.X, viewportPanelSize.Y), new Vector2(0, 1), new Vector2(1, 0));

        _viewportBounds[0] = ImGui.GetItemRectMin();
        _viewportBounds[1] = ImGui.GetItemRectMax();
        _viewportSize = _viewportBounds[1] - _viewportBounds[0];

        var sceneValidator = DragDropDrawer.CreateExtensionValidator([".scene"], checkFileExists: false);
        DragDropDrawer.HandleFileDropTarget(DragDropDrawer.ContentBrowserItemPayload, sceneValidator,
            onDropped: path => sceneManager.Open(PathBuilder.Build(path)));

        if (ImGui.IsWindowHovered())
        {
            var currentMode = viewport.SceneToolbar.CurrentMode;
            var globalMousePos = ImGui.GetMousePos();
            var localMousePos = new Vector2(globalMousePos.X - _viewportBounds[0].X, globalMousePos.Y - _viewportBounds[0].Y);

            viewport.ViewportToolManager.SetMode(currentMode);
            viewport.ViewportToolManager.SetHoveredEntity(_hoveredEntity);

            var currentSelection = panels.SceneHierarchyPanel.GetSelectedEntity();
            if (currentSelection != null)
                viewport.ViewportToolManager.SetTargetEntity(currentSelection);

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                viewport.ViewportToolManager.HandleMouseDown(localMousePos, _viewportBounds, _editorCamera);
                if (_hoveredEntity != null && currentMode != EditorMode.Ruler)
                    panels.SceneHierarchyPanel.SetSelectedEntity(_hoveredEntity);
            }

            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                viewport.ViewportToolManager.HandleMouseMove(localMousePos, _viewportBounds, _editorCamera);

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                viewport.ViewportToolManager.HandleMouseUp(localMousePos, _viewportBounds, _editorCamera);
        }

        var focalPoint = _editorCamera.FocalPoint;
        var cameraPos = new Vector2(focalPoint.X, focalPoint.Y);
        var distance = _editorCamera.Distance;
        var fovRad = MathHelpers.DegreesToRadians(_editorCamera.FOV);
        var worldHeight = 2.0f * distance * MathF.Tan(fovRad * 0.5f);
        var zoom = _viewportSize.Y / worldHeight;

        if (viewport.SceneToolbar.ShowGrid)
            viewport.ViewportGrid.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);

        viewport.ViewportRuler.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);
        viewport.ViewportToolManager.RenderActiveTool(_viewportBounds, _editorCamera);

        var viewportDockId = ImGui.GetWindowDockID();
        ImGui.End();
    }

    private void BuildAndPublish() => publishSettingsUI.ShowPublishModal();

    private void ResetCamera()
    {
        _editorCamera.SetFocalPoint(Vector3.Zero);
        _editorCamera.SetDistance(CameraConfig.DefaultEditorDistance);
        _editorCamera.SetPitch(0.0f);
        _editorCamera.SetYaw(0.0f);
    }
}
