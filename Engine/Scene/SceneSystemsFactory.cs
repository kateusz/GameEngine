using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Platform.Box2D;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene.Systems;
using Scripting;

namespace Engine.Scene;

internal sealed class SceneSystemsFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    DebugSettings debugSettings,
    IAudio audio,
    AudioPlaybackService playbackService,
    IModelFactory modelFactory) : ISceneSystemsFactory
{
    private static readonly Vector2 DefaultGravity2D = new(0, -9.8f);

    public IPhysicsQueries PopulateSystemManager(
        SystemManager systemManager,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PhysicsContactQueue contactQueue)
    {
        var physicsWorld = new Box2DPhysicsWorld2D(DefaultGravity2D, contactQueue);

        var audioSystem = new AudioSystem(audio, context, playbackService);
        playbackService.Bind(audioSystem);

        ISystem[] systems =
        [
            new PhysicsSimulationSystem(physicsWorld, context, bodyStore),
            new PhysicsDebugRenderSystem(graphics2D, context, debugSettings, bodyStore),
            audioSystem,
            new SceneRenderSystem(graphics2D, graphics3D, textureFactory, context, modelFactory)
        ];

        foreach (var system in systems)
            systemManager.RegisterSystem(system);

        return physicsWorld;
    }
}
