using ECS;
using Editor.Features.History;
using Editor.Features.Scene;
using Engine.Project;
using Engine.Scene;
using Engine.Scene.Serializer;
using NSubstitute;

namespace Editor.Tests.Scene;

public class SceneManagerPlaySnapshotTests
{
    [Fact]
    public void Play_AfterStop_ReserializesSnapshotFromLiveScene()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ge-play-snap-{Guid.NewGuid():N}");
        var scriptsDir = Path.Combine(root, "scripts");
        Directory.CreateDirectory(scriptsDir);

        try
        {
            var sceneContext = Substitute.For<ISceneContext>();
            sceneContext.State.Returns(SceneState.Edit);
            var scene = Substitute.For<IScene>();
            scene.Entities.Returns(Array.Empty<Entity>());
            sceneContext.ActiveScene.Returns(scene);

            var projectContext = Substitute.For<IProjectContext>();
            projectContext.Root.Returns(root);
            projectContext.ScriptsDir.Returns(scriptsDir);

            var serializer = Substitute.For<ISceneSerializer>();
            var manager = SceneManagerTestFactory.Create(
                sceneContext, projectContext, Substitute.For<IEditorHistory>(), serializer);

            manager.Play();
            serializer.Received(1).Serialize(scene, Arg.Any<string>());

            sceneContext.State.Returns(SceneState.Play);
            manager.Stop();

            manager.Play();
            serializer.Received(2).Serialize(scene, Arg.Any<string>());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
