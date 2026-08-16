using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene.Systems;
using Engine.Scripting;
using Scripting;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneSystemsFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IModelFactory modelFactory,
    DebugSettings debugSettings,
    IScriptEngine scriptEngine,
    IAudio audio,
    AudioPlaybackService playbackService,
    IPhysicsWorld2DFactory physicsWorld2DFactory) : ISceneSystemsFactory
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemsFactory>();
    private static readonly Vector2 DefaultGravity2D = new(0, -9.8f);
    private static readonly Vector3 DefaultGravity3D = new(0, -9.8f, 0);

    public IPhysicsQueries PopulateSystemManager(
        ISystemManager systemManager,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PhysicsContactQueue contactQueue,
        ScriptRuntimeStore scriptStore,
        SceneDimension dimension = SceneDimension.TwoD)
    {
        var primaryCamera = new PrimaryCameraSystem(context);
        var contactListener = new SceneContactListener(contactQueue, scriptStore);

        IPhysicsQueries physicsQueries;
        ISystem[] systems;
        if (dimension == SceneDimension.ThreeD)
        {
            var physicsWorld = physicsWorld2DFactory.Create3D(DefaultGravity3D);
            physicsWorld.SetContactListener(contactListener);
            physicsQueries = new PhysicsQueries3DAdapter(physicsWorld);
            systems = Create3DSystems(physicsWorld, context, primaryCamera);
        }
        else
        {
            var physicsWorld = physicsWorld2DFactory.Create(DefaultGravity2D);
            physicsWorld.SetContactListener(contactListener);
            physicsQueries = physicsWorld;
            systems = Create2DSystems(physicsWorld, context, bodyStore, primaryCamera);
        }

        var audioSystem = new AudioSystem(audio, context, playbackService);
        playbackService.Bind(audioSystem);

        ISystem[] shared =
        [
            new ScriptUpdateSystem(context, scriptEngine, scriptStore),
            audioSystem,
            new SkeletalAnimationSystem(context, modelFactory),
            primaryCamera,
            new SceneRenderSystem(graphics2D, graphics3D, textureFactory, modelFactory, context, primaryCamera)
        ];

        foreach (var system in systems.Concat(shared))
        {
            systemManager.RegisterSystem(system);
            Logger.Debug("Registered per-scene system: {SystemType}", system.GetType().Name);
        }

        return physicsQueries;
    }

    private ISystem[] Create2DSystems(
        IPhysicsWorld2D physicsWorld,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PrimaryCameraSystem primaryCamera) =>
    [
        new PhysicsSimulationSystem(physicsWorld, context, bodyStore),
        new PhysicsDebugRenderSystem(graphics2D, context, debugSettings, bodyStore, primaryCamera)
    ];

    private ISystem[] Create3DSystems(
        IPhysicsWorld3D physicsWorld,
        IContext context,
        PrimaryCameraSystem primaryCamera)
    {
        var bodyStore3D = new PhysicsRuntimeBodyStore3D();
        return
        [
            new PhysicsSimulationSystem3D(physicsWorld, context, bodyStore3D),
            new PhysicsDebugRenderSystem3D(graphics3D, context, debugSettings, bodyStore3D, primaryCamera)
        ];
    }
}
