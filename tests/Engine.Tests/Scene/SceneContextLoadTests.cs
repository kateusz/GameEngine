using ECS;
using ECS.Systems;
using Engine.Core.Window;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using NSubstitute;
using Scripting;
using Shouldly;

namespace Engine.Tests.Scene;

public class SceneContextLoadTests
{
    private sealed class MarkerSystem : IGameSystem
    {
        public int Priority => 0;
        public void OnUpdate(TimeSpan deltaTime) { }
    }

    [Fact]
    public void RequestLoad_does_not_swap_until_Flush_and_stays_in_Play()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ge-ctx-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "next.scene");
        File.WriteAllText(path, """{"Scene":"next","Entities":[]}""");

        try
        {
            var factory = new SceneFactory(StubSystemsFactory(), Substitute.For<IPointerSurface>());
            var serializer = new SceneSerializer(new ComponentSerializerRegistry(), new SerializerOptions());
            var ctx = new SceneContext(factory, serializer, () => [new MarkerSystem()]);

            var initial = factory.Create("initial");
            ctx.SetScene(initial);
            RuntimeSceneStarter.Start(initial, ctx, []);

            ctx.RequestLoad(path);
            ReferenceEquals(ctx.ActiveScene, initial).ShouldBeTrue();

            ctx.FlushPendingLoad();

            ctx.State.ShouldBe(SceneState.Play);
            ctx.ActiveScene.ShouldNotBeNull();
            ReferenceEquals(ctx.ActiveScene, initial).ShouldBeFalse();
            ctx.ActiveScene!.Name.ShouldBe("next");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Flush_without_RequestLoad_is_noop()
    {
        var factory = new SceneFactory(StubSystemsFactory(), Substitute.For<IPointerSurface>());
        var serializer = new SceneSerializer(new ComponentSerializerRegistry(), new SerializerOptions());
        var ctx = new SceneContext(factory, serializer, () => []);
        var scene = factory.Create("keep");
        ctx.SetScene(scene);
        ctx.SetState(SceneState.Play);

        ctx.FlushPendingLoad();

        ReferenceEquals(ctx.ActiveScene, scene).ShouldBeTrue();
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
