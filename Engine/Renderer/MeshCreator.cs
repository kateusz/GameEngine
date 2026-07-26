using System.Numerics;
using Engine.Core;
using Math;
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

    public readonly record struct SplitPart(
        string PartName,
        string MeshRelativePath,
        int SubmeshStart,
        int SubmeshCount,
        Vector3 Translation,
        Vector3 Rotation,
        Vector3 Scale);

    public readonly record struct SplitResult(bool Success, IReadOnlyList<SplitPart> Parts, string? Error)
    {
        public static SplitResult Ok(IReadOnlyList<SplitPart> parts) => new(true, parts, null);
        public static SplitResult Fail(string error) => new(false, [], error);
    }

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

    private static bool TryResolvePaths(
        string sourceAbsolutePath,
        string projectAssetsRoot,
        string stem,
        out string assetsRoot,
        out string source,
        out string safeStem,
        out string? error)
    {
        assetsRoot = source = safeStem = string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(sourceAbsolutePath))
        {
            error = "Source path is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(projectAssetsRoot))
        {
            error = "Project assets root is empty.";
            return false;
        }

        if (!TrySanitizeStem(stem, out safeStem, out error))
            return false;

        assetsRoot = Path.GetFullPath(projectAssetsRoot);
        source = Path.GetFullPath(sourceAbsolutePath);
        if (!System.IO.File.Exists(source))
        {
            error = $"Source model not found: {source}";
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
        if (!TryResolvePaths(sourceAbsolutePath, projectAssetsRoot, stem,
                out var assetsRoot, out var source, out var safeStem, out var error))
            return Result.Fail(error!);

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

    /// <summary>
    /// Packs Assimp mesh-bearing nodes into one <c>models/{stem}.mesh</c> (ordered submeshes).
    /// Returns one <see cref="SplitPart"/> per node with submesh range + local transform.
    /// Textures relocated once under <c>models/textures/</c>.
    /// </summary>
    public static SplitResult CreateSplit(string sourceAbsolutePath, string projectAssetsRoot, string stem)
    {
        if (!TryResolvePaths(sourceAbsolutePath, projectAssetsRoot, stem,
                out var assetsRoot, out var source, out var safeStem, out var error))
            return SplitResult.Fail(error!);

        try
        {
            using var assimp = Assimp.GetApi();
            var importer = new AssimpModelImporter(assimp);
            var assimpParts = importer.ImportParts(source);

            if (assimpParts.Count == 0)
                return SplitResult.Fail($"Assimp produced no mesh nodes for: {source}");

            var modelsDir = Path.Combine(assetsRoot, "models");
            Directory.CreateDirectory(modelsDir);

            var allSubmeshes = assimpParts.SelectMany(p => p.Submeshes).ToList();
            TextureRelocator.Relocate(allSubmeshes, assetsRoot);

            var meshAbsolute = Path.Combine(modelsDir, $"{safeStem}.mesh");
            using (var stream = System.IO.File.Create(meshAbsolute))
                MeshWriter.Write(stream, new Model(allSubmeshes));

            var relative = PathBuilder.ToAssetRelativePath(meshAbsolute);
            var written = new List<SplitPart>(assimpParts.Count);
            var submeshCursor = 0;
            foreach (var part in assimpParts)
            {
                if (!MathHelpers.DecomposeTransform(
                        part.LocalToRoot, out var translation, out var rotation, out var scale))
                {
                    translation = Vector3.Zero;
                    rotation = Vector3.Zero;
                    scale = Vector3.One;
                }

                var count = part.Submeshes.Count;
                written.Add(new SplitPart(
                    part.Name, relative, submeshCursor, count, translation, rotation, scale));
                Logger.Debug(
                    "Split part source={Source} part={Part} range={Start}+{Count} → {Relative}",
                    source, part.Name, submeshCursor, count, relative);
                submeshCursor += count;
            }

            Logger.Information(
                "Created split model source={Source} → {Relative} parts={Parts} submeshes={Submeshes}",
                source, relative, written.Count, submeshCursor);
            return SplitResult.Ok(written);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "CreateSplit failed for {Source}", source);
            return SplitResult.Fail($"CreateSplit failed: {ex.Message}");
        }
    }

    /// <summary>Returns 1 if <c>models/{stem}.mesh</c> exists, else 0.</summary>
    public static int CountExistingSplitMeshes(string projectAssetsRoot, string stem)
    {
        if (!TrySanitizeStem(stem, out var safeStem, out _))
            return 0;

        var meshPath = Path.Combine(Path.GetFullPath(projectAssetsRoot), "models", $"{safeStem}.mesh");
        return System.IO.File.Exists(meshPath) ? 1 : 0;
    }
}
