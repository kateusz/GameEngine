namespace Engine.Scene;

internal struct MeshPassStats
{
    public int Renderers;
    public int MeshDraws;
    public int Instances;
    public int CubeDraws;
    public int Culled;
    public int MissingMeshIndex;
    public int ShadowCasterCulled;
    public int ZoneCulled;
    public int SingleMaterialDraws;
    public int MultiMaterialDraws;
    public int MaxBatchInstances;
    public int MergedGroups;
    public long MergedVertices;
    public int MergedRebuilds;
    public long Vertices;
    public long Indices;
    public double CpuMs;
    public double CollectCpuMs;
    public double MeshFlushCpuMs;
    public double MergeFlushCpuMs;
}
