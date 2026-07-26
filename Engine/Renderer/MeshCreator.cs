using Engine.Core;
using Serilog;
using Silk.NET.Assimp;

namespace Engine.Renderer;

/// <summary>
/// Thin Assimp → texture relocate → <see cref="MeshWriter"/> create façade.
/// </summary>
public static class MeshCreator
{
    private static readonly ILogger Logger = Log.ForContext(typeof(MeshCreator));

    public readonly record struct Result(bool Success, string? MeshRelativePath, string? Error)
    {
        public static Result Ok(string meshRelativePath) => new(true, meshRelativePath, null);
        public static Result Fail(string error) => new(false, null, error);
    }

    /// <summary>
    /// Rejects path separators / <c>..</c> so stem cannot escape <c>models/</c>.
    /// </summary>
    public static bool TrySanitizeStem(string stem, out string sanitized, out string? error)
    {
        sanitized = stem?.Trim() ?? string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            error = "Stem is empty.";
            return false;
        }

        if (sanitized.Contains("..", StringComparison.Ordinal)
            || sanitized.IndexOfAny([ '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ]) >= 0
            || sanitized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            error = $"Invalid stem '{stem}' — must be a single path segment (no separators or '..').";
            sanitized = string.Empty;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates <paramref name="sourceAbsolutePath"/> into
    /// <c>{projectAssetsRoot}/models/{stem}.mesh</c> with textures relocated under <c>models/textures/</c>.
    /// </summary>
    public static Result Create(string sourceAbsolutePath, string projectAssetsRoot, string stem)
    {
        if (string.IsNullOrWhiteSpace(sourceAbsolutePath))
            return Result.Fail("Source path is empty.");
        if (string.IsNullOrWhiteSpace(projectAssetsRoot))
            return Result.Fail("Project assets root is empty.");
        if (!TrySanitizeStem(stem, out var safeStem, out var stemError))
            return Result.Fail(stemError!);

        var assetsRoot = Path.GetFullPath(projectAssetsRoot);
        var source = Path.GetFullPath(sourceAbsolutePath);
        if (!System.IO.File.Exists(source))
            return Result.Fail($"Source model not found: {source}");

        try
        {
            using var assimp = Assimp.GetApi();
            var importer = new AssimpModelImporter(assimp);
            var submeshes = importer.Import(source);

            if (submeshes.Count == 0)
                return Result.Fail($"Assimp produced no meshes for: {source}");

            var modelsDir = Path.Combine(assetsRoot, "models");
            Directory.CreateDirectory(modelsDir);

            TextureRelocator.Relocate(submeshes, assetsRoot);

            var meshAbsolute = Path.Combine(modelsDir, $"{safeStem}.mesh");
            var model = new Model(submeshes);
            using (var stream = System.IO.File.Create(meshAbsolute))
                MeshWriter.Write(stream, model);

            var relative = PathBuilder.ToAssetRelativePath(meshAbsolute);
            Logger.Information("Created model source={Source} → {Relative}", source, relative);
            return Result.Ok(relative);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Create failed for {Source}", source);
            return Result.Fail($"Create failed: {ex.Message}");
        }
    }
}
