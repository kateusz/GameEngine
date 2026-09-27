namespace Engine.Renderer;

/// <summary>
/// Per-frame 3D renderer statistics (GPU draws, geometry, pipeline culling / batching).
/// </summary>
public class Statistics
{
    // --- Graphics3D (GPU submissions) ---
    public uint DrawCalls { get; set; }
    public uint ColorDrawCalls { get; set; }
    public uint DirectionalShadowDrawCalls { get; set; }
    public uint PointShadowDrawCalls { get; set; }
    public uint CubeDraws { get; set; }
    public uint MeshDraws { get; set; }
    public uint InstancedDraws { get; set; }
    public uint Instances { get; set; }
    public long Vertices { get; set; }
    public long Indices { get; set; }

    // --- SceneRenderPipeline (color pass + lights) ---
    public int Renderers { get; set; }
    public int FrustumCulled { get; set; }
    public int ZoneCulled { get; set; }
    public int ShadowCasterCulled { get; set; }
    public int SingleMaterialDraws { get; set; }
    public int MultiMaterialDraws { get; set; }
    public int MaxBatchInstances { get; set; }
    public int PointLights { get; set; }
    public int PointShadowLights { get; set; }
    public int PointShadowCacheHits { get; set; }
    public bool DirectionalShadow { get; set; }
    public double ColorCpuMs { get; set; }
    public double ShadowCpuMs { get; set; }

    public long Triangles => Indices / 3;
}
