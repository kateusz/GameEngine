namespace Engine.Animation;

internal record CacheEntry(AnimationAsset Asset)
{
    public int ReferenceCount { get; set; } = 1;
}