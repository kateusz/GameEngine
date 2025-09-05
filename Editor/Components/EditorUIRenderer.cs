using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Components;

public interface IEditorUIRenderer
{
    void RenderMainUI(
        Workspace workspace,
        IEditorViewport viewport,
        IEditorPerformanceMonitor performanceMonitor,
        SceneController sceneController,
        EditorInputHandler inputHandler,
        ProjectController projectController);
    void Dispose();
}

public class EditorUIRenderer : IEditorUIRenderer, IDisposable
{
    private readonly Texture2D _iconPlay;
    private readonly Texture2D _iconStop;
    private bool _disposed;

    public EditorUIRenderer()
    {
        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");
    }

    public void RenderMainUI(
        Workspace workspace,
        IEditorViewport viewport,
        IEditorPerformanceMonitor performanceMonitor,
        SceneController sceneController,
        EditorInputHandler inputHandler,
        ProjectController projectController)
    {
        workspace.RenderMainUI(
            () => RenderToolbar(sceneController),
            () => RenderViewport(viewport, sceneController, inputHandler, performanceMonitor),
            (showSettings) => { });
        
        projectController.RenderProjectDialogs();
    }

    private void RenderToolbar(SceneController sceneController)
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
        var icon = sceneController.CurrentState == SceneState.Edit ? _iconPlay : _iconStop;

        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));

        if (ImGui.ImageButton("playstop", (IntPtr)icon.GetRendererId(), new Vector2(size, size), new Vector2(0, 0),
                new Vector2(1, 1)))
        {
            switch (sceneController.CurrentState)
            {
                case SceneState.Edit:
                    sceneController.PlayScene();
                    break;
                case SceneState.Play:
                    sceneController.StopScene();
                    break;
            }
        }
        
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        ImGui.End();
    }

    private void RenderViewport(
        IEditorViewport viewport, 
        SceneController sceneController, 
        EditorInputHandler inputHandler,
        IEditorPerformanceMonitor performanceMonitor)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

        ImGui.Begin("Viewport");
        {
            var viewportMinRegion = ImGui.GetWindowContentRegionMin();
            var viewportMaxRegion = ImGui.GetWindowContentRegionMax();
            var viewportOffset = ImGui.GetWindowPos();
            
            viewport.UpdateViewportBounds(
                new Vector2(viewportMinRegion.X + viewportOffset.X, viewportMinRegion.Y + viewportOffset.Y),
                new Vector2(viewportMaxRegion.X + viewportOffset.X, viewportMaxRegion.Y + viewportOffset.Y)
            );

            var viewportFocused = ImGui.IsWindowFocused();
            var viewportHovered = ImGui.IsWindowHovered();
            
            viewport.SetFocusedState(viewportFocused);
            viewport.SetHoveredState(viewportHovered);
            inputHandler.ViewportFocused = viewportFocused;
            Application.ImGuiLayer.BlockEvents = !viewportFocused && !viewportHovered;

            var viewportPanelSize = ImGui.GetContentRegionAvail();
            viewport.SetViewportSize(viewportPanelSize);
            
            var textureId = viewport.GetColorAttachmentId();
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
                            sceneController.OpenScene(Path.Combine(AssetsManager.AssetsPath, path));
                    }
                    ImGui.EndDragDropTarget();
                }
            }

            ImGui.End();
        }

        ImGui.PopStyleVar();
        
        RenderStatsWindow(viewport, performanceMonitor, inputHandler);
    }
    
    private void RenderStatsWindow(
        IEditorViewport viewport, 
        IEditorPerformanceMonitor performanceMonitor,
        EditorInputHandler inputHandler)
    {
        ImGui.Begin("Stats");

        var name = "None";
        if (viewport.HoveredEntity != null)
        {
            name = viewport.HoveredEntity.Name;
        }

        ImGui.Text($"Hovered Entity: {name}");
        
        performanceMonitor.RenderPerformanceStats();

        ImGui.Separator();
        var stats = Graphics2D.Instance.GetStats();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats.DrawCalls}");
        ImGui.Text($"Quads: {stats.QuadCount}");
        ImGui.Text($"Vertices: {stats.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats.GetTotalIndexCount()}");
        
        ImGui.Text("Camera:");
        var camPos = inputHandler.CameraController.Camera.Position;
        ImGui.Text($"Position: ({camPos.X:F2}, {camPos.Y:F2}, {camPos.Z:F2})");
        ImGui.Text($"Rotation: {inputHandler.CameraController.Camera.Rotation:F1}°");
        
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