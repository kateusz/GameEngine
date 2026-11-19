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
        if (File.Exists(siblingPath) && SiblingStampMatchesSource(normalizedPath, siblingPath))
            return true;

        var stamp = RuntimeMeshStamp.FromFile(normalizedPath);
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

    // ponytail: mtime + size + importer version only, no SHA256 of the source GLB.
    // Ceiling: a same-size edit that also moves the source mtime backwards is missed — re-cook manually.
    private static bool SiblingStampMatchesSource(string sourcePath, string siblingPath)
    {
        var sourceInfo = new FileInfo(sourcePath);
        if (new FileInfo(siblingPath).LastWriteTimeUtc < sourceInfo.LastWriteTimeUtc)
            return false;

        Span<byte> header = stackalloc byte[RuntimeMeshFormat.HeaderSize];
        try
        {
            using var stream = File.OpenRead(siblingPath);
            if (stream.Read(header) < header.Length)
                return false;
        }
        catch
        {
            return false;
        }

        if (!RuntimeMeshReader.TryReadStamp(header, out var fileStamp))
            return false;

        return fileStamp.SourceSize == (ulong)sourceInfo.Length
               && fileStamp.ImporterVersion == RuntimeMeshFormat.ImporterVersion
               && fileStamp.PostProcessFlags == RuntimeMeshFormat.AssimpPostProcessFlags;
    }
}
