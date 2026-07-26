using Editor.Features.Scene;
using Editor.Features.Viewport;
using Engine.Renderer;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Viewport;

public class EditorViewportWireframeApplyRestoreTests
{
    [Fact]
    public void ApplyWireframeDuring_WhenWireframe_SetsTrueBeforeSceneAndFalseAfter()
    {
        var graphics3D = Substitute.For<IGraphics3D>();
        var order = new List<string>();
        graphics3D.When(g => g.SetWireframe(true)).Do(_ => order.Add("enable"));
        graphics3D.When(g => g.SetWireframe(false)).Do(_ => order.Add("disable"));

        EditorViewport.ApplyWireframeDuring(
            graphics3D,
            ViewportDisplayMode.Wireframe,
            () => order.Add("scene"));

        order.ShouldBe(["enable", "scene", "disable"]);
    }

    [Fact]
    public void ApplyWireframeDuring_WhenNormal_SetsFalseBeforeAndAfterScene()
    {
        var graphics3D = Substitute.For<IGraphics3D>();
        var order = new List<string>();
        graphics3D.When(g => g.SetWireframe(false)).Do(_ => order.Add("disable"));
        graphics3D.When(g => g.SetWireframe(true)).Do(_ => order.Add("enable"));

        EditorViewport.ApplyWireframeDuring(
            graphics3D,
            ViewportDisplayMode.Normal,
            () => order.Add("scene"));

        order.ShouldBe(["disable", "scene", "disable"]);
        graphics3D.DidNotReceive().SetWireframe(true);
    }

    [Fact]
    public void ApplyWireframeDuring_WhenSceneThrows_StillSetsWireframeFalse()
    {
        var graphics3D = Substitute.For<IGraphics3D>();

        Should.Throw<InvalidOperationException>(() =>
            EditorViewport.ApplyWireframeDuring(
                graphics3D,
                ViewportDisplayMode.Wireframe,
                () => throw new InvalidOperationException("boom")));

        Received.InOrder(() =>
        {
            graphics3D.SetWireframe(true);
            graphics3D.SetWireframe(false);
        });
    }
}
