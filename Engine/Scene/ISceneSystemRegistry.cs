using ECS;

namespace Engine.Scene;

/// <summary>
/// Interface for a central registry of scene systems.
/// Provides system configuration that can be reused across multiple scenes.
/// </summary>
public interface ISceneSystemRegistry
{
    /// <summary>
    /// Populates the SystemManager with singleton systems shared across all scenes.
    /// Systems are registered as shared instances - NOT new instances per scene.
    /// </summary>
    /// <param name="systemManager">The system manager to populate with systems.</param>
    /// <returns>List of registered systems (singleton instances).</returns>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently for multiple scenes.
    /// All returned systems are singletons that will be shared across scenes.
    /// Systems are marked as shared to prevent multiple OnShutdown() calls when scenes are disposed.
    /// </remarks>
    IReadOnlyList<ISystem> PopulateSystemManager(ISystemManager systemManager);
}
