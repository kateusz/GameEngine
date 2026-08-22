using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class SystemManagerFactoryTests
{
    private readonly ISceneSystemsFactory _mockSystemsFactory = Substitute.For<ISceneSystemsFactory>();

    public SystemManagerFactoryTests()
    {
        _mockSystemsFactory.PopulateSystemManager(
                Arg.Any<ISystemManager>(),
                Arg.Any<IContext>(),
                Arg.Any<PhysicsRuntimeBodyStore>(),
                Arg.Any<PhysicsContactQueue>(),
                Arg.Any<ScriptRuntimeStore>(),
                Arg.Any<SceneDimension>())
            .Returns(Substitute.For<IPhysicsQueries>());
    }

    [Fact]
    public void Create_ShouldPopulateSystemsForContext()
    {
        var context = new Context();
        var builder = new SystemManagerFactory(_mockSystemsFactory);

        var build = builder.Create(context);

        _mockSystemsFactory.Received(1).PopulateSystemManager(
            build.SystemManager, context, build.BodyStore, build.ContactQueue, build.ScriptStore,
            SceneDimension.TwoD);
        build.BodyStore.ShouldNotBeNull();
        build.ContactQueue.ShouldNotBeNull();
        build.ScriptStore.ShouldNotBeNull();
    }

    [Fact]
    public void Create_ThreeD_StillUses2DPhysicsStore()
    {
        var context = new Context();
        var builder = new SystemManagerFactory(_mockSystemsFactory);

        var build = builder.Create(context, SceneDimension.ThreeD);

        build.BodyStore.ShouldNotBeNull();
        _mockSystemsFactory.Received(1).PopulateSystemManager(
            build.SystemManager, context, build.BodyStore, build.ContactQueue, build.ScriptStore,
            SceneDimension.ThreeD);
    }

    [Fact]
    public void Create_ShouldReturnNewSystemManagerPerCall()
    {
        var builder = new SystemManagerFactory(_mockSystemsFactory);

        var first = builder.Create(new Context()).SystemManager;
        var second = builder.Create(new Context()).SystemManager;

        first.ShouldNotBeSameAs(second);
    }
}
