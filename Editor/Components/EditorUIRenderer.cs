using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Components;

public interface IEditorUIRenderer : IDisposable
{
    void RenderMainUI();
}

public class EditorUIRenderer : IEditorUIRenderer, IDisposable
{
    private readonly Workspace? _workspace;
    private readonly IEditorViewport _viewport;
    private readonly IEditorPerformanceMonitor _performanceMonitor;
    private readonly SceneController _sceneController;
    private readonly EditorInputHandler _inputHandler;
    private readonly ProjectController? _projectController;
    
    private readonly Texture2D _iconPlay;
    private readonly Texture2D _iconStop;
    private bool _disposed;

    public EditorUIRenderer(Workspace? workspace, IEditorViewport viewport, IEditorPerformanceMonitor performanceMonitor, SceneController sceneController, EditorInputHandler inputHandler, ProjectController? projectController)
    {
        _workspace = workspace;
        _viewport = viewport;
        _performanceMonitor = performanceMonitor;
        _sceneController = sceneController;
        _inputHandler = inputHandler;
        _projectController = projectController;
        
        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");
    }

    public void RenderMainUI()
    {
        _workspace?.RenderMainUI(
            RenderToolbar,
            RenderViewport,
            (showSettings) => { });
        
        _projectController?.RenderProjectDialogs();
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
        
        // Force viewport to dock in the central node
        ImGui.SetNextWindowDockID(0x3BC79352u, ImGuiCond.Always);
        
        // Set reasonable minimum viewport size and allow unlimited maximum size
        ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(float.MaxValue, float.MaxValue));

        // Add flags to ensure viewport can resize properly within dock node
        ImGui.Begin("Viewport", 
            ImGuiWindowFlags.NoScrollbar | 
            ImGuiWindowFlags.NoScrollWithMouse | 
            ImGuiWindowFlags.NoCollapse);
        {
            var viewportMinRegion = ImGui.GetWindowContentRegionMin();
            var viewportMaxRegion = ImGui.GetWindowContentRegionMax();
            var viewportOffset = ImGui.GetWindowPos();
            
            var boundsMin = new Vector2(viewportMinRegion.X + viewportOffset.X, viewportMinRegion.Y + viewportOffset.Y);
            var boundsMax = new Vector2(viewportMaxRegion.X + viewportOffset.X, viewportMaxRegion.Y + viewportOffset.Y);
            
            var state = _viewport.State;
            
            // Only update viewport bounds if they've changed
            if (state.ViewportBounds[0] != boundsMin || state.ViewportBounds[1] != boundsMax)
            {
                _viewport.UpdateViewportBounds(boundsMin, boundsMax);
            }

            var viewportFocused = ImGui.IsWindowFocused();
            var viewportHovered = ImGui.IsWindowHovered();
            
            // Only update focused state if it changed
            if (state.ViewportFocused != viewportFocused)
            {
                _viewport.SetFocusedState(viewportFocused);
            }
            
            // Only update hovered state if it changed
            if (state.ViewportHovered != viewportHovered)
            {
                _viewport.SetHoveredState(viewportHovered);
            }
            
            Application.ImGuiLayer.BlockEvents = !viewportFocused && !viewportHovered;

            var viewportPanelSize = ImGui.GetContentRegionAvail();
            var windowSize = ImGui.GetWindowSize();
            var dockId = ImGui.GetWindowDockID();
            
            // Ensure minimum viewport size
            var minSize = new Vector2(Math.Max(32, viewportPanelSize.X), Math.Max(32, viewportPanelSize.Y));
            
            // Debug viewport sizing information
            Console.WriteLine($"Viewport Debug - ContentRegion: {viewportPanelSize}, WindowSize: {windowSize}, MinSize: {minSize}, DockID: 0x{dockId:X8}, Focused: {viewportFocused}, Hovered: {viewportHovered}");
            
            // Only update viewport size if it changed significantly (avoid micro-updates)
            var sizeDelta = new Vector2(
                Math.Abs(state.ViewportSize.X - minSize.X),
                Math.Abs(state.ViewportSize.Y - minSize.Y)
            );
            
            if (sizeDelta.X > 1.0f || sizeDelta.Y > 1.0f)
            {
                _viewport.SetViewportSize(minSize);
            }
            
            var textureId = _viewport.GetColorAttachmentId();
            var texturePointer = new IntPtr(textureId);
            ImGui.Image(texturePointer, new Vector2(viewportPanelSize.X, viewportPanelSize.Y), new Vector2(0, 1),
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
        if (_viewport.State.HoveredEntity != null)
        {
            name = _viewport.State.HoveredEntity.Name;
        }

        ImGui.Text($"Hovered Entity: {name}");
        
        _performanceMonitor.RenderPerformanceStats();

        ImGui.Separator();
        var stats = Graphics2D.Instance.GetStats();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats.DrawCalls}");
        ImGui.Text($"Quads: {stats.QuadCount}");
        ImGui.Text($"Vertices: {stats.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats.GetTotalIndexCount()}");
        
        ImGui.Text("Camera:");
        var camPos = _inputHandler.CameraController.Camera.Position;
        ImGui.Text($"Position: ({camPos.X:F2}, {camPos.Y:F2}, {camPos.Z:F2})");
        ImGui.Text($"Rotation: {_inputHandler.CameraController.Camera.Rotation:F1}°");
        
        var stats3D = Graphics3D.Instance.GetStats();
        ImGui.Text("Renderer3D Stats:");
        ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");

        ImGui.End();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        // Texture cleanup handled by graphics system
        _disposed = true;
    }
}