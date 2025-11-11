using System.Numerics;
using Engine.Scene.Components;
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
        component.Texture.ShouldBeNull();
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
        component.Texture.ShouldBeNull();
        component.TilingFactor.ShouldBe(1.0f);
    }

    [Fact]
    public void SpriteRendererComponent_FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var tilingFactor = 2.5f;

        // Act
        var component = new SpriteRendererComponent(color, null, tilingFactor);

        // Assert
        component.Color.ShouldBe(color);
        component.Texture.ShouldBeNull();
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
        var original = new SpriteRendererComponent(color, null, 2.0f);

        // Act
        var clone = (SpriteRendererComponent)original.Clone();
        clone.Color = Vector4.Zero;

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.TilingFactor.ShouldBe(2.0f);
        original.Color.ShouldBe(color);
        clone.Color.ShouldBe(Vector4.Zero);
    }
}