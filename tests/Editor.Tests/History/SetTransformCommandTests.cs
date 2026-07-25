using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Engine.Scene;
using NSubstitute;
using SceneComponents;
using Shouldly;

namespace Editor.Tests.History;

/// <summary>
/// Wrap-site: MoveTool / RotateTool / ScaleTool push SetTransformCommand on mouse-up when TRS dirty
/// (shared TrsEqual gate — one invert suite covers all three tools).
/// </summary>
public class SetTransformCommandTests
{
    private readonly ISceneContext _sceneContext = Substitute.For<ISceneContext>();

    public SetTransformCommandTests()
    {
        _sceneContext.State.Returns(SceneState.Edit);
    }

    private static (IScene scene, Entity entity, TransformComponent transform) CreateSceneEntity(
        Vector3 translation, Vector3 rotation, Vector3 scale)
    {
        var entity = Entity.Create(1, "target");
        var transform = entity.AddComponent(new TransformComponent(translation, rotation, scale));
        var context = Substitute.For<IContext>();
        context.Contains(entity.Id).Returns(true);
        context.GetById(entity.Id).Returns(entity);
        var scene = Substitute.For<IScene>();
        scene.Context.Returns(context);
        scene.Entities.Returns([entity]);
        return (scene, entity, transform);
    }

    private static SetTransformCommand CreateCommand(
        IScene scene,
        int entityId,
        Vector3 beforeT, Vector3 beforeR, Vector3 beforeS,
        Vector3 afterT, Vector3 afterR, Vector3 afterS)
        => new(scene, entityId, beforeT, beforeR, beforeS, afterT, afterR, afterS);

    [Fact]
    public void Execute_AppliesAfterTrs_Undo_RestoresBeforeTrs()
    {
        var beforeT = new Vector3(1, 2, 3);
        var beforeR = new Vector3(0.1f, 0.2f, 0.3f);
        var beforeS = new Vector3(1, 1, 1);
        var afterT = new Vector3(10, 20, 30);
        var afterR = new Vector3(1, 2, 3);
        var afterS = new Vector3(2, 3, 4);

        var (scene, entity, transform) = CreateSceneEntity(beforeT, beforeR, beforeS);
        var command = CreateCommand(scene, entity.Id, beforeT, beforeR, beforeS, afterT, afterR, afterS);

        command.Execute();

        transform.Translation.ShouldBe(afterT);
        transform.Rotation.ShouldBe(afterR);
        transform.Scale.ShouldBe(afterS);

        command.Undo();

        transform.Translation.ShouldBe(beforeT);
        transform.Rotation.ShouldBe(beforeR);
        transform.Scale.ShouldBe(beforeS);
    }

    [Fact]
    public void Redo_ReAppliesAfterTrs()
    {
        var beforeT = Vector3.Zero;
        var beforeR = Vector3.Zero;
        var beforeS = Vector3.One;
        var afterT = new Vector3(5, 0, 0);
        var afterR = new Vector3(0, 0, 1.5f);
        var afterS = new Vector3(2, 2, 2);

        var (scene, entity, transform) = CreateSceneEntity(beforeT, beforeR, beforeS);
        var history = new EditorHistory(_sceneContext);
        var command = CreateCommand(scene, entity.Id, beforeT, beforeR, beforeS, afterT, afterR, afterS);

        history.Execute(command);
        history.Undo();
        transform.Translation.ShouldBe(beforeT);

        history.Redo();

        transform.Translation.ShouldBe(afterT);
        transform.Rotation.ShouldBe(afterR);
        transform.Scale.ShouldBe(afterS);
    }

    [Fact]
    public void Execute_WhenBeforeEqualsAfter_LeavesTransformUnchanged()
    {
        var trs = new Vector3(4, 5, 6);
        var rot = new Vector3(0.5f, 0, 0);
        var scale = new Vector3(1.5f, 1.5f, 1.5f);

        var (scene, entity, transform) = CreateSceneEntity(trs, rot, scale);
        var command = CreateCommand(scene, entity.Id, trs, rot, scale, trs, rot, scale);

        command.Execute();

        transform.Translation.ShouldBe(trs);
        transform.Rotation.ShouldBe(rot);
        transform.Scale.ShouldBe(scale);
    }

    [Fact]
    public void TrsEqual_WhenIdentical_IsTrue_WhenDifferent_IsFalse()
    {
        // Tools skip history.Execute when before/after TRS are equal (dirty check).
        var a = new Vector3(1, 2, 3);
        var b = new Vector3(1, 2, 3);
        var c = new Vector3(9, 9, 9);

        SetTransformCommand.TrsEqual(a, a, a, b, b, b).ShouldBeTrue();
        SetTransformCommand.TrsEqual(a, a, a, c, a, a).ShouldBeFalse();
        SetTransformCommand.TrsEqual(a, a, a, a, c, a).ShouldBeFalse();
        SetTransformCommand.TrsEqual(a, a, a, a, a, c).ShouldBeFalse();
    }
}
