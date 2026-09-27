using System.Numerics;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class RendererStatsPanel(ISceneContext sceneContext, IGraphics2D graphics2D, IGraphics3D graphics3D)
{
    public bool IsVisible { get; set; } = true;

    public void Draw(string hoveredEntityName, Vector3 cameraPosition, Vector3 focalPoint, float cameraRotation, Action? renderPerformanceMonitor)
    {
        if (!IsVisible)
            return;

        var isVisible = IsVisible;
        ImGui.Begin("Stats", ref isVisible);
        IsVisible = isVisible;
        
        ImGui.Text($"Hovered Entity: {hoveredEntityName}");
        
        renderPerformanceMonitor?.Invoke();
        
        ImGui.Separator();
        
        ImGui.Text("Editor Camera");
        ImGui.Text($"Position: ({cameraPosition.X:F2}, {cameraPosition.Y:F2}, {cameraPosition.Z:F2})");
        ImGui.Text($"Focal: ({focalPoint.X:F2}, {focalPoint.Y:F2}, {focalPoint.Z:F2})");
        ImGui.Text($"Rotation: {cameraRotation:F1}°");

        ImGui.Separator();

        var dimension = sceneContext.ActiveScene?.Dimension ?? SceneDimension.TwoD;
        if (dimension == SceneDimension.TwoD)
        {
            var stats2D = graphics2D.GetStats();
            ImGui.Text("Renderer2D Stats");
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
        }
        else
        {
            DrawRenderer3DStats(graphics3D.GetStats());
        }
        
        ImGui.End();
    }

    private static void DrawRenderer3DStats(Engine.Renderer.Statistics stats)
    {
        ImGui.Text("Renderer3D Stats");
        ImGui.Indent();

        ImGui.Text($"Draw Calls: {stats.DrawCalls}");
        ImGui.Text($"  Color / Dir Shadow / Point Shadow: {stats.ColorDrawCalls} / {stats.DirectionalShadowDrawCalls} / {stats.PointShadowDrawCalls}");
        ImGui.Text($"Cubes: {stats.CubeDraws}");
        ImGui.Text($"Mesh Draws: {stats.MeshDraws} (instanced: {stats.InstancedDraws})");
        ImGui.Text($"Instances: {stats.Instances}");
        ImGui.Text($"Vertices: {stats.Vertices}");
        ImGui.Text($"Triangles: {stats.Triangles}");

        ImGui.Separator();
        ImGui.Text($"Renderers: {stats.Renderers}");
        ImGui.Text($"Frustum Culled: {stats.FrustumCulled}");
        ImGui.Text($"Zone Culled: {stats.ZoneCulled}");
        ImGui.Text($"Shadow Caster Culled: {stats.ShadowCasterCulled}");
        ImGui.Text($"Material Batches: single {stats.SingleMaterialDraws} / multi {stats.MultiMaterialDraws}");
        ImGui.Text($"Max Batch Instances: {stats.MaxBatchInstances}");

        ImGui.Separator();
        ImGui.Text($"Directional Shadow: {(stats.DirectionalShadow ? "on" : "off")}");
        ImGui.Text($"Point Lights: {stats.PointLights} (shadow: {stats.PointShadowLights}, cache hits: {stats.PointShadowCacheHits})");
        ImGui.Text($"CPU Color: {stats.ColorCpuMs:F2} ms");
        ImGui.Text($"CPU Shadows: {stats.ShadowCpuMs:F2} ms");

        ImGui.Unindent();
    }
}
