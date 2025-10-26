using Engine.Renderer;
using ImGuiNET;

namespace Editor.Panels;

public class RendererStatsPanel
{
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;

    public RendererStatsPanel(IGraphics2D graphics2D, IGraphics3D graphics3D)
    {
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
    }

    // TODO: refactor it
    public void Render()
    {
        // --- Renderer2D Stats ---
        var stats2D = _graphics2D.GetStats();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats2D.GetTotalIndexCount()}");

        ImGui.Separator();

        // --- Renderer3D Stats ---
        var stats3D = _graphics3D.GetStats();
        ImGui.Text("Renderer3D Stats:");
        ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");

        ImGui.End();
    }
}