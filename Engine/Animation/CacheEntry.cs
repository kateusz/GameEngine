namespace Engine.Animation;

internal record CacheEntry(AnimationAsset Asset)
{
    public int ReferenceCount { get; set; } = 1;

    // todo: is that needed?
    public DateTime LastAccessTime { get; set; } = DateTime.Now;
}