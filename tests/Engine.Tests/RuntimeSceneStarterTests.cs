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
        public void OnUpdate(TimeSpan deltaTime) { }
    }

    [Fact]
    public void Start_registers_systems_sets_play_state_and_starts()
    {
        var scene = Substitute.For<IScene>();
        var sceneContext = new SceneContext();
        var gameSystem = new TestGameSystem();

        RuntimeSceneStarter.Start(scene, sceneContext, [gameSystem]);

        scene.Received(1).OnRuntimeStart(Arg.Is<IEnumerable<ISystem>>(s => s.Single() == gameSystem));
        sceneContext.State.ShouldBe(SceneState.Play);
    }
}
