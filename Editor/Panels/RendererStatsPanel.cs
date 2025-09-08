using Engine.Renderer;
using ImGuiNET;

namespace Editor.Panels;

public class RendererStatsPanel
{
    public void Render(Statistics stats2D, Statistics stats3D)
    {
        // --- Renderer2D Stats ---
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats2D.GetTotalIndexCount()}");

        ImGui.Separator();

        // --- Renderer3D Stats ---
        ImGui.Text("Renderer3D Stats:");
        ImGui.Text($"Draw Calls: {stats3D.DrawCalls}");

        ImGui.End();
    }
}