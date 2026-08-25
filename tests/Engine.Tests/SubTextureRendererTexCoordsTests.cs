using System.Numerics;
using Engine.Scene;
using Engine.Renderer.Textures;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests;

public class SubTextureRendererTexCoordsTests
{
    [Fact]
    public void Get_ReusesTexCoordsArray_WhenInputsUnchanged()
    {
        var texture = Substitute.For<Texture2D>();
        texture.Width.Returns(256);
        texture.Height.Returns(256);

        var component = new SubTextureRendererComponent
        {
            Coords = new Vector2(1, 1),
            CellSize = new Vector2(32, 32),
            SpriteSize = Vector2.One,
        };

        var first = SceneRenderPipeline.GetSubTextureTexCoords(component, texture);
        var second = SceneRenderPipeline.GetSubTextureTexCoords(component, texture);

        ReferenceEquals(first, second).ShouldBeTrue();
        first[0].X.ShouldBe(0.125f, 0.0001f);
    }

    [Fact]
    public void Get_RecomputesIntoSameArray_WhenCoordsChange()
    {
        var texture = Substitute.For<Texture2D>();
        texture.Width.Returns(256);
        texture.Height.Returns(256);

        var component = new SubTextureRendererComponent
        {
            Coords = new Vector2(0, 0),
            CellSize = new Vector2(32, 32),
            SpriteSize = Vector2.One,
        };

        var first = SceneRenderPipeline.GetSubTextureTexCoords(component, texture);
        component.Coords = new Vector2(1, 1);
        var second = SceneRenderPipeline.GetSubTextureTexCoords(component, texture);

        ReferenceEquals(first, second).ShouldBeTrue();
        second[0].X.ShouldBe(0.125f, 0.0001f);
    }

    [Fact]
    public void Get_UsesManualTexCoords_WhenSetExplicitly()
    {
        var texture = Substitute.For<Texture2D>();
        texture.Width.Returns(256);
        texture.Height.Returns(256);

        var manual = new Vector2[]
        {
            new(0.1f, 0.2f),
            new(0.3f, 0.2f),
            new(0.3f, 0.4f),
            new(0.1f, 0.4f),
        };

        var component = new SubTextureRendererComponent { TexCoords = manual };
        var result = SceneRenderPipeline.GetSubTextureTexCoords(component, texture);

        ReferenceEquals(result, manual).ShouldBeTrue();
    }
}
