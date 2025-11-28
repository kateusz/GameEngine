namespace Engine.Animation;

public interface IAnimationAssetManager
{
    /// <summary>
    /// Load animation asset from JSON file.
    /// If already cached, increments reference count and returns cached asset.
    /// </summary>
    /// <param name="path">Relative path from Assets/ directory (e.g., "Animations/player.json")</param>
    /// <returns>Loaded animation asset, or null on error</returns>
    AnimationAsset? LoadAsset(string path);

    /// <summary>
    /// Unload animation asset and decrement reference count.
    /// If reference count reaches 0, disposes texture and removes from cache.
    /// </summary>
    /// <param name="path">Asset path</param>
    void UnloadAsset(string path);

    /// <summary>
    /// Check if asset is cached.
    /// </summary>
    bool IsCached(string path);

    /// <summary>
    /// Get reference count for cached asset.
    /// </summary>
    int GetReferenceCount(string path);

    /// <summary>
    /// Remove all assets with reference count = 0.
    /// Call this in editor after scene changes.
    /// </summary>
    void ClearUnusedAssets();

    /// <summary>
    /// Force unload all cached assets.
    /// Call this on scene unload.
    /// </summary>
    void ClearAllAssets();

    /// <summary>
    /// Get number of cached assets.
    /// </summary>
    int GetCachedAssetCount();

    /// <summary>
    /// Get total memory usage (approximate).
    /// </summary>
    long GetTotalMemoryUsage();
}