using System.Numerics;
using Engine.Core;
using Engine.Scene;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Components;

public class SpriteRendererComponentTests
{
    [Fact]
    public void SpriteRendererComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new SpriteRendererComponent();

        // Assert
        component.Color.ShouldBe(Vector4.One);
        component.TexturePath.ShouldBeNull();
        component.TilingFactor.ShouldBe(1.0f);
    }

    [Fact]
    public void SpriteRendererComponent_ColorConstructor_ShouldSetColor()
    {
        // Arrange
        var color = new Vector4(1f, 0f, 0f, 1f); // Red

        // Act
        var component = new SpriteRendererComponent(color);

        // Assert
        component.Color.ShouldBe(color);
        component.TexturePath.ShouldBeNull();
        component.TilingFactor.ShouldBe(1.0f);
    }

    [Fact]
    public void SpriteRendererComponent_Properties_ShouldSetAllValues()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var tilingFactor = 2.5f;

        // Act
        var component = new SpriteRendererComponent(color)
        {
            TexturePath = "textures/test.png",
            TilingFactor = tilingFactor
        };

        // Assert
        component.Color.ShouldBe(color);
        component.TexturePath.ShouldBe("textures/test.png");
        component.TilingFactor.ShouldBe(tilingFactor);
    }

    [Fact]
    public void SpriteRendererComponent_SetColor_ShouldUpdateValue()
    {
        // Arrange
        var component = new SpriteRendererComponent();
        var newColor = new Vector4(0.2f, 0.4f, 0.6f, 0.8f);

        // Act
        component.Color = newColor;

        // Assert
        component.Color.ShouldBe(newColor);
    }

    [Fact]
    public void SpriteRendererComponent_SetTilingFactor_ShouldUpdateValue()
    {
        // Arrange
        var component = new SpriteRendererComponent();

        // Act
        component.TilingFactor = 3.0f;

        // Assert
        component.TilingFactor.ShouldBe(3.0f);
    }

    [Fact]
    public void SpriteRendererComponent_Clone_ShouldCopyAllProperties()
    {
        // Arrange
        var color = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);
        var original = new SpriteRendererComponent(color)
        {
            TexturePath = "textures/player.png",
            TilingFactor = 2.0f
        };

        // Act
        var clone = (SpriteRendererComponent)original.Clone();
        clone.Color = Vector4.Zero;

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.TilingFactor.ShouldBe(2.0f);
        clone.TexturePath.ShouldBe("textures/player.png");
        original.Color.ShouldBe(color);
        clone.Color.ShouldBe(Vector4.Zero);
    }

    [Fact]
    public void TexturePath_Change_ClearsResolvedTexturePath()
    {
        var component = new SpriteRendererComponent { TexturePath = "textures/a.png" };
        component.ResolvedTexturePath = "/cached/path.png";

        component.TexturePath = "textures/b.png";

        component.ResolvedTexturePath.ShouldBeNull();
    }
}

[Collection("PathBuilder")]
public class SpriteRendererResolvedTexturePathTests : IDisposable
{
    private static string GameAssets =>
        OperatingSystem.IsWindows() ? @"C:\game\assets" : "/game/assets";

    public SpriteRendererResolvedTexturePathTests()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(GameAssets);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose() => PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());

    [Fact]
    public void GetResolvedSpriteTexturePath_ReusesCache_WhenTexturePathUnchanged()
    {
        var component = new SpriteRendererComponent { TexturePath = "textures/player.png" };

        var first = SceneRenderPipeline.GetResolvedSpriteTexturePath(component);
        var second = SceneRenderPipeline.GetResolvedSpriteTexturePath(component);

        ReferenceEquals(first, second).ShouldBeTrue();
    }
}