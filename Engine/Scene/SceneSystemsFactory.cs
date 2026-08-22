using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
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
    DebugSettings debugSettings,
    IScriptEngine scriptEngine,
    IAudio audio,
    AudioPlaybackService playbackService,
    IPhysicsWorldFactory physicsWorldFactory) : ISceneSystemsFactory
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemsFactory>();
    private static readonly Vector2 DefaultGravity2D = new(0, -9.8f);

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

        var physicsWorld = physicsWorldFactory.Create(DefaultGravity2D);
        physicsWorld.SetContactListener(contactListener);
        physicsQueries = physicsWorld;
        systems = Create2DSystems(physicsWorld, context, bodyStore, primaryCamera);

        var audioSystem = new AudioSystem(audio, context, playbackService);
        playbackService.Bind(audioSystem);

        ISystem[] shared =
        [
            new ScriptUpdateSystem(context, scriptEngine, scriptStore),
            audioSystem,
            primaryCamera,
            new SceneRenderSystem(graphics2D, graphics3D, textureFactory, context, primaryCamera)
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
}