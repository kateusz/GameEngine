using System.IO;
using ECS;
using ECS.Systems;
using Engine.Audio;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using Engine.Scripting;
using NSubstitute;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests;

public class SceneHierarchyTests : IDisposable
{
    private readonly IGraphics2D _g2 = Substitute.For<IGraphics2D>();
    private readonly IGraphics3D _g3 = Substitute.For<IGraphics3D>();
    private readonly ISceneSystemRegistry _reg = Substitute.For<ISceneSystemRegistry>();
    private readonly IContext _ctx = new Context();

    public SceneHierarchyTests()
    {
        _reg.PopulateSystemManager(Arg.Any<ISystemManager>())
            .Returns(new List<ISystem>());
    }

    public void Dispose() => _ctx.Clear();

    private EngineScene NewScene() =>
        new("p", "p", _reg, _g2, _g3, _ctx, new Engine.Core.DebugSettings());

    private static (Entity e, TransformComponent t) WithTransform(Entity e)
    {
        var t = e.AddComponent<TransformComponent>();
        return (e, t);
    }

    [Fact]
    public void GetRootEntities_AllEntitiesAreRootsByDefault()
    {
        using var scene = NewScene();
        var a = WithTransform(scene.CreateEntity("A")).e;
        var b = WithTransform(scene.CreateEntity("B")).e;

        scene.GetRootEntities().ShouldBe(new[] { a, b }, ignoreOrder: true);
    }

    [Fact]
    public void SetParent_RegistersChildOnParent_AndChildKnowsParent()
    {
        using var scene = NewScene();
        var (parent, parentT) = WithTransform(scene.CreateEntity("P"));
        var (child, childT) = WithTransform(scene.CreateEntity("C"));

        scene.SetParent(child, parent);

        childT.ParentId.ShouldBe(parent.Id);
        parentT.ChildIds.ShouldContain(child.Id);
        scene.GetChildren(parent).ShouldBe(new[] { child });
        scene.GetRootEntities().ShouldNotContain(child);
    }

    [Fact]
    public void SetParent_Null_UnparentsChildToRoot()
    {
        using var scene = NewScene();
        var (parent, parentT) = WithTransform(scene.CreateEntity("P"));
        var (child, childT) = WithTransform(scene.CreateEntity("C"));
        scene.SetParent(child, parent);

        scene.SetParent(child, null);

        childT.ParentId.ShouldBeNull();
        parentT.ChildIds.ShouldBeEmpty();
        scene.GetRootEntities().ShouldContain(child);
    }

    [Fact]
    public void SetParent_MovingFromOldParent_RemovesFromOldChildren()
    {
        using var scene = NewScene();
        var (p1, p1T) = WithTransform(scene.CreateEntity("P1"));
        var (p2, p2T) = WithTransform(scene.CreateEntity("P2"));
        var (c, _) = WithTransform(scene.CreateEntity("C"));
        scene.SetParent(c, p1);

        scene.SetParent(c, p2);

        p1T.ChildIds.ShouldNotContain(c.Id);
        p2T.ChildIds.ShouldContain(c.Id);
    }

    [Fact]
    public void SetParent_ToSelf_Throws()
    {
        using var scene = NewScene();
        var (e, _) = WithTransform(scene.CreateEntity("E"));

        Should.Throw<InvalidOperationException>(() => scene.SetParent(e, e));
    }

    [Fact]
    public void SetParent_ToOwnDescendant_Throws()
    {
        using var scene = NewScene();
        var (a, _) = WithTransform(scene.CreateEntity("A"));
        var (b, _) = WithTransform(scene.CreateEntity("B"));
        var (c, _) = WithTransform(scene.CreateEntity("C"));
        scene.SetParent(b, a);
        scene.SetParent(c, b);

        Should.Throw<InvalidOperationException>(() => scene.SetParent(a, c));
    }

    [Fact]
    public void SetParent_RequiresChildToHaveTransform()
    {
        using var scene = NewScene();
        var child = scene.CreateEntity("C");          // no TransformComponent
        var (parent, _) = WithTransform(scene.CreateEntity("P"));

        Should.Throw<InvalidOperationException>(() => scene.SetParent(child, parent));
    }

