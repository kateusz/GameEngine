using System.Numerics;
using ECS.Systems;
using Editor.Features.Viewport;
using Engine.Core.Window;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using Scripting;
using Shouldly;

namespace Editor.Tests.Viewport;

public class EditorCameraFramingTests
{
    private static IScene CreateScene()
    {
        var systemsFactory = Substitute.For<ISceneSystemsFactory>();
        systemsFactory.PopulateSystemManager(
                Arg.Any<SystemManager>(),
                Arg.Any<ECS.Context>(),
                Arg.Any<PhysicsRuntimeBodyStore>(),
                Arg.Any<PhysicsContactQueue>())
            .Returns(Substitute.For<IPhysicsQueries>());

        return new SceneFactory(systemsFactory, Substitute.For<IPointerSurface>())
            .Create("test-scene");
    }

    [Fact]
    public void FocusOnEntity_UsesWorldTranslation_ForParentedEntity()
    {
        using var scene = CreateScene();
        var sceneContext = Substitute.For<ISceneContext>();
        sceneContext.ActiveScene.Returns(scene);

        var parent = scene.CreateEntity("parent");
        parent.AddComponent(new TransformComponent(new Vector3(10f, 0f, 5f), Vector3.Zero, Vector3.One));

        var child = scene.CreateEntity("child");
        child.AddComponent(new TransformComponent());
        scene.SetParent(child, parent);

        var camera = new EditorCamera();
        var framing = new EditorCameraFramingService(sceneContext);
        framing.SetCamera(camera);

        framing.FocusOnEntity(child);

        camera.FocalPoint.ShouldBe(new Vector3(10f, 0f, 5f));
    }

    /// <summary>
    /// Sponza-style: scaled root, intermediate group without Transform, child with large local translation.
    /// Focus must use composed world position, not local Translation and not identity.
    /// </summary>
    [Fact]
    public void FocusOnEntity_ScaledParent_UsesComposedWorldNotLocal()
    {
        using var scene = CreateScene();
        var sceneContext = Substitute.For<ISceneContext>();
        sceneContext.ActiveScene.Returns(scene);

        var root = scene.CreateEntity("sponza");
        root.AddComponent(new TransformComponent(Vector3.Zero, Vector3.Zero, new Vector3(0.05f)));

        var group = scene.CreateEntity("Plants");
        // no TransformComponent — real scenes have empty group nodes
        scene.SetParent(group, root);

        var child = scene.CreateEntity("Plant");
        child.AddComponent(new TransformComponent(
            new Vector3(495.27972f, 148.15741f, 198.49435f),
            Vector3.Zero,
            Vector3.One));
        scene.SetParent(child, group);

        var camera = new EditorCamera();
        var framing = new EditorCameraFramingService(sceneContext);
        framing.SetCamera(camera);

        framing.FocusOnEntity(child);

        // local would be ~495; identity would be 0; composed world is local * parentScale
        camera.FocalPoint.X.ShouldBe(24.763986f, 0.001f);
        camera.FocalPoint.Y.ShouldBe(7.4078705f, 0.001f);
        camera.FocalPoint.Z.ShouldBe(9.9247175f, 0.001f);
    }
}
