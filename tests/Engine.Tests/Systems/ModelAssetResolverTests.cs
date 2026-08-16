using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Systems;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class ModelAssetResolverTests : IDisposable
{
    private readonly string _assetsRoot;

    public ModelAssetResolverTests()
    {
        _assetsRoot = Path.Combine(Path.GetTempPath(), "GameEngine-ModelResolver", Guid.NewGuid().ToString("N"), "assets");
        Directory.CreateDirectory(_assetsRoot);
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assetsRoot);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            var root = Directory.GetParent(_assetsRoot)?.FullName;
            if (root is not null && Directory.Exists(root))
                Directory.Delete(root, true);
        }
        catch
        {
            // ponytail: temp cleanup best-effort
        }
    }

    [Fact]
    public void SyncAll_ResolvesPathOnceUntilChanged()
    {
        var model = CreateStubModel();
        var factory = Substitute.For<IModelFactory>();
        factory.Create(Arg.Any<string>()).Returns(model);

        var context = new Context();
        var entity = Entity.Create(1, "crate");
        entity.AddComponent(new ModelRendererComponent { ModelPath = "models/crate.mesh" });
        context.Register(entity);

        ModelAssetResolver.SyncAll(context, factory);
        ModelAssetResolver.SyncAll(context, factory);

        factory.Received(1).Create(Arg.Any<string>());
        entity.GetComponent<ResolvedModelComponent>().Model.ShouldBeSameAs(model);
        entity.GetComponent<ResolvedModelComponent>().SourcePath.ShouldBe("models/crate.mesh");
    }

    [Fact]
    public void SyncAll_ReResolvesWhenPathChanges()
    {
        var first = CreateStubModel();
        var second = CreateStubModel();
        var factory = Substitute.For<IModelFactory>();
        factory.Create(Arg.Any<string>()).Returns(first, second);

        var context = new Context();
        var entity = Entity.Create(1, "crate");
        var renderer = new ModelRendererComponent { ModelPath = "models/a.mesh" };
        entity.AddComponent(renderer);
        context.Register(entity);

        ModelAssetResolver.SyncAll(context, factory);
        renderer.ModelPath = "models/b.mesh";
        ModelAssetResolver.SyncAll(context, factory);

        factory.Received(2).Create(Arg.Any<string>());
        entity.GetComponent<ResolvedModelComponent>().Model.ShouldBeSameAs(second);
    }

    [Fact]
    public void SyncAll_ClearsResolvedComponentForEmptyPath()
    {
        var factory = Substitute.For<IModelFactory>();
        var context = new Context();
        var entity = Entity.Create(1, "cube");
        entity.AddComponent(new ModelRendererComponent());
        entity.AddComponent(new ResolvedModelComponent { SourcePath = "stale", Model = CreateStubModel() });
        context.Register(entity);

        ModelAssetResolver.SyncAll(context, factory);

        entity.HasComponent<ResolvedModelComponent>().ShouldBeFalse();
        factory.DidNotReceive().Create(Arg.Any<string>());
    }

    private static Model CreateStubModel() =>
        new([new ModelSubmesh(new Mesh("m"), new MeshMaterial())]);
}
