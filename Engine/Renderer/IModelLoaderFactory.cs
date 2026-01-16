namespace Engine.Renderer;

/// <summary>
/// Factory for creating model loader instances.
/// </summary>
public interface IModelLoaderFactory
{
    /// <summary>
    /// Creates a model loader for the current platform/API configuration.
    /// </summary>
    IModelLoader Create();
}
