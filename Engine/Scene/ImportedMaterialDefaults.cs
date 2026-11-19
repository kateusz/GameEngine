using Engine.Renderer.Meshes;
using SceneComponents.Rendering;

namespace Engine.Scene;

/// <summary>
/// Seeds component PBR from a mesh file once at import/spawn time.
/// Render paths must not mutate serialized materials — use ResolveEffectivePbr instead.
/// </summary>
public static class ImportedMaterialDefaults
{
    public static void Apply(ModelRendererComponent renderer, Mesh submesh)
    {
        if (renderer.MeshIndex is not int || renderer.FactorsSeeded)
            return;

        renderer.FactorsSeeded = true;
        if (renderer.Metallic != 0f || renderer.Roughness != 0.5f || renderer.Ao != 1f)
            return;

        renderer.Metallic = submesh.MetallicFactor;
        renderer.Roughness = submesh.RoughnessFactor;
    }
}
