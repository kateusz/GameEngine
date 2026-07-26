using Editor.UI.Drawers;
using Engine.Renderer;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.Features.Import;

public static class Import3DModelBatch
{
    public const string NoProjectError = "Open a project before importing 3D models.";
    public const string NoActiveSceneNote = "Meshes written; no active scene — skipped hierarchy spawn.";

    public static readonly string[] SupportedExtensions = [".fbx", ".glb", ".gltf"];

    public readonly record struct Failure(string Source, string Error);
    public readonly record struct SourceImport(string Source, IReadOnlyList<MeshCreator.SplitPart> Parts);

    public readonly record struct ImportBatchSummary(
        int Succeeded,
        IReadOnlyList<Failure> Failures,
        IReadOnlyList<SourceImport> Sources);

    public readonly record struct DuplicateDestination(string Stem, IReadOnlyList<string> Sources);

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

    public static IReadOnlyList<DuplicateDestination> FindDuplicateDestinations(
        IReadOnlyList<string> sources)
    {
        var byStem = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            var stem = Path.GetFileNameWithoutExtension(source);
            if (!byStem.TryGetValue(stem, out var list))
            {
                list = [];
                byStem[stem] = list;
            }

            list.Add(source);
        }

        return byStem
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
            count += MeshCreator.CountExistingSplitMeshes(
                projectAssetsRoot, Path.GetFileNameWithoutExtension(source));
        return count;
    }

    public static bool TryImportBatch(
        IReadOnlyList<string> sources,
        string projectAssetsRoot,
        bool overwriteConfirmed,
        out ImportBatchSummary? summary)
    {
        summary = null;
        if (sources.Count == 0)
        {
            summary = new ImportBatchSummary(0, [], []);
            return true;
        }

        var duplicates = FindDuplicateDestinations(sources);
        if (duplicates.Count > 0)
        {
            summary = new ImportBatchSummary(0, [
                new Failure(string.Empty, FormatDuplicateDestinationMessage(duplicates))
            ], []);
            return true;
        }

        if (CountExistingDestinations(sources, projectAssetsRoot) > 0 && !overwriteConfirmed)
            return false;

        var succeeded = 0;
        var failures = new List<Failure>();
        var results = new List<SourceImport>();
        foreach (var source in sources)
        {
            var result = MeshCreator.CreateSplit(
                source, projectAssetsRoot, Path.GetFileNameWithoutExtension(source));

            if (result.Success)
            {
                succeeded++;
                results.Add(new SourceImport(source, result.Parts));
            }
            else
            {
                failures.Add(new Failure(source, result.Error ?? "Unknown error"));
            }
        }

        summary = new ImportBatchSummary(succeeded, failures, results);
        return true;
    }
    
    public static string SpawnHierarchy(
        IScene scene,
        string parentName,
        IReadOnlyList<MeshCreator.SplitPart> parts)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (parts.Count == 0)
            return "No parts to spawn.";

        var safeParent = string.IsNullOrWhiteSpace(parentName) ? "ImportedModel" : parentName.Trim();
        var parent = scene.CreateEntity(safeParent);
        parent.AddComponent(new TransformComponent());

        foreach (var part in parts)
        {
            var child = scene.CreateEntity(part.PartName);
            child.AddComponent(new TransformComponent
            {
                Translation = part.Translation,
                Rotation = part.Rotation,
                Scale = part.Scale
            });
            child.AddComponent(new ModelRendererComponent
            {
                ModelPath = part.MeshRelativePath,
                SubmeshStart = part.SubmeshStart,
                SubmeshCount = part.SubmeshCount
            });
            scene.SetParent(child, parent);
        }

        return $"Spawned parent '{safeParent}' with {parts.Count} child mesh(es).";
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
        string sourceDisplay,
        string? spawnNote = null)
    {
        var partCount = summary.Sources.Sum(s => s.Parts.Count);
        var lines = new List<string>
        {
            $"Imported sources: {summary.Succeeded}",
            $"Failed: {summary.Failures.Count}",
            $"Mesh files written: {summary.Succeeded}",
            $"Hierarchy parts: {partCount}",
            string.Empty,
            "Source:",
            sourceDisplay
        };

        if (!string.IsNullOrWhiteSpace(spawnNote))
        {
            lines.Add(string.Empty);
            lines.Add(spawnNote);
        }

        if (summary.Failures.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Failures:");
            foreach (var f in summary.Failures)
            {
                var name = string.IsNullOrEmpty(f.Source) ? "(batch)" : Path.GetFileName(f.Source);
                lines.Add($"- {name}: {f.Error}");
            }
        }

        return string.Join('\n', lines);
    }

    public static string FormatOverwriteMessage(int conflictCount) =>
        $"{conflictCount} destination .mesh file(s) already exist under assets/models/.\n\n" +
        "Overwrite all conflicts in this import batch?";

    public static string FormatDuplicateDestinationMessage(
        IReadOnlyList<DuplicateDestination> duplicates)
    {
        var lines = new List<string>
        {
            "Multiple inputs map to the same destination stem (stem.mesh).",
            "Rename or remove conflicting sources before importing.",
            string.Empty,
            "Conflicts:"
        };

        foreach (var d in duplicates)
        {
            var names = string.Join(", ", d.Sources.Select(Path.GetFileName));
            lines.Add($"- {d.Stem}.mesh ← {names}");
        }

        return string.Join('\n', lines);
    }
}
