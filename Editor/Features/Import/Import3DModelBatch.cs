using Editor.UI.Drawers;
using Engine.Renderer;

namespace Editor.Features.Import;

public static class Import3DModelBatch
{
    public const string NoProjectError = "Open a project before importing 3D models.";

    public static readonly string[] SupportedExtensions = [".fbx", ".glb", ".gltf"];

    public readonly record struct Failure(string Source, string Error);

    public readonly record struct ImportBatchSummary(int Succeeded, IReadOnlyList<Failure> Failures);

    public readonly record struct DuplicateDestination(
        string DestinationPath,
        IReadOnlyList<string> Sources);

    /// <summary>
    /// Non-recursive: single allowlisted file, or files in a folder with allowlisted extensions.
    /// </summary>
    public static IReadOnlyList<string> EnumerateSources(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return [];

        var full = Path.GetFullPath(path.Trim());
        if (File.Exists(full))
            return IsSupportedExtension(full) ? [full] : [];

        if (!Directory.Exists(full))
            return [];

        return Directory.EnumerateFiles(full)
            .Where(IsSupportedExtension)
            .Select(Path.GetFullPath)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsSupportedExtension(string path)
    {
        var ext = Path.GetExtension(path);
        return SupportedExtensions.Any(s =>
            string.Equals(s, ext, StringComparison.OrdinalIgnoreCase));
    }

    public static string DestinationMeshPath(string projectAssetsRoot, string sourceAbsolutePath)
    {
        var stem = Path.GetFileNameWithoutExtension(sourceAbsolutePath);
        return Path.Combine(projectAssetsRoot, "models", $"{stem}.mesh");
    }

    /// <summary>
    /// Sources that share a destination stem (e.g. robot.fbx + robot.glb → robot.mesh).
    /// </summary>
    public static IReadOnlyList<DuplicateDestination> FindDuplicateDestinations(
        IReadOnlyList<string> sources,
        string projectAssetsRoot)
    {
        var byDest = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            var dest = DestinationMeshPath(projectAssetsRoot, source);
            if (!byDest.TryGetValue(dest, out var list))
            {
                list = [];
                byDest[dest] = list;
            }

            list.Add(source);
        }

        return byDest
            .Where(kv => kv.Value.Count > 1)
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new DuplicateDestination(kv.Key, kv.Value))
            .ToList();
    }

    public static int CountExistingDestinations(
        IReadOnlyList<string> sources,
        string projectAssetsRoot)
    {
        var count = 0;
        foreach (var source in sources)
        {
            if (File.Exists(DestinationMeshPath(projectAssetsRoot, source)))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Ensures <c>assets/models</c> exists under the project assets root.
    /// </summary>
    public static void EnsureModelsDirectory(string projectAssetsRoot) =>
        Directory.CreateDirectory(Path.Combine(projectAssetsRoot, "models"));

    /// <summary>
    /// When destinations already exist and <paramref name="overwriteConfirmed"/> is false,
    /// returns false and does not create meshes.
    /// </summary>
    public static bool TryImportBatch(
        IReadOnlyList<string> sources,
        string projectAssetsRoot,
        bool overwriteConfirmed,
        out ImportBatchSummary? summary)
    {
        summary = null;
        if (sources.Count == 0)
        {
            summary = new ImportBatchSummary(0, []);
            return true;
        }

        var duplicates = FindDuplicateDestinations(sources, projectAssetsRoot);
        if (duplicates.Count > 0)
        {
            summary = new ImportBatchSummary(0, [
                new Failure(string.Empty, FormatDuplicateDestinationMessage(duplicates))
            ]);
            return true;
        }

        if (CountExistingDestinations(sources, projectAssetsRoot) > 0 && !overwriteConfirmed)
            return false;

        EnsureModelsDirectory(projectAssetsRoot);

        var succeeded = 0;
        var failures = new List<Failure>();
        foreach (var source in sources)
        {
            var stem = Path.GetFileNameWithoutExtension(source);
            MeshCreator.Result result;
            try
            {
                result = MeshCreator.Create(source, projectAssetsRoot, stem);
            }
            catch (Exception ex)
            {
                result = MeshCreator.Result.Fail(ex.Message);
            }

            if (result.Success)
                succeeded++;
            else
                failures.Add(new Failure(source, result.Error ?? "Unknown error"));
        }

        summary = new ImportBatchSummary(succeeded, failures);
        return true;
    }

    public static MessageType SummaryMessageType(int succeeded, int failed)
    {
        if (failed == 0)
            return MessageType.Success;
        if (succeeded == 0)
            return MessageType.Error;
        return MessageType.Warning;
    }

    public static string FormatSummaryMessage(
        ImportBatchSummary summary,
        string sourceDisplay)
    {
        var lines = new List<string>
        {
            $"Imported: {summary.Succeeded}",
            $"Failed: {summary.Failures.Count}",
            string.Empty,
            "Source:",
            sourceDisplay
        };

        if (summary.Failures.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Failures:");
            foreach (var f in summary.Failures)
                lines.Add($"- {Path.GetFileName(f.Source)}: {f.Error}");
        }

        return string.Join('\n', lines);
    }

    public static string FormatOverwriteMessage(int conflictCount) =>
        $"{conflictCount} destination *.mesh file(s) already exist under assets/models/.\n\n" +
        "Overwrite all conflicts in this import batch?";

    public static string FormatDuplicateDestinationMessage(
        IReadOnlyList<DuplicateDestination> duplicates)
    {
        var lines = new List<string>
        {
            "Multiple inputs map to the same destination *.mesh file.",
            "Rename or remove conflicting sources before importing.",
            string.Empty,
            "Conflicts:"
        };

        foreach (var d in duplicates)
        {
            var names = string.Join(", ", d.Sources.Select(Path.GetFileName));
            lines.Add($"- {Path.GetFileName(d.DestinationPath)} ← {names}");
        }

        return string.Join('\n', lines);
    }
}
