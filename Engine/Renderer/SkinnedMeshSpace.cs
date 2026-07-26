using System.Numerics;

namespace Engine.Renderer;

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
            baked.Add(new ModelSubmesh(BakeMesh(submesh.Mesh, part.LocalToRoot), submesh.Material));

        return new AssimpModelPart(part.Name, Matrix4x4.Identity, baked);
    }

    private static Mesh BakeMesh(Mesh mesh, Matrix4x4 localToRoot)
    {
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            mesh.Vertices[i] = v with
            {
                Position = Vector3.Transform(v.Position, localToRoot),
                Normal = Vector3.TransformNormal(v.Normal, localToRoot),
                Tangent = Vector3.TransformNormal(v.Tangent, localToRoot),
                Bitangent = Vector3.TransformNormal(v.Bitangent, localToRoot)
            };
        }

        return mesh;
    }
}
