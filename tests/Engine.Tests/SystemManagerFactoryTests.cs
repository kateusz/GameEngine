using ECS;
using ECS.Systems;
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
        var context = new Context();
        var builder = new SystemManagerFactory(_mockSystemsFactory);

        var build = builder.Create(context);

        _mockSystemsFactory.Received(1).PopulateSystemManager(build.SystemManager, context, build.BodyStore, build.ContactQueue);
        build.BodyStore.ShouldNotBeNull();
        build.ContactQueue.ShouldNotBeNull();
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
