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
    public void Create_ThreeD_SetsSceneDimension()
    {
        var scene = CreateFactory().Create("test", SceneDimension.ThreeD);

        scene.Dimension.ShouldBe(SceneDimension.ThreeD);
    }

    [Fact]
    public void Create_Default_IsTwoD()
    {
        var scene = CreateFactory().Create("test");

        scene.Dimension.ShouldBe(SceneDimension.TwoD);
    }

    private static SceneFactory CreateFactory()
    {
        var systemsFactory = Substitute.For<ISceneSystemsFactory>();
        systemsFactory.PopulateSystemManager(
                Arg.Any<ISystemManager>(),
                Arg.Any<IContext>(),
                Arg.Any<PhysicsRuntimeBodyStore>(),
                Arg.Any<PhysicsContactQueue>())
            .Returns(Substitute.For<IPhysicsQueries>());

        return new SceneFactory(systemsFactory, Substitute.For<IPointerSurface>());
    }
}
