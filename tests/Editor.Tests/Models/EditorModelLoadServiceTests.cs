using Editor.Features.Models;
using Engine.Renderer.Models;
using Engine.Renderer.Textures;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Models;

public class EditorModelLoadServiceTests
{
    [Fact]
    public void Request_WhenModelAlreadyCached_CompletesWithoutDiskWork()
    {
        var models = Substitute.For<IModelFactory>();
        var textures = Substitute.For<ITextureFactory>();
        var service = new EditorModelLoadService(models, textures);
        var meter = new EditorLoadWorkMeter();
        meter.Expect(1);

        // Path does not exist: reading it would fail, so completing proves the cache hit.
        var path = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".glb");
        models.IsCached(path).Returns(true);
        var model = (Model)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Model));
        models.Create(path).Returns(model);

        Model? ready = null;
        service.Request(path, m => ready = m, meter);

        ready.ShouldBeSameAs(model);
        service.IsBusy.ShouldBeFalse();
        meter.Done.ShouldBe(1);
        textures.DidNotReceiveWithAnyArgs().Decode(default!, default);
    }
}
