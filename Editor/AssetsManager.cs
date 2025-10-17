namespace Editor;

public static class AssetsManager
{
    private static string _assetsPath = Path.Combine(Environment.CurrentDirectory, "assets");
    private static readonly Dictionary<string, AssetMetadata> _assetRegistry = new();
    private static FileSystemWatcher? _watcher;

    public static string AssetsPath => _assetsPath;

    public static void SetAssetsPath(string path)
    {
        _assetsPath = path;
        ScanAssets();
        SetupFileWatcher();
    }

    private static void ScanAssets()
    {
        if (!Directory.Exists(_assetsPath))
            return;

        _assetRegistry.Clear();

        var files = Directory.GetFiles(_assetsPath, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
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
    }

    private static void SetupFileWatcher()
    {
        _watcher?.Dispose();

        if (!Directory.Exists(_assetsPath))
            return;

        _watcher = new FileSystemWatcher(_assetsPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        _watcher.Created += OnAssetChanged;
        _watcher.Changed += OnAssetChanged;
        _watcher.Deleted += OnAssetDeleted;
        _watcher.Renamed += OnAssetRenamed;

        _watcher.EnableRaisingEvents = true;
    }

    private static void OnAssetChanged(object sender, FileSystemEventArgs e)
    {
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

    private static void OnAssetDeleted(object sender, FileSystemEventArgs e)
    {
        var relativePath = Path.GetRelativePath(_assetsPath, e.FullPath);
        _assetRegistry.Remove(relativePath);
    }

    private static void OnAssetRenamed(object sender, RenamedEventArgs e)
    {
        var oldRelativePath = Path.GetRelativePath(_assetsPath, e.OldFullPath);
        _assetRegistry.Remove(oldRelativePath);
        OnAssetChanged(sender, e);
    }

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

    public static IEnumerable<AssetMetadata> GetAssets(AssetType type = AssetType.All)
    {
        return type == AssetType.All
            ? _assetRegistry.Values
            : _assetRegistry.Values.Where(a => a.Type == type);
    }

    public static bool TryGetAsset(string relativePath, out AssetMetadata asset)
    {
        return _assetRegistry.TryGetValue(relativePath, out asset);
    }

    public static void Refresh()
    {
        ScanAssets();
    }
}

public record struct AssetMetadata
{
    public string Path { get; init; }
    public AssetType Type { get; init; }
    public DateTime LastModified { get; init; }
}

public enum AssetType
{
    Unknown,
    Texture,
    Model,
    Audio,
    Shader,
    Scene,
    Prefab,
    Script,
    Font,
    All
}