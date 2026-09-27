using ECS;
using Editor.Features.Selection;
using Editor.Features.Viewport;
using Editor.UI.Elements;
using Engine.Scene;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Selection;

public class EditorSelectionTests
{
    [Fact]
    public void Select_SameHierarchyEntity_StillRaisesSelectionChanged()
    {
        var entity = new Entity(1, "e");
        var (selection, _, _) = CreateSelection(entity);

        Entity? last = null;
        SelectionSource lastSource = default;
        selection.SelectionChanged += (e, source) =>
        {
            last = e;
            lastSource = source;
        };

        selection.Select(entity, SelectionSource.Hierarchy);
        selection.Select(entity, SelectionSource.Hierarchy);

        last.ShouldBe(entity);
        lastSource.ShouldBe(SelectionSource.Hierarchy);
    }

    [Fact]
    public void Select_SameViewportEntity_DoesNotRaiseAgain()
    {
        var entity = new Entity(1, "e");
        var (selection, _, _) = CreateSelection(entity);

        var count = 0;
        selection.SelectionChanged += (_, _) => count++;

        selection.Select(entity, SelectionSource.Viewport);
        selection.Select(entity, SelectionSource.Viewport);

        count.ShouldBe(1);
    }

    [Fact]
    public void Select_Hierarchy_CallsCameraFraming()
    {
        var entity = new Entity(1, "e");
        var (selection, _, cameraFraming) = CreateSelection(entity);

        selection.Select(entity, SelectionSource.Hierarchy);

        cameraFraming.Received(1).FocusOnEntity(entity, false);
    }

    [Fact]
    public void Toggle_AddsAndRemoves_DoesNotFrameCamera_PrimaryBecomesLastWhenPrimaryRemoved()
    {
        var e1 = new Entity(1, "a");
        var e2 = new Entity(2, "b");
        var (selection, _, cameraFraming) = CreateSelection(e1, e2);

        selection.Select(e1, SelectionSource.Hierarchy);
        cameraFraming.ClearReceivedCalls();
        selection.Toggle(e2);

        selection.SelectedEntities.Count.ShouldBe(2);
        selection.SelectedEntity.ShouldBe(e2);

        selection.Toggle(e2);
        selection.SelectedEntities.Count.ShouldBe(1);
        selection.SelectedEntity.ShouldBe(e1);

        selection.Toggle(e1);
        selection.SelectedEntity.ShouldBeNull();

        cameraFraming.DidNotReceive().FocusOnEntity(Arg.Any<Entity>(), Arg.Any<bool>());
    }

    [Fact]
    public void SelectRange_SelectsVisibleSpan_KeepsAnchor_SetsPrimaryToClicked()
    {
        var e1 = new Entity(1, "a");
        var e2 = new Entity(2, "b");
        var e3 = new Entity(3, "c");
        var (selection, _, cameraFraming) = CreateSelection(e1, e2, e3);
        var visible = new List<Entity> { e1, e2, e3 };

        selection.Select(e1, SelectionSource.Hierarchy);
        cameraFraming.ClearReceivedCalls();

        selection.SelectRange(visible, e3);

        selection.SelectedEntities.Select(e => e.Id).ShouldBe([1, 2, 3]);
        selection.SelectedEntity.ShouldBe(e3);
        cameraFraming.Received(1).FocusOnEntity(e3, false);

        selection.SelectRange(visible, e2);
        selection.SelectedEntities.Select(e => e.Id).ShouldBe([1, 2]);
        selection.SelectedEntity.ShouldBe(e2);
    }

    [Fact]
    public void SelectRange_MissingAnchor_ReplacesWithClicked()
    {
        var e1 = new Entity(1, "a");
        var e2 = new Entity(2, "b");
        var e3 = new Entity(3, "c");
        var (selection, _, _) = CreateSelection(e1, e2, e3);
        var visible = new List<Entity> { e2, e3 };

        selection.Select(e1, SelectionSource.Hierarchy);
        selection.SelectRange(visible, e3);

        selection.SelectedEntities.Count.ShouldBe(1);
        selection.SelectedEntity.ShouldBe(e3);
    }

    [Fact]
    public void Prune_RemovesDestroyedEntityFromSelection()
    {
        var e1 = new Entity(1, "a");
        var e2 = new Entity(2, "b");
        var sceneContext = Substitute.For<ISceneContext>();
        var scene = Substitute.For<IScene>();
        sceneContext.ActiveScene.Returns(scene);
        scene.Entities.Returns(new[] { e1, e2 });

        var selection = new EditorSelection(sceneContext, Substitute.For<IEditorCameraFraming>());
        selection.Select(e1, SelectionSource.Hierarchy);
        selection.Toggle(e2);
        selection.SelectedEntities.Select(e => e.Id).ShouldBe([1, 2]);

        scene.Entities.Returns(new[] { e1 });
        selection.SelectedEntities.Select(e => e.Id).ShouldBe([1]);
    }

    [Fact]
    public void SameFloat_RoundsToTwoDecimals_NonFiniteIsNeverUniform()
    {
        MultiField.SameFloat(1.001f, 1.0f).ShouldBeTrue();
        MultiField.SameFloat(float.NaN, float.NaN).ShouldBeFalse();
    }

    private static (EditorSelection selection, ISceneContext sceneContext, IEditorCameraFraming cameraFraming)
        CreateSelection(params Entity[] entities)
    {
        var sceneContext = Substitute.For<ISceneContext>();
        var cameraFraming = Substitute.For<IEditorCameraFraming>();
        var scene = Substitute.For<IScene>();
        scene.Entities.Returns(entities);
        sceneContext.ActiveScene.Returns(scene);
        return (new EditorSelection(sceneContext, cameraFraming), sceneContext, cameraFraming);
    }
}
