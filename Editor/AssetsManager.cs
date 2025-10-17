using System.Collections.Concurrent;

namespace Editor;

/// <summary>
/// Manages asset discovery, tracking, and hot reloading for the game engine editor.
/// Provides automatic file system monitoring and metadata tracking for project assets.
/// </summary>
public static class AssetsManager
{
    private static string _assetsPath = Path.Combine(Environment.CurrentDirectory, "assets");
    private static readonly ConcurrentDictionary<string, AssetMetadata> _assetRegistry = new();
    private static FileSystemWatcher? _watcher;
    private static readonly object _setupLock = new();

    /// <summary>
    /// Gets the current assets directory path.
    /// </summary>
    public static string AssetsPath => _assetsPath;

    /// <summary>
    /// Gets the total number of registered assets.
    /// </summary>
    public static int AssetCount => _assetRegistry.Count;

    /// <summary>
    /// Sets the assets directory path and initializes asset scanning and file watching.
    /// This method is idempotent - calling it multiple times with the same path has no effect.
    /// </summary>
    /// <param name="path">The absolute or relative path to the assets directory.</param>
    public static void SetAssetsPath(string path)
    {
        // Normalize the path to ensure consistent comparison
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        lock (_setupLock)
        {
            // Make idempotent - don't rescan if path hasn't changed
            if (_assetsPath == normalizedPath)
                return;

            _assetsPath = normalizedPath;
            ScanAssets();
            SetupFileWatcher();
        }
    }

    /// <summary>
    /// Shuts down the asset manager, disposing of file watchers and clearing the registry.
    /// Call this when closing the project or application.
    /// </summary>
    public static void Shutdown()
    {
        lock (_setupLock)
        {
            _watcher?.Dispose();
            _watcher = null;
            _assetRegistry.Clear();
        }
    }

    private static void ScanAssets()
    {
        if (!Directory.Exists(_assetsPath))
            return;

        _assetRegistry.Clear();

        try
        {
            var files = Directory.GetFiles(_assetsPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    var assetType = GetAssetType(extension);
                    if (assetType != AssetType.Unknown)
                    {
                        var relativePath = Path.GetRelativePath(_assetsPath, file);
                        _assetRegistry[relativePath] = new AssetMetadata
                        {
                            Path = file,
                            Type = assetType,
                            LastModified = File.GetLastWriteTime(file)
                        };
                    }
                }
                catch (Exception ex)
                {
                    // Log individual file errors but continue scanning
                    Console.WriteLine($"[AssetsManager] Error scanning file '{file}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AssetsManager] Error scanning assets directory: {ex.Message}");
        }
    }

    private static void SetupFileWatcher()
    {
        _watcher?.Dispose();

        if (!Directory.Exists(_assetsPath))
            return;

        try
        {
            _watcher = new FileSystemWatcher(_assetsPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                InternalBufferSize = 64 * 1024 // 64KB buffer to prevent overflow
            };

            _watcher.Created += OnAssetChanged;
            _watcher.Changed += OnAssetChanged;
            _watcher.Deleted += OnAssetDeleted;
            _watcher.Renamed += OnAssetRenamed;
            _watcher.Error += OnWatcherError;

            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AssetsManager] Error setting up file watcher: {ex.Message}");
        }
    }

    private static void OnAssetChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Check if file exists before processing (handles race conditions)
            if (!File.Exists(e.FullPath))
                return;

