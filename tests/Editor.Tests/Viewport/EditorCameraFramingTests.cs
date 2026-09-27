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
}
