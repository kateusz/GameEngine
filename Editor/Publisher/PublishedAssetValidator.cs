using System.Text.Json;

namespace Editor.Publisher;

/// <summary>
/// Validates that packaged asset directories and scene/prefab path references exist on disk.
/// </summary>
public static class PublishedAssetValidator
{
    private static readonly HashSet<string> PathPropertyNames = new(StringComparer.Ordinal)
    {
        "TexturePath",
        "AudioClipPath",
        "ModelPath",
        "SkeletonPath",
        "ClipPath"
    };

    public static PublishResult ValidateAssetsDirectory(string projectRoot)
    {
        var assetsPath = Path.Combine(projectRoot, "assets");
        if (Directory.Exists(assetsPath))
            return new PublishResult { Success = true };

        return PublishResult.Failed($"Assets directory not found at {assetsPath}. Cannot publish without assets.");
    }

    /// <summary>
    /// Scans scene/prefab JSON under <paramref name="assetsRoot"/> for known path properties
    /// and verifies each referenced file exists under that assets root.
    /// </summary>
    public static PublishResult ValidateAssetReferences(string assetsRoot)
    {
        if (!Directory.Exists(assetsRoot))
            return PublishResult.Failed($"Assets directory not found at {assetsRoot}.");

        var missing = new List<string>();
        foreach (var file in EnumerateSceneAndPrefabFiles(assetsRoot))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                CollectMissingPaths(doc.RootElement, assetsRoot, file, missing);
            }
            catch (JsonException ex)
            {
                return PublishResult.Failed($"Invalid JSON in '{RelativeToAssets(assetsRoot, file)}': {ex.Message}");
            }
        }

        if (missing.Count == 0)
            return new PublishResult { Success = true };

        var unique = missing.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        return PublishResult.Failed(
            "Missing asset references:\n" + string.Join("\n", unique.Select(p => $"  - {p}")));
    }

    private static IEnumerable<string> EnumerateSceneAndPrefabFiles(string assetsRoot) =>
        Directory.EnumerateFiles(assetsRoot, "*.scene", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(assetsRoot, "*.prefab", SearchOption.AllDirectories));

    private static void CollectMissingPaths(
        JsonElement element,
        string assetsRoot,
        string sourceFile,
        List<string> missing)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (PathPropertyNames.Contains(prop.Name)
                        && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var path = prop.Value.GetString();
                        if (string.IsNullOrWhiteSpace(path))
                            continue;

                        if (!AssetFileExists(assetsRoot, path))
                        {
                            missing.Add(
                                $"{path} (from {RelativeToAssets(assetsRoot, sourceFile)}, property {prop.Name})");
                        }
                    }
                    else
                    {
                        CollectMissingPaths(prop.Value, assetsRoot, sourceFile, missing);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectMissingPaths(item, assetsRoot, sourceFile, missing);
                break;
        }
    }

    private static bool AssetFileExists(string assetsRoot, string path)
    {
        var relative = path.Replace('\\', '/');
        if (relative.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            relative = relative[7..];

        var fullPath = Path.GetFullPath(Path.Combine(assetsRoot, relative));
        var assetsFull = Path.GetFullPath(assetsRoot);
        var underAssets = fullPath.StartsWith(assetsFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || fullPath.Equals(assetsFull, StringComparison.OrdinalIgnoreCase);
        return underAssets && File.Exists(fullPath);
    }

    private static string RelativeToAssets(string assetsRoot, string file) =>
        Path.GetRelativePath(assetsRoot, file).Replace('\\', '/');
}
