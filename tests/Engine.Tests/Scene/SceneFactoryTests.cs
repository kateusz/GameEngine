using ECS;
using ECS.Systems;
using Engine.Core.Window;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using Scripting;
using Shouldly;

namespace Engine.Tests.Scene;

public class SceneFactoryTests
{
    [Fact]
    public void Create_ThreeD_SetsSceneDimensionAndPopulatesSystems()
    {
        var systemsFactory = StubSystemsFactory();
        var scene = new SceneFactory(systemsFactory, Substitute.For<IPointerSurface>())
            .Create("test", SceneDimension.ThreeD);

        scene.Dimension.ShouldBe(SceneDimension.ThreeD);
        systemsFactory.Received(1).PopulateSystemManager(
            Arg.Any<SystemManager>(),
            Arg.Any<Context>(),
            Arg.Any<PhysicsRuntimeBodyStore>(),
            Arg.Any<PhysicsContactQueue>());
    }

    [Fact]
    public void Create_Default_IsTwoDAndPopulatesSystems()
    {
        var systemsFactory = StubSystemsFactory();
        var scene = new SceneFactory(systemsFactory, Substitute.For<IPointerSurface>())
            .Create("test");

        scene.Dimension.ShouldBe(SceneDimension.TwoD);
        systemsFactory.Received(1).PopulateSystemManager(
            Arg.Any<SystemManager>(),
            Arg.Any<Context>(),
            Arg.Any<PhysicsRuntimeBodyStore>(),
            Arg.Any<PhysicsContactQueue>());
    }

    private static ISceneSystemsFactory StubSystemsFactory()
    {
        var systemsFactory = Substitute.For<ISceneSystemsFactory>();
        systemsFactory.PopulateSystemManager(
                Arg.Any<SystemManager>(),
                Arg.Any<Context>(),
                Arg.Any<PhysicsRuntimeBodyStore>(),
                Arg.Any<PhysicsContactQueue>())
            .Returns(Substitute.For<IPhysicsQueries>());
        return systemsFactory;
    }
}
