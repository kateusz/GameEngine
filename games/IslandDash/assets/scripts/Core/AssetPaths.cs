using System.IO;

namespace IslandDash;

/// <summary>
/// Resolves project-relative asset paths for files the game reads itself (level and manifest text files).
/// The scripting API exposes no asset-path service, so the project root is found by walking up to game.config.json
/// from the game assembly (editor: &lt;project&gt;/.engine/...) or the executable directory (published runtime).
/// Model/texture/audio paths are passed to the engine as-is and resolved by the engine.
/// </summary>
public static class AssetPaths
{
    private const string ConfigFileName = "game.config.json";
    private static string? _assetsRoot;

    public static string Resolve(string relativePath)
    {
        var path = relativePath.Replace('\\', '/');
        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            path = path["assets/".Length..];
        return Path.Combine(AssetsRoot, path);
    }

    private static string AssetsRoot => _assetsRoot ??= FindAssetsRoot();

    private static string FindAssetsRoot()
    {
        string?[] starts =
        [
            Path.GetDirectoryName(typeof(AssetPaths).Assembly.Location),
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        ];

        foreach (var start in starts)
        {
            for (var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, ConfigFileName)) && Directory.Exists(Path.Combine(dir.FullName, "assets")))
                    return Path.Combine(dir.FullName, "assets");
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "assets");
    }
}
