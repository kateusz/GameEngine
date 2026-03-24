using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Renderer;
using Engine.Renderer.Profiling;
using ImGuiNET;

namespace Editor.Panels;

public class RendererStatsPanel(IGraphics2D graphics2D, IGraphics3D graphics3D, IPerformanceProfiler profiler, ProfileExporter profileExporter)
{
    public bool IsVisible { get; set; } = true;

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

        var data = profiler.GetData();
        if (profiler.Enabled && data.RegisteredScopes.Count > 0)
        {
            ImGui.SeparatorText("Per-System Timing");
            foreach (var scope in data.RegisteredScopes)
            {
                var cpuMs = data.GetScopeTimingMs(scope);
                var gpuMs = data.GetGpuTimingMs(scope);
                ImGui.Text($"{scope}: CPU {cpuMs:F2}ms | GPU {gpuMs:F2}ms");
            }

            ImGui.SeparatorText("State Changes");
            ImGui.Text($"Texture Binds: {data.GetCounterValue("TextureBinds")}");
            ImGui.Text($"Shader Binds: {data.GetCounterValue("ShaderBinds")}");
            ImGui.Text($"Buffer Uploads: {data.GetCounterValue("BufferUploads")}");
            ImGui.Text($"Framebuffer Binds: {data.GetCounterValue("FramebufferBinds")}");
            ImGui.Text($"Batch Flushes: {data.GetCounterValue("BatchFlushes")}");
            ImGui.Text($"Batch Efficiency: {data.GetGaugeValue("BatchEfficiency"):P1}");

            ImGui.SeparatorText("Allocations");
            foreach (var scope in data.RegisteredScopes)
            {
                var alloc = data.GetAllocation(scope);
                if (alloc > 0)
                {
                    var color = alloc > 1024
                        ? EditorUIConstants.ErrorColor
                        : EditorUIConstants.SuccessColor;
                    ImGui.TextColored(color, $"{scope}: {alloc:N0} bytes");
                }
            }
        }

        if (profiler.Enabled)
        {
            ImGui.Separator();
            if (ButtonDrawer.DrawButton("Export CSV"))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                profileExporter.ExportToFile($"profiling/{timestamp}-editor.csv", 300);
            }
            ImGui.SameLine();
            if (ButtonDrawer.DrawButton("Export JSON"))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                profileExporter.ExportToFile($"profiling/{timestamp}-editor.json", 300, json: true);
            }
        }

        ImGui.End();
    }
}