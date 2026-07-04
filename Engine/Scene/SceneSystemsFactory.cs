using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
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
    IAudio audio,
    IPhysicsWorld2DFactory physicsWorld2DFactory) : ISceneSystemsFactory
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemsFactory>();
    private static readonly Vector2 DefaultGravity = new(0, -9.8f);

    public void PopulateSystemManager(ISystemManager systemManager, IContext context, PhysicsRuntimeBodyStore bodyStore)
    {
        var primaryCamera = new PrimaryCameraSystem(context);

        var physicsWorld = physicsWorld2DFactory.Create(DefaultGravity);
        physicsWorld.SetContactListener(new SceneContactListener());

        ISystem[] systems =
        [
            new PhysicsSimulationSystem(physicsWorld, context, bodyStore),
            new ScriptUpdateSystem(scriptEngine),
            new AudioSystem(audio, context),
            primaryCamera,
            new SpriteRenderingSystem(graphics2D, textureFactory, context, primaryCamera),
            new SubTextureRenderingSystem(graphics2D, textureFactory, context, primaryCamera),
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
