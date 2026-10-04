using Engine.Renderer.Meshes;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshTextureRefs
{
    private static readonly ILogger Logger = Log.ForContext(typeof(RuntimeMeshTextureRefs));

    public static IEnumerable<(string RelativePath, bool SRgb)> Enumerate(SourceModel source)
    {
        foreach (var submesh in source.Submeshes)
        {
            foreach (var item in Enumerate(submesh))
                yield return item;
        }
    }

    public static IEnumerable<(string RelativePath, bool SRgb)> Enumerate(SourceSubmesh submesh)
    {
        if (!string.IsNullOrEmpty(submesh.DiffusePath))
            yield return (submesh.DiffusePath, true);
        if (!string.IsNullOrEmpty(submesh.NormalPath))
            yield return (submesh.NormalPath, false);
        if (!string.IsNullOrEmpty(submesh.MetallicRoughnessPath))
            yield return (submesh.MetallicRoughnessPath, false);
        if (!string.IsNullOrEmpty(submesh.OcclusionPath))
            yield return (submesh.OcclusionPath, false);
        if (!string.IsNullOrEmpty(submesh.EmissivePath))
            yield return (submesh.EmissivePath, true);
    }

    public static void BindTextures(
        Mesh mesh,
        SourceSubmesh submesh,
        string sourceDirectory,
        ITextureFactory textureFactory)
    {
        mesh.DiffuseTexture = Load(textureFactory, sourceDirectory, submesh.DiffusePath, sRgb: true);
        mesh.NormalTexture = Load(textureFactory, sourceDirectory, submesh.NormalPath);
        mesh.MetallicRoughnessTexture = Load(textureFactory, sourceDirectory, submesh.MetallicRoughnessPath);
        mesh.OcclusionTexture = Load(textureFactory, sourceDirectory, submesh.OcclusionPath);
        mesh.EmissiveTexture = Load(textureFactory, sourceDirectory, submesh.EmissivePath, sRgb: true);
    }

    private static Texture2D? Load(
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
            Logger.Warning(ex, "Failed to load texture: {Path}", absolute);
            return null;
        }
    }
}
