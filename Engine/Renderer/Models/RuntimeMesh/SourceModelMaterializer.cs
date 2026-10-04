using Engine.Renderer.Meshes;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class SourceModelMaterializer
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SourceModelMaterializer));

    public static (IReadOnlyList<Mesh> Submeshes, ModelSceneNode? SceneGraph) ToMeshes(
        SourceModel source,
        string sourcePath,
        ITextureFactory textureFactory)
    {
        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? string.Empty;
        var meshes = new List<Mesh>(source.Submeshes.Count);
        foreach (var submesh in source.Submeshes)
        {
            var mesh = new Mesh(submesh.Name);
            mesh.Vertices.AddRange(submesh.Vertices);
            mesh.Indices.AddRange(submesh.Indices);
            mesh.MetallicFactor = submesh.Metallic;
            mesh.RoughnessFactor = submesh.Roughness;
            mesh.BaseColorFactor = submesh.BaseColorFactor;
            mesh.DiffuseTexture = LoadTexture(textureFactory, sourceDirectory, submesh.DiffusePath, sRgb: true);
            mesh.NormalTexture = LoadTexture(textureFactory, sourceDirectory, submesh.NormalPath);
            mesh.MetallicRoughnessTexture =
                LoadTexture(textureFactory, sourceDirectory, submesh.MetallicRoughnessPath);
            mesh.OcclusionTexture = LoadTexture(textureFactory, sourceDirectory, submesh.OcclusionPath);
            meshes.Add(mesh);
        }

        var sceneGraph = RuntimeMeshSceneGraph.Unflatten(source.Nodes, source.Lights);
        return (meshes, sceneGraph);
    }

    private static Texture2D? LoadTexture(
        ITextureFactory textureFactory,
        string sourceDirectory,
        string relativePath,
        bool sRgb = false)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        var absolute = RuntimeMeshPaths.ResolveFromSourceDirectory(sourceDirectory, relativePath);
        if (!File.Exists(absolute))
        {
            Logger.Warning("Texture path missing on disk: {Path}", absolute);
            return null;
        }

        try
        {
            return textureFactory.Create(absolute, sRgb);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load texture {Path}", absolute);
            return null;
        }
    }
}
