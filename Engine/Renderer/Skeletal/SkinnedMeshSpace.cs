using System.Numerics;

namespace Engine.Renderer.Skeletal;

/// <summary>Bake skinned mesh node transforms into vertex data so animation palettes share root space.</summary>
internal static class SkinnedMeshSpace
{
    public static AssimpSkinnedImport BakePartsToRootSpace(AssimpSkinnedImport import)
    {
        var parts = new List<AssimpModelPart>(import.Parts.Count);
        foreach (var part in import.Parts)
            parts.Add(BakePartToRootSpace(part));

        return new AssimpSkinnedImport(parts, import.Skeleton, import.Animations);
    }

    public static AssimpModelPart BakePartToRootSpace(AssimpModelPart part)
    {
        if (part.LocalToRoot == Matrix4x4.Identity)
            return part;

        var baked = new List<ModelSubmesh>(part.Submeshes.Count);
        foreach (var submesh in part.Submeshes)
            baked.Add(new ModelSubmesh(BakeMesh(CloneMesh(submesh.Mesh), part.LocalToRoot), submesh.Material));

        return new AssimpModelPart(part.Name, Matrix4x4.Identity, baked);
    }

    private static Mesh CloneMesh(Mesh mesh)
    {
        var clone = new Mesh(mesh.Name)
        {
            Vertices = [.. mesh.Vertices],
            Indices = [.. mesh.Indices]
        };
        return clone;
    }

    private static Mesh BakeMesh(Mesh mesh, Matrix4x4 localToRoot)
    {
        var upper = localToRoot with
        {
            M14 = 0,
            M24 = 0,
            M34 = 0,
            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };
        Matrix4x4.Invert(upper, out var invUpper);
        var normalMatrix = Matrix4x4.Transpose(invUpper);

        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            mesh.Vertices[i] = v with
            {
                Position = Vector3.Transform(v.Position, localToRoot),
                Normal = TransformUnitDirection(v.Normal, normalMatrix),
                Tangent = TransformUnitDirection(v.Tangent, normalMatrix),
                Bitangent = TransformUnitDirection(v.Bitangent, normalMatrix)
            };
        }

        return mesh;
    }

    private static Vector3 TransformUnitDirection(Vector3 direction, Matrix4x4 normalMatrix)
    {
        var transformed = Vector3.TransformNormal(direction, normalMatrix);
        return transformed.LengthSquared() > 1e-12f ? Vector3.Normalize(transformed) : transformed;
    }
}
