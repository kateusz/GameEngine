using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer;
using Engine.Renderer.Skeletal;
using Engine.Renderer.Textures;
using Engine.Scene.Systems;
using Engine.Scripting;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneSystemsFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IModelFactory modelFactory,
    ISkeletonFactory skeletonFactory,
    IAnim3dFactory anim3dFactory,
    DebugSettings debugSettings,
    IScriptEngine scriptEngine,
    IAudio audio,
    AudioPlaybackService playbackService,
    IPhysicsWorld2DFactory physicsWorld2DFactory) : ISceneSystemsFactory
{
    private static readonly ILogger Logger = Log.ForContext<SceneSystemsFactory>();
    private static readonly Vector2 DefaultGravity = new(0, -9.8f);

    public IPhysicsWorld2D PopulateSystemManager(
        ISystemManager systemManager,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PhysicsContactQueue contactQueue,
        ScriptRuntimeStore scriptStore)
    {
        var primaryCamera = new PrimaryCameraSystem(context);

        var physicsWorld = physicsWorld2DFactory.Create(DefaultGravity);
        physicsWorld.SetContactListener(new SceneContactListener(contactQueue, scriptStore));

        var audioSystem = new AudioSystem(audio, context, playbackService);
        playbackService.Bind(audioSystem);

        ISystem[] systems =
        [
            new PhysicsSimulationSystem(physicsWorld, context, bodyStore),
            new ScriptUpdateSystem(context, scriptEngine, scriptStore),
            audioSystem,
            new SkeletalAnimationSystem(context, skeletonFactory, anim3dFactory),
            primaryCamera,
            new SceneRenderSystem(graphics2D, graphics3D, textureFactory, modelFactory, context, primaryCamera),
            new PhysicsDebugRenderSystem(graphics2D, context, debugSettings, bodyStore, primaryCamera)
        ];

        foreach (var system in systems)
        {
            systemManager.RegisterSystem(system);
            Logger.Debug("Registered per-scene system: {SystemType}", system.GetType().Name);
        }

        return physicsWorld;
    }
}
