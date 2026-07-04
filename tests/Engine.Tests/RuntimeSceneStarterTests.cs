using ECS.Systems;
using Engine.Scene;
using NSubstitute;
using Shouldly;

namespace Engine.Tests;

public class RuntimeSceneStarterTests
{
    private sealed class TestGameSystem : IGameSystem
    {
        public int Priority => 0;
        public void OnInit() { }
        public void OnUpdate(TimeSpan deltaTime) { }
        public void OnShutdown() { }
    }

    [Fact]
    public void Start_registers_systems_sets_play_state_and_starts()
    {
        var scene = Substitute.For<IScene>();
        var sceneContext = new SceneContext();
        var gameSystem = new TestGameSystem();

        RuntimeSceneStarter.Start(scene, sceneContext, [gameSystem]);

        scene.Received(1).RegisterRuntimeSystem(gameSystem);
        scene.Received(1).OnRuntimeStart();
        sceneContext.State.ShouldBe(SceneState.Play);
    }

    [Fact]
    public void Start_swallows_InvalidOperationException_on_reentry()
    {
        var scene = Substitute.For<IScene>();
        var sceneContext = new SceneContext();
        var gameSystem = new TestGameSystem();

        scene.When(s => s.RegisterRuntimeSystem(gameSystem))
            .Do(_ => throw new InvalidOperationException("already registered"));

        Should.NotThrow(() => RuntimeSceneStarter.Start(scene, sceneContext, [gameSystem]));

        scene.Received(1).OnRuntimeStart();
        sceneContext.State.ShouldBe(SceneState.Play);
    }
}