    [Fact]
    public void MutatingParentTranslation_MarksDescendantsWorldDirty()
    {
        using var scene = NewScene();
        var (a, aT) = WithTransform(scene.CreateEntity("A"));
        var (b, bT) = WithTransform(scene.CreateEntity("B"));
        var (c, cT) = WithTransform(scene.CreateEntity("C"));
        scene.SetParent(b, a);
        scene.SetParent(c, b);

        TransformComponent? Resolve(int id) =>
            scene.Entities.FirstOrDefault(e => e.Id == id)?.GetComponent<TransformComponent>();

        _ = aT.GetWorldTransform(Resolve);
        _ = bT.GetWorldTransform(Resolve);
        _ = cT.GetWorldTransform(Resolve);

        aT.Translation = new System.Numerics.Vector3(5, 0, 0);

        aT.IsWorldDirty.ShouldBeTrue();
        bT.IsWorldDirty.ShouldBeTrue();
        cT.IsWorldDirty.ShouldBeTrue();
    }

    [Fact]
    public void DestroyEntity_CascadesToDescendants()
    {
        using var scene = NewScene();
        var (a, _) = WithTransform(scene.CreateEntity("A"));
        var (b, _) = WithTransform(scene.CreateEntity("B"));
        var (c, _) = WithTransform(scene.CreateEntity("C"));
        scene.SetParent(b, a);
        scene.SetParent(c, b);

        scene.DestroyEntity(a);

        scene.Entities.ShouldBeEmpty();
    }

    [Fact]
    public void DestroyEntity_RemovesSelfFromParentChildren()
    {
        using var scene = NewScene();
        var (parent, parentT) = WithTransform(scene.CreateEntity("P"));
        var (child, _) = WithTransform(scene.CreateEntity("C"));
        scene.SetParent(child, parent);

        scene.DestroyEntity(child);

        parentT.ChildIds.ShouldNotContain(child.Id);
        scene.Entities.ShouldContain(parent);
    }

    [Fact]
    public void DuplicateEntity_DuplicatesEntireSubtree()
    {
        using var scene = NewScene();
        var (a, _) = WithTransform(scene.CreateEntity("A"));
        var (b, _) = WithTransform(scene.CreateEntity("B"));
        var (c, _) = WithTransform(scene.CreateEntity("C"));
        scene.SetParent(b, a);
        scene.SetParent(c, b);

        var dup = scene.DuplicateEntity(a);

        dup.Id.ShouldNotBe(a.Id);
        dup.Name.ShouldBe(a.Name);

        var dupT = dup.GetComponent<TransformComponent>();
        dupT.ParentId.ShouldBeNull();
        dupT.ChildIds.Count.ShouldBe(1);

        var dupB = scene.Entities.First(e => e.Id == dupT.ChildIds[0]);
        var dupBT = dupB.GetComponent<TransformComponent>();
        dupBT.ChildIds.Count.ShouldBe(1);
        dupBT.ParentId.ShouldBe(dup.Id);
    }

    [Fact]
    public void Serialization_RoundTripsParentId_AndRebuildsChildIds()
    {
        var serializerOptions = new SerializerOptions();
        var componentDeserializer = new ComponentDeserializer(
            Substitute.For<IAudioEngine>(),
            Substitute.For<ITextureFactory>(),
            Substitute.For<IMeshFactory>(),
            Substitute.For<IScriptEngine>(),
            serializerOptions);
        var sceneSerializer = new SceneSerializer(componentDeserializer, serializerOptions);

        var path = Path.Combine(Path.GetTempPath(), $"hierarchy-{Guid.NewGuid():N}.scene.json");
        try
        {
            using (var scene = NewScene())
            {
                var (parent, _) = WithTransform(scene.CreateEntity("P"));
                var (child, _) = WithTransform(scene.CreateEntity("C"));
                scene.SetParent(child, parent);
                sceneSerializer.Serialize(scene, path);
            }

            using var loaded = NewScene();
            sceneSerializer.Deserialize(loaded, path);

            var loadedChild = loaded.Entities.First(e => e.Name == "C");
            var loadedParent = loaded.Entities.First(e => e.Name == "P");
            loadedChild.GetComponent<TransformComponent>().ParentId.ShouldBe(loadedParent.Id);
            loadedParent.GetComponent<TransformComponent>().ChildIds.ShouldContain(loadedChild.Id);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
