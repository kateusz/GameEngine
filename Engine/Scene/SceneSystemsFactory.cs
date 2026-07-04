using System.Numerics;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using ECS.Systems;
using Engine.Audio;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene.Systems;
using Engine.Scripting;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneSystemsFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    DebugSettings debugSettings,
    IScriptEngine scriptEngine,
    IAudioEngine audioEngine,
    IAudioEffectFactory effectFactory) : ISceneSystemsFactory
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemsFactory>();

    public void PopulateSystemManager(ISystemManager systemManager, IContext context, PhysicsRuntimeBodyStore bodyStore)
    {
        var primaryCamera = new PrimaryCameraSystem(context);

        var physicsWorld = new World(new Vector2(0, -9.8f));
        var contactListener = new SceneContactListener();
        physicsWorld.SetContactListener(contactListener);

        ISystem[] systems =
        [
            new PhysicsSimulationSystem(physicsWorld, context, bodyStore),
            new ScriptUpdateSystem(scriptEngine),
            new AudioSystem(audioEngine, effectFactory, context),
            primaryCamera,
            new SpriteRenderingSystem(graphics2D, textureFactory, context, primaryCamera),
            new SubTextureRenderingSystem(graphics2D, textureFactory, context, primaryCamera),
            new LightingSystem(graphics3D, primaryCamera, context),
            new ModelRenderingSystem(graphics3D, context, primaryCamera),
            new PhysicsDebugRenderSystem(graphics2D, context, debugSettings, primaryCamera, bodyStore)
        ];

        foreach (var system in systems)
        {
            systemManager.RegisterSystem(system);
            Logger.Debug("Registered per-scene system: {SystemType}", system.GetType().Name);
        }
    }
}
