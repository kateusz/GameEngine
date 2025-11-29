using ECS;
using Engine.Scene.Systems;
using Serilog;

namespace Engine.Scene;

/// <summary>
/// Central registry for scene systems. Provides system configuration that can be reused across multiple scenes.
/// Uses factory delegates for system creation to support proper dependency injection.
/// </summary>
internal sealed class SceneSystemRegistry : ISceneSystemRegistry
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemRegistry>();

    // Singleton system instances - shared across ALL scenes
    private readonly SpriteRenderingSystem _spriteRenderingSystem;
    private readonly ModelRenderingSystem _modelRenderingSystem;
    private readonly ScriptUpdateSystem _scriptUpdateSystem;
    private readonly SubTextureRenderingSystem _subTextureRenderingSystem;
    private readonly PhysicsDebugRenderSystem _physicsDebugRenderSystem;
    private readonly AudioSystem _audioSystem;
    private readonly AnimationSystem _animationSystem;
    private readonly TileMapRenderSystem _tileMapRenderSystem;

    private readonly Lock _lock = new();
    
    public SceneSystemRegistry(SpriteRenderingSystem spriteRenderingSystem, ModelRenderingSystem modelRenderingSystem,
        ScriptUpdateSystem scriptUpdateSystem, SubTextureRenderingSystem subTextureRenderingSystem,
        PhysicsDebugRenderSystem physicsDebugRenderSystem, AudioSystem audioSystem, AnimationSystem animationSystem,
        TileMapRenderSystem tileMapRenderSystem)
    {
        _spriteRenderingSystem = spriteRenderingSystem;
        _modelRenderingSystem = modelRenderingSystem;
        _scriptUpdateSystem = scriptUpdateSystem;
        _subTextureRenderingSystem = subTextureRenderingSystem;
        _physicsDebugRenderSystem = physicsDebugRenderSystem;
        _audioSystem = audioSystem;
        _animationSystem = animationSystem;
        _tileMapRenderSystem = tileMapRenderSystem;
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
    /// Systems are marked as shared to prevent multiple OnShutdown() calls when scenes are disposed.
    /// </remarks>
    public IReadOnlyList<ISystem> PopulateSystemManager(ISystemManager systemManager)
    {
        lock (_lock)
        {
            var systems = new List<ISystem>
            {
                _tileMapRenderSystem,
                _scriptUpdateSystem,
                _spriteRenderingSystem,
                _subTextureRenderingSystem,
                _modelRenderingSystem,
                _physicsDebugRenderSystem,
                _audioSystem,
                _animationSystem
            };

            foreach (var system in systems)
            {
                try
                {
                    // Mark as shared to prevent OnShutdown() being called multiple times
                    // when different scenes are disposed
                    systemManager.RegisterSystem(system, isShared: true);
                    Logger.Debug("Registered shared singleton system: {SystemType}", system.GetType().Name);
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