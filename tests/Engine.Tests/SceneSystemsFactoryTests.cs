using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Systems;
using Engine.Scripting;
using NSubstitute;
using Shouldly;

namespace Engine.Tests;

public class SceneSystemsFactoryTests
{
    [Fact]
    public void Populate_TwoD_Registers2DStepperAndDebug()
    {
        var registered = Populate(SceneDimension.TwoD);

        registered.ShouldContain(s => s is PhysicsSimulationSystem);
        registered.ShouldContain(s => s is PhysicsDebugRenderSystem);
    }

    [Fact]
    public void Populate_ThreeD_StillRegisters2DPhysics()
    {
        var registered = Populate(SceneDimension.ThreeD);

        registered.ShouldContain(s => s is PhysicsSimulationSystem);
        registered.ShouldContain(s => s is PhysicsDebugRenderSystem);
    }

    private static List<ISystem> Populate(SceneDimension dimension)
    {
        var worldFactory = Substitute.For<IPhysicsWorldFactory>();
        worldFactory.Create(Arg.Any<Vector2>()).Returns(Substitute.For<IPhysicsWorld2D>());

        var factory = new SceneSystemsFactory(
            Substitute.For<IGraphics2D>(),
            Substitute.For<IGraphics3D>(),
            Substitute.For<ITextureFactory>(),
            new DebugSettings(),
            Substitute.For<IScriptEngine>(),
            Substitute.For<IAudio>(),
            new AudioPlaybackService(),
            worldFactory);

        var registered = new List<ISystem>();
        var systemManager = Substitute.For<ISystemManager>();
        systemManager.When(x => x.RegisterSystem(Arg.Any<ISystem>(), Arg.Any<bool>()))
            .Do(ci => registered.Add(ci.ArgAt<ISystem>(0)));

        factory.PopulateSystemManager(
            systemManager,
            new Context(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            new ScriptRuntimeStore(),
            dimension);

        return registered;
    }
}
