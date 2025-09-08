using Engine.Renderer;
using ImGuiNET;

namespace Editor.Panels;

public class RendererStatsUI
{
    public void Render()
    {
        // --- Renderer2D Stats ---
        var stats2D = Graphics2D.Instance.GetStats();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats2D.GetTotalIndexCount()}");

        ImGui.Separator();

        // --- Renderer3D Stats ---
        var stats3D = Graphics3D.Instance.GetStats();
        ImGui.Text("Renderer3D Stats:");
        ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");

        ImGui.End();
    }
}