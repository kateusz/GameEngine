using Evergine.Bindings.MeshOptimizer;
using Serilog;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshLodSimplifier
{
    private static readonly ILogger Logger = Log.ForContext(typeof(RuntimeMeshLodSimplifier));
    private static int _loggedMissingLibrary;

    internal static unsafe void Simplify(SourceSubmesh submesh)
    {
        var vertexCount = submesh.Vertices.Count;
        if (vertexCount == 0 || submesh.Indices.Count <= 12 || submesh.Indices.Count % 3 != 0)
            return;

        var floatsPerVertex = RuntimeMeshFormat.VertexLayoutStride / sizeof(float);
        var positions = new float[vertexCount * floatsPerVertex];
        var attributes = new float[vertexCount * 5];
        for (var i = 0; i < vertexCount; i++)
        {
            var vertex = submesh.Vertices[i];
            var baseIndex = i * floatsPerVertex;
            positions[baseIndex] = vertex.Position.X;
            positions[baseIndex + 1] = vertex.Position.Y;
            positions[baseIndex + 2] = vertex.Position.Z;
            var attribute = i * 5;
            attributes[attribute] = vertex.Normal.X;
            attributes[attribute + 1] = vertex.Normal.Y;
            attributes[attribute + 2] = vertex.Normal.Z;
            attributes[attribute + 3] = vertex.TexCoord.X;
            attributes[attribute + 4] = vertex.TexCoord.Y;
        }

        var weights = new float[] { 1f, 1f, 1f, 1f, 1f };
        const uint LockBorder = 1u << 0;
        const uint Sparse = 1u << 1;
        const float MaxRelativeError = 1f;

        var current = submesh.Indices.ToArray();
        float relative = 0f;
        try
        {
            float scale;
            fixed (float* positionPtr = positions)
                scale = MeshOptimizer.SimplifyScale(positionPtr, (UIntPtr)vertexCount, (UIntPtr)RuntimeMeshFormat.VertexLayoutStride);

            while (current.Length > 12)
            {
                uint target = (uint)System.Math.Max(12, (current.Length / 3 / 2) * 3);
                var destination = new uint[current.Length];
                float step = 0f;
                UIntPtr written;
                fixed (float* positionPtr = positions)
                fixed (float* attributePtr = attributes)
                fixed (float* weightPtr = weights)
                fixed (uint* indexPtr = current)
                fixed (uint* destinationPtr = destination)
                {
                    written = MeshOptimizer.SimplifyWithAttributes(
                        destinationPtr, indexPtr, (UIntPtr)current.Length,
                        positionPtr, (UIntPtr)vertexCount, (UIntPtr)RuntimeMeshFormat.VertexLayoutStride,
                        attributePtr, (UIntPtr)20, weightPtr, (UIntPtr)5,
                        vertex_lock: null,
                        (UIntPtr)target, MaxRelativeError,
                        LockBorder | Sparse, &step);
                }

                int count = (int)written;
                if (count <= 0 || count % 3 != 0 || count > current.Length * 0.75f)
                    break;

                var kept = destination.AsSpan(0, count).ToArray();
                if (kept.Any(index => index >= (uint)vertexCount))
                    break;

                relative = MathF.Max(relative * 1.5f, step);
                if (relative > MaxRelativeError)
                    break;

                var cacheOptimized = new uint[count];
                fixed (uint* optimizedPtr = cacheOptimized)
                fixed (uint* keptPtr = kept)
                    MeshOptimizer.OptimizeVertexCache(optimizedPtr, keptPtr, (UIntPtr)count, (UIntPtr)vertexCount);

                submesh.LodIndices.Add(cacheOptimized);
                submesh.LodErrors.Add(MathF.Max(relative * scale, 1e-8f));
                current = cacheOptimized;
            }
        }
        catch (Exception)
        {
            LogOnceMissingLibrary();
        }
    }

    private static void LogOnceMissingLibrary()
    {
        if (Interlocked.Exchange(ref _loggedMissingLibrary, 1) != 0)
            return;

        Logger.Warning("Mesh LOD simplifier unavailable; writing the mesh with no coarser levels.");
    }
}
