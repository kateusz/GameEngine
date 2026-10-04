using System.Numerics;
using Engine.Renderer.Meshes;

namespace Engine.Renderer.Models.RuntimeMesh;

internal sealed class SourceModel(
    IReadOnlyList<SourceSubmesh> submeshes,
    IReadOnlyList<SourceNode> nodes,
    IReadOnlyList<SourceLight> lights)
{
    public IReadOnlyList<SourceSubmesh> Submeshes { get; } = submeshes;
    public IReadOnlyList<SourceNode> Nodes { get; } = nodes;
    public IReadOnlyList<SourceLight> Lights { get; } = lights;
}

internal sealed class SourceSubmesh
{
    public required string Name { get; init; }
    public List<Mesh.Vertex> Vertices { get; } = [];
    public List<uint> Indices { get; } = [];
    public Vector3 BoundsMin { get; set; }
    public Vector3 BoundsMax { get; set; }
    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.5f;
    public Vector3 BaseColorFactor { get; set; } = Vector3.One;
    public Vector3 Emissive { get; set; }
    public string DiffusePath { get; set; } = string.Empty;
    public string NormalPath { get; set; } = string.Empty;
    public string MetallicRoughnessPath { get; set; } = string.Empty;
    public string OcclusionPath { get; set; } = string.Empty;
    public bool AlphaCutout { get; set; }
    public bool DoubleSided { get; set; }
    public float AlphaCutoff { get; set; } = 0.5f;
}

internal sealed class SourceNode
{
    public required string Name { get; init; }
    public Matrix4x4 LocalTransform { get; init; } = Matrix4x4.Identity;
    public List<int> MeshIndices { get; } = [];
    public int LightIndex { get; set; } = -1;
    public List<int> ChildIndices { get; } = [];
}

internal abstract class SourceLight;

internal sealed class SourcePointLight(Vector4 color, float intensity, float range) : SourceLight
{
    public Vector4 Color { get; } = color;
    public float Intensity { get; } = intensity;
    public float Range { get; } = range;
}

internal sealed class SourceDirectionalLight(Vector4 color, Vector3 direction) : SourceLight
{
    public Vector4 Color { get; } = color;
    public Vector3 Direction { get; } = direction;
}