            var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            var assetType = GetAssetType(extension);
            if (assetType != AssetType.Unknown)
            {
                var relativePath = Path.GetRelativePath(_assetsPath, e.FullPath);
                _assetRegistry[relativePath] = new AssetMetadata
                {
                    Path = e.FullPath,
                    Type = assetType,
                    LastModified = File.GetLastWriteTime(e.FullPath)
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AssetsManager] Error processing asset change for '{e.FullPath}': {ex.Message}");
        }
    }

    private static void OnAssetDeleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            var relativePath = Path.GetRelativePath(_assetsPath, e.FullPath);
            _assetRegistry.TryRemove(relativePath, out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AssetsManager] Error processing asset deletion for '{e.FullPath}': {ex.Message}");
        }
    }

    private static void OnAssetRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            var oldRelativePath = Path.GetRelativePath(_assetsPath, e.OldFullPath);
            _assetRegistry.TryRemove(oldRelativePath, out _);
            OnAssetChanged(sender, e);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AssetsManager] Error processing asset rename from '{e.OldFullPath}' to '{e.FullPath}': {ex.Message}");
        }
    }

    private static void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        Console.WriteLine($"[AssetsManager] FileSystemWatcher error: {ex?.Message}");

        // Try to recover by recreating the watcher
        try
        {
            lock (_setupLock)
            {
                SetupFileWatcher();
            }
        }
        catch (Exception recoveryEx)
        {
            Console.WriteLine($"[AssetsManager] Failed to recover from watcher error: {recoveryEx.Message}");
        }
    }

    /// <summary>
    /// Determines the asset type based on file extension.
    /// </summary>
    /// <param name="extension">The file extension (including the leading dot).</param>
    /// <returns>The corresponding AssetType, or AssetType.Unknown if not recognized.</returns>
    private static AssetType GetAssetType(string extension)
    {
        return extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" => AssetType.Texture,
            ".obj" or ".fbx" or ".gltf" or ".glb" or ".model" => AssetType.Model,
            ".wav" or ".ogg" or ".mp3" => AssetType.Audio,
            ".vert" or ".frag" or ".glsl" or ".shader" => AssetType.Shader,
            ".scene" => AssetType.Scene,
            ".prefab" => AssetType.Prefab,
            ".cs" => AssetType.Script,
            ".ttf" or ".otf" or ".woff" or ".woff2" => AssetType.Font,
            _ => AssetType.Unknown
        };
    }

    /// <summary>
    /// Gets all registered assets, optionally filtered by type.
    /// </summary>
    /// <param name="type">The asset type to filter by, or AssetType.All for all assets.</param>
    /// <returns>An enumerable collection of asset metadata.</returns>
    /// <remarks>
    /// This method returns a snapshot of the registry. The collection is thread-safe
    /// but may not reflect changes made during enumeration.
    /// </remarks>
    public static IEnumerable<AssetMetadata> GetAssets(AssetType type = AssetType.All)
    {
        return type == AssetType.All
            ? _assetRegistry.Values
            : _assetRegistry.Values.Where(a => a.Type == type);
    }

    /// <summary>
    /// Attempts to retrieve asset metadata by relative path.
    /// </summary>
    /// <param name="relativePath">The relative path to the asset from the assets directory.</param>
    /// <param name="asset">When this method returns, contains the asset metadata if found; otherwise, the default value.</param>
    /// <returns>true if the asset was found; otherwise, false.</returns>
    public static bool TryGetAsset(string relativePath, out AssetMetadata asset)
    {
        return _assetRegistry.TryGetValue(relativePath, out asset);
    }

    /// <summary>
    /// Gets asset metadata by relative path, or null if not found.
    /// </summary>
    /// <param name="relativePath">The relative path to the asset from the assets directory.</param>
    /// <returns>The asset metadata if found; otherwise, null.</returns>
    public static AssetMetadata? GetAsset(string relativePath)
    {
        return _assetRegistry.TryGetValue(relativePath, out var asset) ? asset : null;
    }

    /// <summary>
    /// Manually refreshes the asset registry by rescanning the assets directory.
    /// This is typically not needed due to automatic file watching, but can be useful
    /// after external bulk operations or if file watching is disabled.
    /// </summary>
    public static void Refresh()
    {
        ScanAssets();
    }
}

/// <summary>
/// Contains metadata about a registered asset.
/// </summary>
public record struct AssetMetadata
{
    /// <summary>
    /// Gets the absolute path to the asset file.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Gets the asset type based on file extension.
    /// </summary>
    public AssetType Type { get; init; }

    /// <summary>
    /// Gets the last modification time of the asset file.
    /// </summary>
    public DateTime LastModified { get; init; }
}

/// <summary>
/// Defines the types of assets recognized by the asset management system.
/// </summary>
public enum AssetType
{
    /// <summary>Unknown or unrecognized asset type.</summary>
    Unknown,

    /// <summary>Image/texture files (.png, .jpg, .bmp, etc.).</summary>
    Texture,

    /// <summary>3D model files (.obj, .fbx, .gltf, etc.).</summary>
    Model,

    /// <summary>Audio files (.wav, .ogg, .mp3).</summary>
    Audio,

    /// <summary>Shader files (.vert, .frag, .glsl, etc.).</summary>
    Shader,

    /// <summary>Scene files (.scene).</summary>
    Scene,

    /// <summary>Prefab files (.prefab).</summary>
    Prefab,

    /// <summary>Script files (.cs).</summary>
    Script,

    /// <summary>Font files (.ttf, .otf, etc.).</summary>
    Font,

    /// <summary>All asset types (used for filtering).</summary>
    All
}
