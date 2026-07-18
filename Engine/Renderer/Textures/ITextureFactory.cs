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
    /// Gets a shared singleton 1x1 black texture (RGBA 0,0,0,255).
    /// This method is thread-safe and ensures only one black texture is created.
    /// </summary>
    /// <returns>A shared black texture instance.</returns>
    Texture2D GetBlackTexture();

    /// <summary>
    /// Gets a shared singleton 1x1 flat normal map texture (RGBA 128,128,255,255).
    /// This method is thread-safe and ensures only one flat normal texture is created.
    /// </summary>
    /// <returns>A shared flat normal texture instance.</returns>
    Texture2D GetFlatNormalTexture();

    /// <summary>
    /// Creates or retrieves a cached texture from the specified file path.
    /// Returns a cached instance when the same path+colorspace was loaded before.
    /// The factory owns cached textures and disposes them via <see cref="ClearCache"/> or container shutdown.
    /// </summary>
    /// <param name="path">The file path to the texture resource.</param>
    /// <param name="sRgb">
    /// When true, upload as sRGB (albedo/base color). When false, upload as linear
    /// (metallic-roughness, normals, AO, data maps).
    /// </param>
    /// <returns>A texture instance, either from cache or newly created.</returns>
    Texture2D Create(string path, bool sRgb = false);

    /// <summary>
    /// Creates a new procedural texture with the specified dimensions.
    /// Procedural textures are not cached as they may have different content despite identical dimensions.
    /// </summary>
    /// <param name="width">The width of the texture in pixels.</param>
    /// <param name="height">The height of the texture in pixels.</param>
    /// <returns>A new texture instance with the specified dimensions.</returns>
    Texture2D Create(int width, int height);

    /// <summary>
    /// Disposes and removes all path-cached textures.
    /// Useful during scene transitions or when explicit memory management is required.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets the current number of entries in the texture cache.
    /// Returns the number of path-cached textures currently held by the factory.
    /// </summary>
    /// <returns>The number of cache entries.</returns>
    int GetCacheSize();
}
