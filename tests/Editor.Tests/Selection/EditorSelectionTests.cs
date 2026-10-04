using ECS;
using Editor.Features.Selection;
using Editor.Features.Viewport;
using Engine.Scene;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Selection;

public class EditorSelectionTests
{
    [Fact]
    public void Select_SameHierarchyEntity_StillRaisesSelectionChanged()
    {
        var sceneContext = Substitute.For<ISceneContext>();
        var cameraFraming = Substitute.For<IEditorCameraFraming>();
        var selection = new EditorSelection(sceneContext, cameraFraming);
        var entity = new Entity(1, "e");

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
        var sceneContext = Substitute.For<ISceneContext>();
        var cameraFraming = Substitute.For<IEditorCameraFraming>();
        var selection = new EditorSelection(sceneContext, cameraFraming);
        var entity = new Entity(1, "e");

        var count = 0;
        selection.SelectionChanged += (_, _) => count++;

        selection.Select(entity, SelectionSource.Viewport);
        selection.Select(entity, SelectionSource.Viewport);

        count.ShouldBe(1);
    }

    [Fact]
    public void Select_Hierarchy_CallsCameraFraming()
    {
        var sceneContext = Substitute.For<ISceneContext>();
        var cameraFraming = Substitute.For<IEditorCameraFraming>();
        var selection = new EditorSelection(sceneContext, cameraFraming);
        var entity = new Entity(1, "e");

        selection.Select(entity, SelectionSource.Hierarchy);

        cameraFraming.Received(1).FocusOnEntity(entity, false);
    }
}
