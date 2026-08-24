using System.Numerics;
using Engine.Renderer.Pipeline;
using ImGuiNET;

namespace Editor.Panels;

public class RendererStatsPanel(IGraphics2D graphics2D, IGraphics3D graphics3D)
{
    public bool IsVisible { get; set; } = false;

    public void Draw(string hoveredEntityName, Vector3 cameraPosition, float cameraRotation, Action? renderPerformanceMonitor)
    {
        if (!IsVisible)
            return;

        var isVisible = IsVisible;
        ImGui.Begin("Stats", ref isVisible);
        IsVisible = isVisible;
        
        ImGui.Text($"Hovered Entity: {hoveredEntityName}");
        
        renderPerformanceMonitor?.Invoke();
        
        ImGui.Text("Editor Camera:");
        ImGui.Text($"Position: ({cameraPosition.X:F2}, {cameraPosition.Y:F2}, {cameraPosition.Z:F2})");
        ImGui.Text($"Rotation: {cameraRotation:F1}°");

        ImGui.Separator();
        
        var stats2D = graphics2D.GetStats();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Indent();
        ImGui.Text($"Quad Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Line Draw Calls: {stats2D.LineDrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Line Vertices: {stats2D.LineVertexCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Batch Count: {stats2D.BatchCount}");
        ImGui.Text($"Texture Binds: {stats2D.TextureBinds}");
        ImGui.Text($"Upload: {stats2D.UploadBytes / 1024.0:F1} KB");
        ImGui.Text($"CPU Flush: {stats2D.FlushMs:F3} ms");
        ImGui.Text($"GPU Quad Pass: {stats2D.GpuQuadPassMs:F3} ms");
        ImGui.Unindent();

        ImGui.Separator();

        // --- Renderer3D Stats ---
        var stats3D = graphics3D.GetStats();
        ImGui.Text("Renderer3D Stats:");
        ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");
        
        ImGui.End();
    }
}
