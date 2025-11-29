using ECS.Systems;
using Engine.Scene.Systems;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneSystemRegistry(
    SpriteRenderingSystem spriteRenderingSystem,
    ModelRenderingSystem modelRenderingSystem,
    ScriptUpdateSystem scriptUpdateSystem,
    SubTextureRenderingSystem subTextureRenderingSystem,
    PhysicsDebugRenderSystem physicsDebugRenderSystem,
    AudioSystem audioSystem,
    AnimationSystem animationSystem,
    TileMapRenderSystem tileMapRenderSystem)
    : ISceneSystemRegistry
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemRegistry>();
    
    private readonly Lock _lock = new();
    
    public IReadOnlyList<ISystem> PopulateSystemManager(ISystemManager systemManager)
    {
        lock (_lock)
        {
            var systems = new List<ISystem>
            {
                tileMapRenderSystem,
                scriptUpdateSystem,
                spriteRenderingSystem,
                subTextureRenderingSystem,
                modelRenderingSystem,
                physicsDebugRenderSystem,
                audioSystem,
                animationSystem
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