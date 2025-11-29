namespace Engine.Renderer.Shaders;

/// <summary>
/// Factory interface for creating and managing shader resources with automatic caching.
/// </summary>
public interface IShaderFactory
{
    /// <summary>
    /// Creates or retrieves a cached shader instance for the specified vertex and fragment shader paths.
    /// Uses weak references to allow garbage collection when shaders are no longer in use.
    /// </summary>
    /// <param name="vertPath">Path to the vertex shader file.</param>
    /// <param name="fragPath">Path to the fragment shader file.</param>
    /// <returns>A shader instance, either from cache or newly created.</returns>
    IShader Create(string vertPath, string fragPath);

    /// <summary>
    /// Clears the shader cache, forcing all subsequent shader requests to recompile.
    /// Useful for development scenarios where shaders need to be reloaded.
    /// </summary>
    void ClearCache();
}
