using ECS;
using ECS.Systems;
using Engine.Core.Window;
using Engine.Physics;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using Scripting;
using Shouldly;

namespace Engine.Tests.Scene;

public class SceneFactoryTests
{
    [Fact]
    public void Create_ThreeD_PassesDimensionAndSetsSceneDimension()
    {
        var systemManagerFactory = Substitute.For<ISystemManagerFactory>();
        systemManagerFactory.Create(Arg.Any<IContext>(), SceneDimension.ThreeD).Returns(_ => new SceneBuildResult(
            Substitute.For<ISystemManager>(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            new ScriptRuntimeStore(),
            Substitute.For<IPhysicsQueries>()));

        var scene = new SceneFactory(systemManagerFactory, Substitute.For<IPointerSurface>())
            .Create("test", "test", SceneDimension.ThreeD);

        scene.Dimension.ShouldBe(SceneDimension.ThreeD);
        systemManagerFactory.Received(1).Create(Arg.Any<IContext>(), SceneDimension.ThreeD);
    }

    [Fact]
    public void Create_Default_IsTwoD()
    {
        var systemManagerFactory = Substitute.For<ISystemManagerFactory>();
        systemManagerFactory.Create(Arg.Any<IContext>(), Arg.Any<SceneDimension>()).Returns(_ => new SceneBuildResult(
            Substitute.For<ISystemManager>(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            new ScriptRuntimeStore(),
            Substitute.For<IPhysicsQueries>()));

        var scene = new SceneFactory(systemManagerFactory, Substitute.For<IPointerSurface>())
            .Create("test", "test");

        scene.Dimension.ShouldBe(SceneDimension.TwoD);
        systemManagerFactory.Received(1).Create(Arg.Any<IContext>(), SceneDimension.TwoD);
    }
}
