using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Editor.Managers;
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
using Engine.Scripting;
using ImGuiNET;
using NLog;
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
    private string? _editorScenePath;
    
    private ProjectUI _projectUI;
    private IProjectManager _projectManager;
    private SceneManager _sceneManager;
    
    private readonly RendererStatsUI _rendererStatsUI = new();
    private EditorToolbarUI _editorToolbarUI;
    private readonly PerformanceMonitorUI _performanceMonitor = new();
    private EditorSettingsUI _editorSettingsUI;
    
    public EditorLayer() : base("EditorLayer")
    {
    }

    public override void OnAttach()
    {
        Logger.Debug("EditorLayer OnAttach.");

        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");

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

        _sceneManager = new SceneManager(_sceneHierarchyPanel);

        _contentBrowserPanel = new ContentBrowserPanel();
        _consolePanel = new ConsolePanel();
        _propertiesPanel = new PropertiesPanel();
        _projectManager  = new ProjectManager();
        _projectUI = new ProjectUI(_projectManager, _contentBrowserPanel);
        _editorToolbarUI = new EditorToolbarUI(_sceneManager, _iconPlay, _iconStop);
        _editorSettingsUI = new EditorSettingsUI(_cameraController, new EditorSettings());
        
        // Prefer current project; otherwise default to CWD/assets/scripts
        var scriptsDir = _projectManager.ScriptsDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
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
        _performanceMonitor.Update(timeSpan);
        
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

        Graphics2D.Instance.SetClearColor(_editorSettingsUI.Settings.BackgroundColor);
        Graphics2D.Instance.Clear();

        _frameBuffer.ClearAttachment(1, -1);

        switch (_sceneManager.SceneState)
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
        if (_sceneManager.SceneState == SceneState.Edit)
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
                    _sceneManager.New(_viewportSize);
                keyPressedEvent.IsHandled = true;
                break;
            }
            case (int)KeyCodes.S:
            {
                if (control)
                {
                    _sceneManager.Save(_projectManager.ScenesDir);
                    keyPressedEvent.IsHandled = true;
                }
                break;
            }
            case (int)KeyCodes.D:
            {
                if (control)
                {
                    _sceneManager.DuplicateEntity();
                    keyPressedEvent.IsHandled = true;
                }
                break;
            }
            case (int)KeyCodes.F:
            {
                if (control)
                {
                    _sceneManager.FocusOnSelectedEntity(_cameraController);
                    keyPressedEvent.IsHandled = true;
                }
                break;
            }
        }
    }

    public override void OnImGuiRender()
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
                        _projectUI.ShowNewProjectPopup();
                    if (ImGui.MenuItem("Open Project"))
                        _projectUI.ShowOpenProjectPopup();
                    if (ImGui.MenuItem("Exit"))
                        Environment.Exit(0);
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Scene..."))
                {
                    if (ImGui.MenuItem("New", "Ctrl+N"))
                        _sceneManager.New(_viewportSize);
                    if (ImGui.MenuItem("Save", "Ctrl+S"))
                        _sceneManager.Save(_projectManager.ScenesDir);
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("View"))
                {
                    if (ImGui.MenuItem("Focus on Selected", "Ctrl+F"))
                        _sceneManager.FocusOnSelectedEntity(_cameraController);
                    if (ImGui.MenuItem("Reset Camera"))
                        ResetCamera();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Settings"))
                {
                    if (ImGui.MenuItem("Editor Settings"))
                        _editorSettingsUI.Show();
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

            _editorSettingsUI.Render();

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
            
            _performanceMonitor.RenderUI();
            
            // Camera info
            ImGui.Text("Camera:");
            var camPos = _cameraController.Camera.Position;
            ImGui.Text($"Position: ({camPos.X:F2}, {camPos.Y:F2}, {camPos.Z:F2})");
            ImGui.Text($"Rotation: {_cameraController.Camera.Rotation:F1}°");

            ImGui.Separator();
            
            _rendererStatsUI.Render();

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
                                _sceneManager.Open(_viewportSize, Path.Combine(AssetsManager.AssetsPath, path));
                        }
                        ImGui.EndDragDropTarget();
                    }
                }

                ImGui.End();
            }

            ImGui.End();
            ImGui.PopStyleVar();

            _editorToolbarUI.Render();
            ImGui.End();
        }
        
        _projectUI.Render();
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
}