using ECS;
using Engine.Scene.Systems;
using Serilog;

namespace Engine.Scene;

/// <summary>
/// Central registry for scene systems. Provides system configuration that can be reused across multiple scenes.
/// Uses factory delegates for system creation to support proper dependency injection.
/// </summary>
public class SceneSystemRegistry
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemRegistry>();

    // Singleton system instances - shared across ALL scenes
    private readonly SpriteRenderingSystem _spriteRenderingSystem;
    private readonly ModelRenderingSystem _modelRenderingSystem;
    private readonly ScriptUpdateSystem _scriptUpdateSystem;
    private readonly SubTextureRenderingSystem _subTextureRenderingSystem;
    private readonly PhysicsDebugRenderSystem _physicsDebugRenderSystem;

    private readonly Lock _lock = new();
    
    public SceneSystemRegistry(SpriteRenderingSystem spriteRenderingSystem, ModelRenderingSystem modelRenderingSystem,
        ScriptUpdateSystem scriptUpdateSystem, SubTextureRenderingSystem subTextureRenderingSystem,
        PhysicsDebugRenderSystem physicsDebugRenderSystem)
    {
        _spriteRenderingSystem = spriteRenderingSystem;
        _modelRenderingSystem = modelRenderingSystem;
        _scriptUpdateSystem = scriptUpdateSystem;
        _subTextureRenderingSystem = subTextureRenderingSystem;
        _physicsDebugRenderSystem = physicsDebugRenderSystem;
    }

    /// <summary>
    /// Populates the SystemManager with singleton systems shared across all scenes.
    /// Systems are registered as shared instances - NOT new instances per scene.
    /// </summary>
    /// <param name="systemManager">The system manager to populate with systems.</param>
    /// <returns>List of registered systems (singleton instances).</returns>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently for multiple scenes.
    /// All returned systems are singletons that will be shared across scenes.
    /// </remarks>
    public IReadOnlyList<ISystem> PopulateSystemManager(SystemManager systemManager)
    {
        lock (_lock)
        {
            var systems = new List<ISystem>
            {
                _scriptUpdateSystem,
                _spriteRenderingSystem,
                _subTextureRenderingSystem,
                _modelRenderingSystem,
                _physicsDebugRenderSystem
            };

            foreach (var system in systems)
            {
                try
                {
                    systemManager.RegisterSystem(system);
                    Logger.Debug("Registered singleton system: {SystemType}", system.GetType().Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to register system {SystemType}", system.GetType().Name);
                }
            }

            return systems.AsReadOnly();
        }
    }
}