using Serilog;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshBuilder
{
    private static readonly ILogger Logger = Log.ForContext(typeof(RuntimeMeshBuilder));

    public static bool TryEnsureUpToDate(string sourcePath)
    {
        var normalizedPath = Path.GetFullPath(sourcePath);
        if (!RuntimeMeshPaths.IsModelSourceExtension(normalizedPath))
        {
            Logger.Warning("Unsupported model source extension: {Path}", normalizedPath);
            return false;
        }

        if (!File.Exists(normalizedPath))
        {
            Logger.Warning("Model source file not found: {Path}", normalizedPath);
            return false;
        }

        var siblingPath = RuntimeMeshPaths.SiblingPath(normalizedPath);
        var stamp = RuntimeMeshStamp.FromFile(normalizedPath);
        if (File.Exists(siblingPath)
            && RuntimeMeshReader.TryReadStamp(File.ReadAllBytes(siblingPath), out var fileStamp)
            && fileStamp.Matches(stamp))
            return true;

        using var importer = new AssimpModelImporter();
        var imported = importer.ImportSource(normalizedPath);
        if (imported == null || imported.Submeshes.Count == 0)
            return false;

        var lodFailed = 0;
        Parallel.ForEach(imported.Submeshes, submesh =>
        {
            if (!RuntimeMeshLodSimplifier.TrySimplify(submesh))
                Interlocked.Exchange(ref lodFailed, 1);
        });

        if (lodFailed != 0)
        {
            Logger.Warning("Skipping .mesh stamp for {Path}; LOD simplifier failed", normalizedPath);
            return false;
        }

        return RuntimeMeshWriter.TryWrite(siblingPath, imported, stamp);
    }
}
