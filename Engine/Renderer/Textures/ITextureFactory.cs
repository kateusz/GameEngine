namespace Engine.Renderer.Textures;

/// <summary>
/// Factory interface for creating and managing texture resources with automatic caching.
/// </summary>
public interface ITextureFactory
{
    /// <summary>
    /// Gets a shared singleton 1x1 white texture.
    /// This method is thread-safe and ensures only one white texture is created.
    /// </summary>
    /// <returns>A shared white texture instance.</returns>
    Texture2D GetWhiteTexture();

    /// <summary>
    /// Creates or retrieves a cached texture from the specified file path.
    /// Uses weak-reference-based caching to prevent duplicate GPU resource allocations
    /// while allowing the garbage collector to reclaim unused textures.
    /// </summary>
    /// <param name="path">The file path to the texture resource.</param>
    /// <returns>A texture instance, either from cache or newly created.</returns>
    Texture2D Create(string path);

    /// <summary>
    /// Creates a new procedural texture with the specified dimensions.
    /// Procedural textures are not cached as they may have different content despite identical dimensions.
    /// </summary>
    /// <param name="width">The width of the texture in pixels.</param>
    /// <param name="height">The height of the texture in pixels.</param>
    /// <returns>A new texture instance with the specified dimensions.</returns>
    Texture2D Create(int width, int height);

    /// <summary>
    /// Clears all cached textures, removing weak references from the cache.
    /// Useful during scene transitions or when explicit memory management is required.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets the current number of entries in the texture cache.
    /// Note that this includes both live textures and dead weak references that haven't been cleaned up yet.
    /// </summary>
    /// <returns>The number of cache entries.</returns>
    int GetCacheSize();
}
