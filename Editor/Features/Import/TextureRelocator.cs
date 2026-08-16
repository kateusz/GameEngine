using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Serilog;

namespace Editor.Features.Import;

/// <summary>
/// Copies Assimp-resolved texture files into <c>assets/models/textures/</c>
/// and rewrites <see cref="MeshMaterial"/> paths to project-relative strings via
/// <see cref="PathBuilder.ToAssetRelativePath"/>.
/// </summary>
internal static class TextureRelocator
{
    private static readonly ILogger Logger = Log.ForContext(typeof(TextureRelocator));

    public static void Relocate(IReadOnlyList<ModelSubmesh> submeshes, string projectAssetsRoot)
    {
        ArgumentNullException.ThrowIfNull(submeshes);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectAssetsRoot);

        var destDir = Path.Combine(projectAssetsRoot, "models", "textures");
        Directory.CreateDirectory(destDir);

        var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var submesh in submeshes)
        {
            var material = submesh.Material;
            material.AlbedoTexturePath = RelocateOne(material.AlbedoTexturePath, destDir, copied);
            material.MetallicRoughnessTexturePath = RelocateOne(material.MetallicRoughnessTexturePath, destDir, copied);
            material.NormalTexturePath = RelocateOne(material.NormalTexturePath, destDir, copied);
            material.EmissiveTexturePath = RelocateOne(material.EmissiveTexturePath, destDir, copied);
        }

        if (copied.Count > 0)
            Logger.Information("Relocated {Count} texture(s) → models/textures/", copied.Count);
    }

    private static string? RelocateOne(string? sourcePath, string destDir, Dictionary<string, string> copied)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return null;

        var normalizedSource = Path.GetFullPath(sourcePath.Replace('\\', Path.DirectorySeparatorChar));

        if (copied.TryGetValue(normalizedSource, out var existingRelative))
            return existingRelative;

        if (!File.Exists(normalizedSource))
        {
            Logger.Warning("Texture relocate skipped — source missing: {Path}", normalizedSource);
            return null;
        }

        var fileName = Path.GetFileName(normalizedSource);
        if (string.IsNullOrEmpty(fileName))
            fileName = "texture.bin";

        var destAbsolute = Path.GetFullPath(Path.Combine(destDir, fileName));
        // ponytail: IgnoreCase+Exists, not inode. Twin dirs that differ only by case
        // on a case-sensitive volume would skip a real copy.
        if (!string.Equals(normalizedSource, destAbsolute, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(destAbsolute))
            File.Copy(normalizedSource, destAbsolute, overwrite: true);

        var relative = PathBuilder.ToAssetRelativePath(destAbsolute);
        copied[normalizedSource] = relative;
        Logger.Debug("Relocated texture {Source} → {Relative}", normalizedSource, relative);
        return relative;
    }
}
