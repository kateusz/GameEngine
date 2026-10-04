using Silk.NET.Assimp;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshFormat
{
    public static uint AssimpPostProcessFlags =>
        (uint)(PostProcessSteps.Triangulate |
               PostProcessSteps.SortByPrimitiveType |
               PostProcessSteps.JoinIdenticalVertices |
               PostProcessSteps.GenerateNormals |
               PostProcessSteps.CalculateTangentSpace |
               PostProcessSteps.FlipUVs);

    public const int HeaderSize = 128;
    public const int HeaderReservedBytes = 12;
    public const uint Version = 1;
    public const uint Flags = 0;
    public const uint VertexLayoutId = 1;
    public const uint IndexType = 1;
    public const uint ImporterVersion = 6;
    public const uint SurfaceAlphaCutout = 1;
    public const uint SurfaceDoubleSided = 2;
    public const int VertexLayoutStride = 56;
    public const int MaxMeshCount = 4096;
    public const int MeshDecodeParallelThreshold = 32;
    public const int MaxNodeCount = 8192;
    public const int MaxLightCount = 256;
    public const int MaxStringBytes = 4096;
    public const int MaxVertexCount = 8_000_000;
    public const int MaxIndexCount = 24_000_000;
    public const long MaxFileBytes = 512L * 1024 * 1024;
    public const long MinFileBytes = HeaderSize;

    public const uint LightKindPoint = 1;
    public const uint LightKindDirectional = 2;

    public static ReadOnlySpan<byte> Magic => "MULESZA1"u8;
}
