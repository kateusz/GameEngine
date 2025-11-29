using System.Numerics;
using Engine.Renderer;
using ImGuiNET;

namespace Editor.Panels;

public class RendererStatsPanel(IGraphics2D graphics2D, IGraphics3D graphics3D)
{
    public bool IsVisible { get; set; }

    public void Draw(string hoveredEntityName, Vector3 cameraPosition, float cameraRotation, Action? renderPerformanceMonitor)
    {
        if (!IsVisible)
            return;

        var isVisible = IsVisible;
        ImGui.Begin("Stats", ref isVisible);
        IsVisible = isVisible;
        
        ImGui.Text($"Hovered Entity: {hoveredEntityName}");
        
        renderPerformanceMonitor?.Invoke();
        
        // Camera info
        ImGui.Text("Camera:");
        ImGui.Text($"Position: ({cameraPosition.X:F2}, {cameraPosition.Y:F2}, {cameraPosition.Z:F2})");
        ImGui.Text($"Rotation: {cameraRotation:F1}°");

        ImGui.Separator();
        
        // --- Renderer2D Stats ---
        var stats2D = graphics2D.GetStats();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats2D.GetTotalIndexCount()}");

        ImGui.Separator();

        // --- Renderer3D Stats ---
        var stats3D = graphics3D.GetStats();
        ImGui.Text("Renderer3D Stats:");
        ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");

        ImGui.End();
    }
}