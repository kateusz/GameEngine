using Engine.Renderer;

namespace Benchmark;

public sealed class PipelineStatsAggregator
{
    private readonly List<Statistics> _samples = [];

    public void AddSample(Statistics stats) => _samples.Add(Copy(stats));

    public int SampleCount => _samples.Count;

    public void Clear() => _samples.Clear();

    public void ApplyTo(BenchmarkResult result)
    {
        if (_samples.Count == 0)
            return;

        result.CustomMetrics["Avg Color Draw Calls"] = Avg(s => s.ColorDrawCalls).ToString("F1");
        result.CustomMetrics["Avg Total Draw Calls"] = Avg(s => s.DrawCalls).ToString("F1");
        result.CustomMetrics["Avg Dir Shadow Draw Calls"] = Avg(s => s.DirectionalShadowDrawCalls).ToString("F1");
        result.CustomMetrics["Avg Point Shadow Draw Calls"] = Avg(s => s.PointShadowDrawCalls).ToString("F1");
        result.CustomMetrics["Avg Mesh Draws"] = Avg(s => s.MeshDraws).ToString("F1");
        result.CustomMetrics["Avg Instanced Draws"] = Avg(s => s.InstancedDraws).ToString("F1");
        result.CustomMetrics["Avg Instances"] = Avg(s => s.Instances).ToString("F0");
        result.CustomMetrics["Avg Triangles"] = Avg(s => s.Triangles).ToString("F0");
        result.CustomMetrics["Avg Frustum Culled"] = Avg(s => s.FrustumCulled).ToString("F1");
        result.CustomMetrics["Avg Occlusion Culled"] = Avg(s => s.OcclusionCulled).ToString("F1");
        result.CustomMetrics["Avg Zone Culled"] = Avg(s => s.ZoneCulled).ToString("F1");
        result.CustomMetrics["Avg Color CPU Ms"] = Avg(s => s.ColorCpuMs).ToString("F3");
        result.CustomMetrics["Avg Color Collect CPU Ms"] = Avg(s => s.ColorCollectCpuMs).ToString("F3");
        result.CustomMetrics["Avg Color Mesh Flush CPU Ms"] = Avg(s => s.ColorMeshFlushCpuMs).ToString("F3");
        result.CustomMetrics["Avg Color Merge Flush CPU Ms"] = Avg(s => s.ColorMergeFlushCpuMs).ToString("F3");
        result.CustomMetrics["Avg Shadow CPU Ms"] = Avg(s => s.ShadowCpuMs).ToString("F3");
        result.CustomMetrics["Max Color CPU Ms"] = _samples.Max(s => s.ColorCpuMs).ToString("F3");
    }

    private static Statistics Copy(Statistics s) =>
        new()
        {
            DrawCalls = s.DrawCalls,
            ColorDrawCalls = s.ColorDrawCalls,
            DirectionalShadowDrawCalls = s.DirectionalShadowDrawCalls,
            PointShadowDrawCalls = s.PointShadowDrawCalls,
            CubeDraws = s.CubeDraws,
            MeshDraws = s.MeshDraws,
            InstancedDraws = s.InstancedDraws,
            Instances = s.Instances,
            Vertices = s.Vertices,
            Indices = s.Indices,
            Renderers = s.Renderers,
            FrustumCulled = s.FrustumCulled,
            OcclusionCulled = s.OcclusionCulled,
            ZoneCulled = s.ZoneCulled,
            ShadowCasterCulled = s.ShadowCasterCulled,
            ColorCpuMs = s.ColorCpuMs,
            ColorCollectCpuMs = s.ColorCollectCpuMs,
            ColorMeshFlushCpuMs = s.ColorMeshFlushCpuMs,
            ColorMergeFlushCpuMs = s.ColorMergeFlushCpuMs,
            ShadowCpuMs = s.ShadowCpuMs,
            PointLights = s.PointLights,
            PointShadowLights = s.PointShadowLights,
            DirectionalShadow = s.DirectionalShadow
        };

    private double Avg(Func<Statistics, double> selector) => _samples.Average(s => selector(s));
}
