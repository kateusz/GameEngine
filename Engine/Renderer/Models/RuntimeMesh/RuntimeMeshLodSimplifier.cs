using Evergine.Bindings.MeshOptimizer;
using Serilog;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshLodSimplifier
{
    private static readonly ILogger Logger = Log.ForContext(typeof(RuntimeMeshLodSimplifier));
    private static int _loggedNativeFailure;

    /// <summary>
    /// Builds coarser index lists from the submesh's original indices.
    /// Returns false when the native library fails — caller must not stamp a sibling as up to date.
    /// </summary>
    internal static unsafe bool TrySimplify(SourceSubmesh submesh)
    {
        submesh.LodIndices.Clear();
        submesh.LodErrors.Clear();

        var vertexCount = submesh.Vertices.Count;
        if (vertexCount == 0 || submesh.Indices.Count <= 12 || submesh.Indices.Count % 3 != 0)
            return true;

        // Packed XYZ only — meshoptimizer position stride is 12, not the 56-byte vertex layout.
        const int PositionStride = 12;
        var positions = new float[vertexCount * 3];
        var attributes = new float[vertexCount * 5];
        for (var i = 0; i < vertexCount; i++)
        {
            var vertex = submesh.Vertices[i];
            var baseIndex = i * 3;
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
        const float MaxRelativeError = 1f;

        var source = submesh.Indices.ToArray();
        var previousCount = source.Length;
        var previousError = 0f;
        try
        {
            float scale;
            fixed (float* positionPtr = positions)
                scale = MeshOptimizer.SimplifyScale(positionPtr, (UIntPtr)vertexCount, (UIntPtr)PositionStride);

            var target = (uint)System.Math.Max(12, (previousCount / 3 / 2) * 3);
            while (target >= 12 && target < previousCount * 0.75f)
            {
                var destination = new uint[source.Length];
                float step = 0f;
                UIntPtr written;
                fixed (float* positionPtr = positions)
                fixed (float* attributePtr = attributes)
                fixed (float* weightPtr = weights)
                fixed (uint* indexPtr = source)
                fixed (uint* destinationPtr = destination)
                {
                    written = MeshOptimizer.SimplifyWithAttributes(
                        destinationPtr, indexPtr, (UIntPtr)source.Length,
                        positionPtr, (UIntPtr)vertexCount, (UIntPtr)PositionStride,
                        attributePtr, (UIntPtr)20, weightPtr, (UIntPtr)5,
                        vertex_lock: null,
                        (UIntPtr)target, MaxRelativeError,
                        LockBorder, &step);
                }

                int count = (int)written;
                if (count <= 0 || count % 3 != 0 || count > previousCount * 0.75f)
                    break;

                var kept = destination.AsSpan(0, count).ToArray();
                if (kept.Any(index => index >= (uint)vertexCount))
                    break;

                if (step > MaxRelativeError)
                    break;

                var absolute = MathF.Max(step * scale, 1e-8f);
                if (absolute <= previousError)
                    break;

                var cacheOptimized = new uint[count];
                fixed (uint* optimizedPtr = cacheOptimized)
                fixed (uint* keptPtr = kept)
                    MeshOptimizer.OptimizeVertexCache(optimizedPtr, keptPtr, (UIntPtr)count, (UIntPtr)vertexCount);

                submesh.LodIndices.Add(cacheOptimized);
                submesh.LodErrors.Add(absolute);
                previousCount = count;
                previousError = absolute;
                target = (uint)System.Math.Max(12, (count / 3 / 2) * 3);
            }

            return true;
        }
        catch (Exception ex)
        {
            submesh.LodIndices.Clear();
            submesh.LodErrors.Clear();
            LogNativeFailure(ex);
            return false;
        }
    }

    private static void LogNativeFailure(Exception ex)
    {
        if (Interlocked.Exchange(ref _loggedNativeFailure, 1) != 0)
            return;

        Logger.Warning(ex, "Mesh LOD simplifier failed; not stamping the .mesh sibling as up to date.");
    }
}
