using Editor.Features.History;
using Engine.Scene;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.History;

public class EditorHistoryTests
{
    private readonly ISceneContext _sceneContext = Substitute.For<ISceneContext>();

    public EditorHistoryTests()
    {
        _sceneContext.State.Returns(SceneState.Edit);
    }

    private EditorHistory CreateHistory() => new(_sceneContext);

    private static IUndoCommand FakeCommand(bool applied = true)
    {
        var command = Substitute.For<IUndoCommand>();
        command.Execute().Returns(applied);
        return command;
    }

    [Fact]
    public void Execute_RunsCommand_PushesUndo_ClearsRedo()
    {
        var history = CreateHistory();
        var first = FakeCommand();
        var second = FakeCommand();

        history.Execute(first);
        history.Undo();
        history.CanRedo.ShouldBeTrue();

        history.Execute(second);

        first.Received(1).Execute();
        second.Received(1).Execute();
        history.CanUndo.ShouldBeTrue();
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void Execute_WhenCommandReturnsFalse_DoesNotPush()
    {
        var history = CreateHistory();
        var command = FakeCommand(applied: false);

        history.Execute(command);

        command.Received(1).Execute();
        history.CanUndo.ShouldBeFalse();
    }

    [Fact]
    public void Undo_WhenCommandThrows_RestoresUndoStack()
    {
        var history = CreateHistory();
        var command = FakeCommand();
        command.When(c => c.Undo()).Do(_ => throw new InvalidOperationException("boom"));

        history.Execute(command);
        Should.Throw<InvalidOperationException>(() => history.Undo());

        history.CanUndo.ShouldBeTrue();
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void Undo_PopsUndo_CallsUndo_PushesRedo()
    {
        var history = CreateHistory();
        var command = FakeCommand();

        history.Execute(command);
        history.Undo();

        command.Received(1).Undo();
        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeTrue();
    }

    [Fact]
    public void Redo_ReExecutes_AndPushesUndo()
    {
        var history = CreateHistory();
        var command = FakeCommand();

        history.Execute(command);
        history.Undo();
        history.Redo();

        command.Received(2).Execute();
        history.CanUndo.ShouldBeTrue();
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void Execute_WhenDepthExceeded_DropsOldestUndoEntry()
    {
        var history = CreateHistory();
        var commands = Enumerable.Range(0, 101).Select(_ => FakeCommand()).ToArray();

        foreach (var command in commands)
            history.Execute(command);

        for (var i = 0; i < 100; i++)
            history.Undo();

        history.CanUndo.ShouldBeFalse();
        commands[0].DidNotReceive().Undo();
        commands[1].Received(1).Undo();
        commands[100].Received(1).Undo();
    }

    [Fact]
    public void Clear_EmptiesBothStacks_AndCanFlagsReflectState()
    {
        var history = CreateHistory();
        var command = FakeCommand();

        history.Execute(command);
        history.Undo();
        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeTrue();

        history.Clear();

        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeFalse();
        history.Undo();
        history.Redo();
        command.Received(1).Undo();
        command.Received(1).Execute();
    }

    [Fact]
    public void ExecuteUndoRedo_WhenNotEdit_AreNoOps()
    {
        var history = CreateHistory();
        var command = FakeCommand();

        _sceneContext.State.Returns(SceneState.Play);

        history.Execute(command);
        history.Undo();
        history.Redo();

        command.DidNotReceive().Execute();
        command.DidNotReceive().Undo();
        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void ExecuteUndoRedo_WhenReturningToEdit_WorkAgain()
    {
        var history = CreateHistory();
        var command = FakeCommand();

        history.Execute(command);
        _sceneContext.State.Returns(SceneState.Play);
        history.Undo();
        command.Received(0).Undo();

        _sceneContext.State.Returns(SceneState.Edit);
        history.Undo();

        command.Received(1).Undo();
        history.CanRedo.ShouldBeTrue();
    }
}
