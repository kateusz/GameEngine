using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using Shouldly;

namespace Engine.Tests;

public class SystemManagerFactoryTests
{
    private readonly ISceneSystemRegistry _mockSystemRegistry = Substitute.For<ISceneSystemRegistry>();
    private readonly IContext _context = new Context();
    private readonly PhysicsRuntimeBodyStore _bodyStore = new();
    private readonly IPhysicsSimulationSystemFactory _physicsFactory;

    public SystemManagerFactoryTests()
    {
        _physicsFactory = new PhysicsSimulationSystemFactory(_bodyStore, _context);
        _mockSystemRegistry.PopulateSystemManager(Arg.Any<ISystemManager>())
            .Returns([]);
    }

    [Fact]
    public void Build_ShouldPopulateSharedSystemsAndPhysics()
    {
        var builder = new SystemManagerFactory(_mockSystemRegistry, _physicsFactory);

        var systemManager = builder.Create();

        _mockSystemRegistry.Received(1).PopulateSystemManager(systemManager);
        systemManager.SystemCount.ShouldBe(1);
    }

    [Fact]
    public void Build_ShouldReturnNewSystemManagerPerCall()
    {
        var builder = new SystemManagerFactory(_mockSystemRegistry, _physicsFactory);

        var first = builder.Create();
        var second = builder.Create();

        first.ShouldNotBeSameAs(second);
    }
}
