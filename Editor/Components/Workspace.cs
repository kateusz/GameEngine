using System.Numerics;
using ECS;
using Editor.Panels;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Components;

public class Workspace : IDisposable
{
    private readonly SceneHierarchyPanel _sceneHierarchyPanel;
    private readonly ContentBrowserPanel _contentBrowserPanel;
    private readonly PropertiesPanel _propertiesPanel;
    private readonly ConsolePanel _consolePanel;
    
    private bool _showSettings = false;
    private Vector4 _backgroundColor = new Vector4(232.0f, 232.0f, 232.0f, 1.0f);
    private bool _disposed = false;

    public Workspace()
    {
        _sceneHierarchyPanel = new SceneHierarchyPanel(CurrentScene.Instance);
        _contentBrowserPanel = new ContentBrowserPanel();
        _consolePanel = new ConsolePanel();
        _propertiesPanel = new PropertiesPanel();
    }

    public SceneHierarchyPanel SceneHierarchyPanel => _sceneHierarchyPanel;
    public ContentBrowserPanel ContentBrowserPanel => _contentBrowserPanel;
    public PropertiesPanel PropertiesPanel => _propertiesPanel;
    public ConsolePanel ConsolePanel => _consolePanel;

    public Vector4 BackgroundColor 
    { 
        get => _backgroundColor;
        set => _backgroundColor = value;
    }

    public void SetEntitySelectedCallback(Action<Entity> callback)
    {
        _sceneHierarchyPanel.EntitySelected = callback;
    }

    public void RenderMainUI(Action toolbarRenderer, Action viewportRenderer, Action<bool> onShowSettings)
    {
        var dockspaceOpen = true;
        const bool fullscreenPersistant = true;
        const ImGuiDockNodeFlags dockspaceFlags = ImGuiDockNodeFlags.PassthruCentralNode;
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking | 
                                           ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | 
                                           ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
        
        if (fullscreenPersistant)
        {
            var viewPort = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewPort.Pos);
            ImGui.SetNextWindowSize(viewPort.Size);
            ImGui.SetNextWindowViewport(viewPort.ID);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));
        }

        ImGui.Begin("DockSpace Demo", ref dockspaceOpen, windowFlags);
        {
            if (fullscreenPersistant)
                ImGui.PopStyleVar(3);

            // Use fixed DockSpace ID for consistent layout
            var dockspaceId = 0x3BC79352u;
            ImGui.DockSpace(dockspaceId, new Vector2(0.0f, 0.0f), dockspaceFlags);

            RenderMenuBar(onShowSettings);
            RenderPanels();
            RenderSettingsWindow();
            
            // Render custom content
            toolbarRenderer.Invoke();
            viewportRenderer.Invoke();
            
            ImGui.End();
        }
    }

    private void RenderMenuBar(Action<bool> onShowSettings)
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New Project"))
                    OnNewProjectRequested?.Invoke();
                if (ImGui.MenuItem("Open Project"))
                    OnOpenProjectRequested?.Invoke();
                if (ImGui.MenuItem("Exit"))
                    Environment.Exit(0);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Scene..."))
            {
                if (ImGui.MenuItem("New", "Ctrl+N"))
                    OnNewSceneRequested?.Invoke();
                if (ImGui.MenuItem("Open...", "Ctrl+O"))
                    OnOpenSceneRequested?.Invoke();
                if (ImGui.MenuItem("Save", "Ctrl+S"))
                    OnSaveSceneRequested?.Invoke();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Focus on Selected", "Ctrl+F"))
                    OnFocusOnSelectedRequested?.Invoke();
                if (ImGui.MenuItem("Reset Camera"))
                    OnResetCameraRequested?.Invoke();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Settings"))
            {
                if (ImGui.MenuItem("Editor Settings"))
                {
                    _showSettings = true;
                    onShowSettings?.Invoke(_showSettings);
                }
                ImGui.EndMenu();
            }
            
            if (ImGui.BeginMenu("Publish"))
            {
                if (ImGui.MenuItem("Build & Publish"))
                    OnBuildAndPublishRequested?.Invoke();
                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }
    }

    private void RenderPanels()
    {
        _sceneHierarchyPanel.OnImGuiRender();
        _propertiesPanel.OnImGuiRender();
        _contentBrowserPanel.OnImGuiRender();
        _consolePanel.OnImGuiRender();
        
        ScriptComponentUI.OnImGuiRender();
        
        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        _propertiesPanel.SetSelectedEntity(selectedEntity);
    }

    private void RenderSettingsWindow()
    {
        if (_showSettings)
        {
            ImGui.Begin("Editor Settings", ref _showSettings, ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Text("Editor Background Color");
            ImGui.ColorEdit4("Background Color", ref _backgroundColor);
            ImGui.End();
        }
    }


    public void UpdateSceneContext()
    {
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
    }

    // Events for decoupling UI from business logic
    public event Action? OnNewProjectRequested;
    public event Action? OnOpenProjectRequested;
    public event Action? OnNewSceneRequested;
    public event Action? OnOpenSceneRequested;
    public event Action? OnSaveSceneRequested;
    public event Action? OnFocusOnSelectedRequested;
    public event Action? OnResetCameraRequested;
    public event Action? OnBuildAndPublishRequested;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _consolePanel?.Dispose();
            _disposed = true;
        }
    }
}