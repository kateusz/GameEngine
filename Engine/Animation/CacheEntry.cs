namespace Engine.Animation;

/// <summary>
/// Cache entry for tracking loaded animation assets with reference counting.
/// Used by AnimationAssetManager to manage asset lifecycle.
/// </summary>
internal record CacheEntry(AnimationAsset Asset)
{
    /// <summary>
    /// Number of active references to this asset.
    /// Asset is disposed when count reaches zero.
    /// </summary>
    public int ReferenceCount { get; set; } = 1;

    /// <summary>
    /// Last time this asset was accessed (for potential LRU eviction - currently unused).
    /// </summary>
    // TODO: Implement LRU eviction strategy or remove if not needed
    public DateTime LastAccessTime { get; set; } = DateTime.Now;
}