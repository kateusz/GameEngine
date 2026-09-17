using DryIoc;
using ECS;
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

    [Fact]
    public void FuncOfGameSystems_ResolvesWithoutRegisterDelegate()
    {
        var container = new Container();
        container.Register<NeedsGameSystems>(Reuse.Singleton);
        container.ValidateAndThrow();
        container.Resolve<NeedsGameSystems>().Get().ShouldBeEmpty();

        GameAssemblyContainerRegistration.TryRegisterContainer(
            container,
            typeof(StubGameSystemA).Assembly);
        container.Resolve<Func<IEnumerable<IGameSystem>>>()().Count().ShouldBe(2);
    }

    [Fact]
    public void CapturedFunc_SeesGameSystemsRegisteredAfterConsumerResolved()
    {
        var container = new Container();
        GameAssemblyContainerRegistration.RegisterGameSystemsResolver(container);
        container.Register<NeedsGameSystems>(Reuse.Singleton);

        var needs = container.Resolve<NeedsGameSystems>();
        needs.Get().ShouldBeEmpty();

        GameAssemblyContainerRegistration.TryRegisterContainer(
            container,
            typeof(StubGameSystemA).Assembly);

        needs.Get().Count().ShouldBe(2);
    }

    [Fact]
    public void CapturedFunc_ResolvesSingletonGameSystemThatDependsOnTransientContext()
    {
        var container = new Container();
        GameAssemblyContainerRegistration.RegisterGameSystemsResolver(container);
        var context = new Context();
        container.RegisterDelegate<Context>(_ => context);
        container.Register<IGameSystem, NeedsContextSystem>(Reuse.Singleton);
        container.Register<NeedsGameSystems>(Reuse.Singleton);

        var systems = container.Resolve<NeedsGameSystems>().Get().ToArray();
        systems.Length.ShouldBe(1);
        systems[0].ShouldBeOfType<NeedsContextSystem>();
    }

    private sealed class NeedsGameSystems(Func<IEnumerable<IGameSystem>> resolve)
    {
        public IEnumerable<IGameSystem> Get() => resolve();
    }

    [Register(typeof(IGameSystem))]
    private sealed class StubGameSystemA : IGameSystem
    {
        public int Priority => 1;
        public void OnUpdate(TimeSpan deltaTime) { }
    }

    [Register(typeof(IGameSystem))]
    private sealed class StubGameSystemB : IGameSystem
    {
        public int Priority => 2;
        public void OnUpdate(TimeSpan deltaTime) { }
    }

    private sealed class NeedsContextSystem(Context context) : IGameSystem
    {
        public int Priority => 1;
        public void OnUpdate(TimeSpan deltaTime) { }
        public Context Context => context;
    }
}
