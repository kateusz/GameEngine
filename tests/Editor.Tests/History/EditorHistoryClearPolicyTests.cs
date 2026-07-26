using ECS.Systems;
using Editor.Features.History;
using Editor.Features.Scene;
using Editor.Features.Scripting;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.History;

public class EditorHistoryClearPolicyTests
{
    private readonly ISceneContext _sceneContext = Substitute.For<ISceneContext>();

    public EditorHistoryClearPolicyTests()
    {
        _sceneContext.State.Returns(SceneState.Edit);
    }

    private EditorHistory CreateHistory() => new(_sceneContext);

    private static IUndoCommand FakeCommand()
    {
        var command = Substitute.For<IUndoCommand>();
        command.Execute().Returns(true);
        return command;
    }

    [Fact]
    public void Clear_EmptiesBothStacks()
    {
        var history = CreateHistory();
        var command = FakeCommand();

        history.Execute(command);
        history.Undo();
        history.CanRedo.ShouldBeTrue();

        history.Clear();

        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void ExecuteUndoRedo_WhenNotEdit_AreNoOps()
    {
        var history = CreateHistory();
        var command = FakeCommand();

        history.Execute(command);
        history.CanUndo.ShouldBeTrue();

        _sceneContext.State.Returns(SceneState.Play);

        history.Execute(FakeCommand());
        history.Undo();
        history.Redo();

        command.Received(1).Execute();
        command.DidNotReceive().Undo();
        history.CanUndo.ShouldBeTrue();
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void Clear_WhenPlay_StillClearsStacks()
    {
        var history = CreateHistory();
        history.Execute(FakeCommand());

        _sceneContext.State.Returns(SceneState.Play);
        history.Clear();

        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void SceneManager_Play_WhenNoProject_DoesNotClearHistory()
    {
        var history = Substitute.For<IEditorHistory>();
        var projectContext = Substitute.For<IProjectContext>();
        projectContext.Root.Returns((string?)null);
        projectContext.ScriptsDir.Returns((string?)null);

        var manager = CreateSceneManager(history, projectContext);

        manager.Play();

        history.DidNotReceive().Clear();
    }

    [Fact]
    public void SceneManager_Play_WhenCompileFails_DoesNotClearHistory()
    {
        var history = Substitute.For<IEditorHistory>();
        var root = Path.Combine(Path.GetTempPath(), $"ge-play-fail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var missingScripts = Path.Combine(root, "scripts-missing");

        try
        {
            var projectContext = Substitute.For<IProjectContext>();
            projectContext.Root.Returns(root);
            projectContext.ScriptsDir.Returns(missingScripts);
            _sceneContext.ActiveScene.Returns(Substitute.For<IScene>());

            var manager = CreateSceneManager(history, projectContext);
            manager.Play();

            history.DidNotReceive().Clear();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SceneManager_Play_WhenCompileSucceeds_ClearsHistory()
    {
        var history = Substitute.For<IEditorHistory>();
        var root = Path.Combine(Path.GetTempPath(), $"ge-play-ok-{Guid.NewGuid():N}");
        var scriptsDir = Path.Combine(root, "scripts");
        Directory.CreateDirectory(scriptsDir);

        try
        {
            var projectContext = Substitute.For<IProjectContext>();
            projectContext.Root.Returns(root);
            projectContext.ScriptsDir.Returns(scriptsDir);
            // Empty scripts dir still compiles (placeholder); keep scene thin for unit speed
            _sceneContext.ActiveScene.Returns(Substitute.For<IScene>());

            var manager = CreateSceneManager(history, projectContext);
            manager.Play();

            history.Received(1).Clear();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SceneManager_Stop_WhenNoEditorScenePath_ClearsHistory()
    {
        var history = Substitute.For<IEditorHistory>();
        var projectContext = Substitute.For<IProjectContext>();
        var scene = Substitute.For<IScene>();
        _sceneContext.ActiveScene.Returns(scene);

        var manager = CreateSceneManager(history, projectContext);
        // EditorScenePath stays null — Stop takes dispose-without-Open branch

        manager.Stop();

        history.Received(1).Clear();
        scene.Received(1).Dispose();
    }

    private SceneManager CreateSceneManager(IEditorHistory history, IProjectContext projectContext)
    {
        var workspace = new GameScriptWorkspace(
            Substitute.For<IScriptEngine>(),
            Substitute.For<IComponentSerializerRegistry>(),
            _ => true,
            _ => { });

        var modelFactory = Substitute.For<IModelFactory>();

        var factory = new SceneFactory(
            Substitute.For<ISystemManagerFactory>(),
            Substitute.For<IPointerSurface>());

        return new SceneManager(
            _sceneContext,
            Substitute.For<ISceneSerializer>(),
            factory,
            Enumerable.Empty<IGameSystem>,
            projectContext,
            workspace,
            history,
            modelFactory);
    }
}
