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
        registered.ShouldNotContain(s => s is PhysicsSimulationSystem3D);
        registered.ShouldContain(s => s is PhysicsDebugRenderSystem);
        registered.ShouldNotContain(s => s is PhysicsDebugRenderSystem3D);
    }

    [Fact]
    public void Populate_ThreeD_Registers3DStepperAndDebug()
    {
        var registered = Populate(SceneDimension.ThreeD);

        registered.ShouldContain(s => s is PhysicsSimulationSystem3D);
        registered.ShouldNotContain(s => s is PhysicsSimulationSystem);
        registered.ShouldContain(s => s is PhysicsDebugRenderSystem3D);
        registered.ShouldNotContain(s => s is PhysicsDebugRenderSystem);
    }

    private static List<ISystem> Populate(SceneDimension dimension)
    {
        var worldFactory = Substitute.For<IPhysicsWorld2DFactory>();
        worldFactory.Create(Arg.Any<Vector2>()).Returns(Substitute.For<IPhysicsWorld2D>());
        worldFactory.Create3D(Arg.Any<Vector3>()).Returns(Substitute.For<IPhysicsWorld3D>());

        var factory = new SceneSystemsFactory(
            Substitute.For<IGraphics2D>(),
            Substitute.For<IGraphics3D>(),
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
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
