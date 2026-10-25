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
        container.RegisterDelegate<Context>(_ => new Context());
        var registered = GameAssemblyContainerRegistration.TryRegisterContainer(
            container,
            typeof(StubGameSystemA).Assembly);

        registered.ShouldBeTrue();

        var systems = container.ResolveMany<IGameSystem>().ToArray();
        systems.Length.ShouldBe(3);
        systems.ShouldContain(s => s is StubGameSystemA);
        systems.ShouldContain(s => s is StubGameSystemB);
        systems.ShouldContain(s => s is NeedsContextSystemMarked);
    }

    [Fact]
    public void FuncOfGameSystems_ResolvesWithoutRegisterDelegate()
    {
        var container = new Container();
        container.RegisterDelegate<Context>(_ => new Context());
        container.Register<NeedsGameSystems>(Reuse.Singleton);
        container.ValidateAndThrow();
        container.Resolve<NeedsGameSystems>().Get().ShouldBeEmpty();

        GameAssemblyContainerRegistration.TryRegisterContainer(
            container,
            typeof(StubGameSystemA).Assembly);
        container.Resolve<Func<IEnumerable<IGameSystem>>>()().Count().ShouldBe(3);
    }

    [Fact]
    public void CapturedFunc_SeesGameSystemsRegisteredAfterConsumerResolved()
    {
        var container = new Container();
        container.RegisterDelegate<Context>(_ => new Context());
        GameAssemblyContainerRegistration.RegisterGameSystemsResolver(container);
        container.Register<NeedsGameSystems>(Reuse.Singleton);

        var needs = container.Resolve<NeedsGameSystems>();
        needs.Get().ShouldBeEmpty();

        GameAssemblyContainerRegistration.TryRegisterContainer(
            container,
            typeof(StubGameSystemA).Assembly);

        needs.Get().Count().ShouldBe(3);
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

    [Fact]
    public void TryRegisterContainer_ForcesTransientGameSystems_SoRequestLoadGetsFreshContext()
    {
        var container = new Container();
        var current = new Context();
        container.RegisterDelegate<Context>(_ => current);
        GameAssemblyContainerRegistration.TryRegisterContainer(
            container,
            typeof(NeedsContextSystemMarked).Assembly);

        var first = container.ResolveMany<IGameSystem>().OfType<NeedsContextSystemMarked>().Single();
        first.Context.ShouldBe(current);

        current = new Context();
        var second = container.ResolveMany<IGameSystem>().OfType<NeedsContextSystemMarked>().Single();
        second.Context.ShouldBe(current);
        ReferenceEquals(first, second).ShouldBeFalse();
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

    [Register(typeof(IGameSystem))]
    private sealed class NeedsContextSystemMarked(Context context) : IGameSystem
    {
        public int Priority => 3;
        public void OnUpdate(TimeSpan deltaTime) { }
        public Context Context => context;
    }
}
