using DryIoc;
using ECS.Systems;
using Engine.Scripting;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class GameAssemblyContainerRegistrationTests
{
    [Fact]
    public void TryRegisterContainer_RegistersMultipleGameSystems()
    {
        var container = new Container();
        var registered = GameAssemblyContainerRegistration.TryRegisterContainer(
            container,
            typeof(StubGameSystemA).Assembly);

        registered.ShouldBeTrue();

        var systems = container.ResolveMany<IGameSystem>().ToArray();
        systems.Length.ShouldBe(2);
        systems.ShouldContain(s => s is StubGameSystemA);
        systems.ShouldContain(s => s is StubGameSystemB);
    }

    [Register(typeof(IGameSystem))]
    private sealed class StubGameSystemA : IGameSystem
    {
        public int Priority => 1;
        public void OnInit() { }
        public void OnShutdown() { }
        public void OnUpdate(TimeSpan deltaTime) { }
    }

    [Register(typeof(IGameSystem))]
    private sealed class StubGameSystemB : IGameSystem
    {
        public int Priority => 2;
        public void OnInit() { }
        public void OnShutdown() { }
        public void OnUpdate(TimeSpan deltaTime) { }
    }
}
