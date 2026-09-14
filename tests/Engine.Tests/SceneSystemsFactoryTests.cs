using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using Shouldly;

namespace Engine.Tests;

public class SceneSystemsFactoryTests
{
    [Fact]
    public void Populate_Registers2DStepperAndDebug()
    {
        var registered = Populate();

        registered.ShouldContain(s => s is PhysicsSimulationSystem);
        registered.ShouldContain(s => s is PhysicsDebugRenderSystem);
    }

    private static IReadOnlyList<ISystem> Populate()
    {
        var worldFactory = Substitute.For<IPhysicsWorldFactory>();
        worldFactory.Create(Arg.Any<Vector2>()).Returns(Substitute.For<IPhysicsWorld2D>());

        var factory = new SceneSystemsFactory(
            Substitute.For<IGraphics2D>(),
            Substitute.For<IGraphics3D>(),
            Substitute.For<ITextureFactory>(),
            new DebugSettings(),
            Substitute.For<IAudio>(),
            new AudioPlaybackService(),
            worldFactory,
            Substitute.For<IModelFactory>());

        var systemManager = new SystemManager();
        factory.PopulateSystemManager(
            systemManager,
            new Context(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue());

        return systemManager.Systems;
    }
}
