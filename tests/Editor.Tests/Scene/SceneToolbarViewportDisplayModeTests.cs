using Editor.Features.Scene;
using Engine.Renderer.Textures;
using Engine.Scene;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Scene;

public class SceneToolbarViewportDisplayModeTests
{
    private readonly ISceneContext _sceneContext = Substitute.For<ISceneContext>();
    private readonly ITextureFactory _textureFactory = Substitute.For<ITextureFactory>();

    private SceneToolbar CreateToolbar() => new(_sceneContext, _textureFactory);

    [Fact]
    public void ViewportDisplayMode_DefaultsToNormal()
    {
        CreateToolbar().ViewportDisplayMode.ShouldBe(ViewportDisplayMode.Normal);
    }

    [Fact]
    public void SettingViewportDisplayMode_DoesNotChangeSceneDimension()
    {
        var scene = Substitute.For<IScene>();
        scene.Dimension = SceneDimension.ThreeD;
        _sceneContext.ActiveScene.Returns(scene);
        var toolbar = CreateToolbar();
        scene.ClearReceivedCalls();

        toolbar.ViewportDisplayMode = ViewportDisplayMode.Wireframe;
        toolbar.ViewportDisplayMode = ViewportDisplayMode.Normal;

        scene.DidNotReceive().Dimension = Arg.Any<SceneDimension>();
    }
}
