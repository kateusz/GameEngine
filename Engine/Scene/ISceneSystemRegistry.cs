using ECS.Systems;

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
    IReadOnlyList<ISystem> PopulateSystemManager(ISystemManager systemManager);
}
