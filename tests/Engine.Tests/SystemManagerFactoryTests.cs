using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using Shouldly;

namespace Engine.Tests;

public class SystemManagerFactoryTests
{
    private readonly ISceneSystemsFactory _mockSystemsFactory = Substitute.For<ISceneSystemsFactory>();

    [Fact]
    public void Create_ShouldPopulateSystemsForContext()
    {
        _mockSystemsFactory.PopulateSystemManager(Arg.Any<ISystemManager>(), Arg.Any<IContext>(), Arg.Any<PhysicsRuntimeBodyStore>(), Arg.Any<PhysicsContactQueue>(), Arg.Any<ScriptRuntimeStore>())
            .Returns(Substitute.For<IPhysicsWorld2D>());

        var context = new Context();
        var builder = new SystemManagerFactory(_mockSystemsFactory);

        var build = builder.Create(context);

        _mockSystemsFactory.Received(1).PopulateSystemManager(build.SystemManager, context, build.BodyStore, build.ContactQueue, build.ScriptStore);
        build.BodyStore.ShouldNotBeNull();
        build.ContactQueue.ShouldNotBeNull();
        build.ScriptStore.ShouldNotBeNull();
    }

    [Fact]
    public void Create_ShouldReturnNewSystemManagerPerCall()
    {
        _mockSystemsFactory.PopulateSystemManager(Arg.Any<ISystemManager>(), Arg.Any<IContext>(), Arg.Any<PhysicsRuntimeBodyStore>(), Arg.Any<PhysicsContactQueue>(), Arg.Any<ScriptRuntimeStore>())
            .Returns(Substitute.For<IPhysicsWorld2D>());

        var builder = new SystemManagerFactory(_mockSystemsFactory);

        var first = builder.Create(new Context()).SystemManager;
        var second = builder.Create(new Context()).SystemManager;

        first.ShouldNotBeSameAs(second);
    }
}
