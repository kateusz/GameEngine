using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Renderer.Textures;
using Engine.Scene.Serializer;
using Serilog;

namespace Engine.Animation;

/// <summary>
/// Singleton manager for loading, caching, and lifecycle management of animation assets.
/// Implements reference counting to automatically unload unused assets.
/// </summary>
public class AnimationAssetManager
{
    private static readonly ILogger Logger = Log.ForContext<AnimationAssetManager>();

    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new RectangleConverter(),
            new JsonStringEnumConverter()
        }
    };

    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly IAssetsManager _assetsManager;
    private readonly ITextureFactory _textureFactory;

    public AnimationAssetManager(IAssetsManager assetsManager, ITextureFactory textureFactory)
    {
        _assetsManager = assetsManager;
        _textureFactory = textureFactory ?? throw new ArgumentNullException(nameof(textureFactory));
    }

    /// <summary>
    /// Load animation asset from JSON file.
    /// If already cached, increments reference count and returns cached asset.
    /// </summary>
    /// <param name="path">Relative path from Assets/ directory (e.g., "Animations/player.json")</param>
    /// <returns>Loaded animation asset, or null on error</returns>
    public AnimationAsset? LoadAsset(string path)
    {
        // Check cache first
        if (_cache.TryGetValue(path, out var entry))
        {
            entry.ReferenceCount++;
            entry.LastAccessTime = DateTime.UtcNow;
            Logger.Information("Animation asset cached hit: {Path} (RefCount: {RefCount})", path, entry.ReferenceCount);
            return entry.Asset;
        }

        // Not cached - load from disk
        try
        {
            // Resolve full path
            var fullPath = ResolveAssetPath(path);
            if (!File.Exists(fullPath))
            {
                Logger.Error("Animation asset not found: {Path}", fullPath);
                return null;
            }

            var jsonText = File.ReadAllText(fullPath);
            var animationAsset = JsonSerializer.Deserialize<AnimationAsset>(jsonText, DefaultSerializerOptions);
            if (animationAsset == null)
            {
                Logger.Error("Failed to deserialize animation asset: {Path}", path);
                return null;
            }

            // Load texture atlas
            var atlasFullPath = ResolveAssetPath(animationAsset.AtlasPath);
            var atlasTexture = _textureFactory.Create(atlasFullPath);

            // Assign texture to asset
            animationAsset.Atlas = atlasTexture;

            // Calculate UV coordinates for all frames
            foreach (var animationClip in animationAsset.Clips)
            {
                foreach (var animationFrame in animationClip.Frames)
                {
                    animationFrame.CalculateUvCoords(atlasTexture.Width, atlasTexture.Height);
                }
            }

            _cache[path] = new CacheEntry(animationAsset);

            Logger.Information("Animation asset loaded: {Path} ({ClipCount} clips)", path, animationAsset.Clips.Length);
            return animationAsset;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load animation asset: {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Unload animation asset and decrement reference count.
    /// If reference count reaches 0, disposes texture and removes from cache.
    /// </summary>
    /// <param name="path">Asset path</param>
    public void UnloadAsset(string path)
    {
        if (!_cache.TryGetValue(path, out var entry))
            return;

        entry.ReferenceCount--;
        Logger.Information("Animation asset unload: {Path} (RefCount: {RefCount})", path, entry.ReferenceCount);

        // If no more references, dispose and remove
        if (entry.ReferenceCount <= 0)
        {
            entry.Asset.Dispose();
            _cache.Remove(path);
            Logger.Information("Animation asset disposed: {Path}", path);
        }
    }

    /// <summary>
    /// Check if asset is cached.
    /// </summary>
    public bool IsCached(string path) => _cache.ContainsKey(path);

    /// <summary>
    /// Get reference count for cached asset.
    /// </summary>
    public int GetReferenceCount(string path) => _cache.TryGetValue(path, out var entry) ? entry.ReferenceCount : 0;

    /// <summary>
    /// Remove all assets with reference count = 0.
    /// Call this in editor after scene changes.
    /// </summary>
    public void ClearUnusedAssets()
    {
        var toRemove = _cache.Where(kvp => kvp.Value.ReferenceCount <= 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var path in toRemove)
        {
            _cache[path].Asset.Dispose();
            _cache.Remove(path);
        }

        if (toRemove.Count > 0)
            Logger.Information("Cleared {Count} unused animation assets", toRemove.Count);
    }

    /// <summary>
    /// Force unload all cached assets.
    /// Call this on scene unload.
    /// </summary>
    public void ClearAllAssets()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Asset.Dispose();
        }

        _cache.Clear();
        Logger.Information("All animation assets cleared");
    }

    /// <summary>
    /// Get number of cached assets.
    /// </summary>
    public int GetCachedAssetCount() => _cache.Count;

    /// <summary>
    /// Get total memory usage (approximate).
    /// </summary>
    public long GetTotalMemoryUsage()
    {
        long total = 0;
        foreach (var entry in _cache.Values)
        {
            var asset = entry.Asset;
            if (asset.Atlas != null)
            {
                // Texture memory: width × height × 4 bytes (RGBA)
                total += asset.Atlas.Width * asset.Atlas.Height * 4;
            }

            // Add metadata overhead (approximate)
            total += asset.Clips.Sum(c => c.Frames.Length * 256); // ~256 bytes per frame
        }

        return total;
    }

    /// <summary>
    /// Resolve asset path relative to Assets/ directory.
    /// </summary>
    private string ResolveAssetPath(string relativePath) => Path.Combine(_assetsManager.AssetsPath, relativePath);
}