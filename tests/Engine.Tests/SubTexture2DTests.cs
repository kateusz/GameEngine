using System.Numerics;
using Bogus;
using Engine.Renderer.Textures;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Engine.Tests;

public class SubTexture2DTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void SubTexture2D_Constructor_ShouldSetTexture()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        var min = new Vector2(0f, 0f);
        var max = new Vector2(1f, 1f);

        // Act
        var subTexture = new SubTexture2D(mockTexture, min, max);

        // Assert
        subTexture.Texture.ShouldBe(mockTexture);
    }

    [Fact]
    public void SubTexture2D_Constructor_ShouldSetTexCoordsCorrectly()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        var min = new Vector2(0.25f, 0.25f);
        var max = new Vector2(0.75f, 0.75f);

        // Act
        var subTexture = new SubTexture2D(mockTexture, min, max);

        // Assert
        subTexture.TexCoords.Length.ShouldBe(4); // QuadVertexCount
        subTexture.TexCoords[0].ShouldBe(new Vector2(0.25f, 0.25f)); // Bottom-left
        subTexture.TexCoords[1].ShouldBe(new Vector2(0.75f, 0.25f)); // Bottom-right
        subTexture.TexCoords[2].ShouldBe(new Vector2(0.75f, 0.75f)); // Top-right
        subTexture.TexCoords[3].ShouldBe(new Vector2(0.25f, 0.75f)); // Top-left
    }

    [Fact]
    public void SubTexture2D_Constructor_WithFullTexture_ShouldCreateFullUVs()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        var min = Vector2.Zero;
        var max = Vector2.One;

        // Act
        var subTexture = new SubTexture2D(mockTexture, min, max);

        // Assert - Should cover entire texture
        subTexture.TexCoords[0].ShouldBe(new Vector2(0f, 0f));
        subTexture.TexCoords[1].ShouldBe(new Vector2(1f, 0f));
        subTexture.TexCoords[2].ShouldBe(new Vector2(1f, 1f));
        subTexture.TexCoords[3].ShouldBe(new Vector2(0f, 1f));
    }

    [Fact]
    public void SubTexture2D_CreateFromCoords_ShouldCalculateCorrectUVs()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        mockTexture.Width.Returns(256);
        mockTexture.Height.Returns(256);

        var coords = new Vector2(1, 1); // Second sprite in grid
        var cellSize = new Vector2(32, 32); // 32x32 sprites
        var spriteSize = Vector2.One; // 1x1 sprite

        // Act
        var subTexture = SubTexture2D.CreateFromCoords(mockTexture, coords, cellSize, spriteSize);

        // Assert
        // Min should be (1*32)/256, (1*32)/256 = (0.125, 0.125)
        // Max should be ((1+1)*32)/256, ((1+1)*32)/256 = (0.25, 0.25)
        subTexture.TexCoords[0].X.ShouldBe(0.125f, 0.0001f);
        subTexture.TexCoords[0].Y.ShouldBe(0.125f, 0.0001f);
        subTexture.TexCoords[2].X.ShouldBe(0.25f, 0.0001f);
        subTexture.TexCoords[2].Y.ShouldBe(0.25f, 0.0001f);
    }

    [Fact]
    public void SubTexture2D_CreateFromCoords_FirstSprite_ShouldStartAtOrigin()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        mockTexture.Width.Returns(128);
        mockTexture.Height.Returns(128);

        var coords = Vector2.Zero; // First sprite
        var cellSize = new Vector2(16, 16);
        var spriteSize = Vector2.One;

        // Act
        var subTexture = SubTexture2D.CreateFromCoords(mockTexture, coords, cellSize, spriteSize);

        // Assert
        // Min should be (0, 0)
        // Max should be (16/128, 16/128) = (0.125, 0.125)
        subTexture.TexCoords[0].ShouldBe(new Vector2(0f, 0f));
        subTexture.TexCoords[2].X.ShouldBe(0.125f, 0.0001f);
        subTexture.TexCoords[2].Y.ShouldBe(0.125f, 0.0001f);
    }

    [Fact]
    public void SubTexture2D_CreateFromCoords_WithLargerSprite_ShouldSpanMultipleCells()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        mockTexture.Width.Returns(256);
        mockTexture.Height.Returns(256);

        var coords = new Vector2(0, 0);
        var cellSize = new Vector2(32, 32);
        var spriteSize = new Vector2(2, 2); // 2x2 sprite (64x64 pixels)

        // Act
        var subTexture = SubTexture2D.CreateFromCoords(mockTexture, coords, cellSize, spriteSize);

        // Assert
        // Max should be (2*32)/256, (2*32)/256 = (0.25, 0.25)
        subTexture.TexCoords[2].X.ShouldBe(0.25f, 0.0001f);
        subTexture.TexCoords[2].Y.ShouldBe(0.25f, 0.0001f);
    }

    [Fact]
    public void SubTexture2D_CreateFromCoords_NonSquareTexture_ShouldHandleCorrectly()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        mockTexture.Width.Returns(512);
        mockTexture.Height.Returns(256);

        var coords = new Vector2(2, 1);
        var cellSize = new Vector2(64, 64);
        var spriteSize = Vector2.One;

        // Act
        var subTexture = SubTexture2D.CreateFromCoords(mockTexture, coords, cellSize, spriteSize);

        // Assert
        // Min X: (2*64)/512 = 0.25
        // Min Y: (1*64)/256 = 0.25
        // Max X: (3*64)/512 = 0.375
        // Max Y: (2*64)/256 = 0.5
        subTexture.TexCoords[0].X.ShouldBe(0.25f, 0.0001f);
        subTexture.TexCoords[0].Y.ShouldBe(0.25f, 0.0001f);
        subTexture.TexCoords[2].X.ShouldBe(0.375f, 0.0001f);
        subTexture.TexCoords[2].Y.ShouldBe(0.5f, 0.0001f);
    }

    [Fact]
    public void SubTexture2D_Deconstruct_ShouldReturnTextureAndCoords()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        var subTexture = new SubTexture2D(mockTexture, Vector2.Zero, Vector2.One);

        // Act
        var (texture, texCoords) = subTexture;

        // Assert
        texture.ShouldBe(mockTexture);
        texCoords.ShouldBe(subTexture.TexCoords);
    }

    [Fact]
    public void SubTexture2D_AsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        var subTexture1 = new SubTexture2D(mockTexture, Vector2.Zero, Vector2.One);
        var subTexture2 = new SubTexture2D(mockTexture, Vector2.Zero, Vector2.One);

        // Act & Assert - Records support structural equality
        subTexture1.ShouldNotBeSameAs(subTexture2);
        // Note: Records compare by value, but Texture2D is a reference type
    }

    [Fact]
    public void SubTexture2D_TexCoords_ShouldAlwaysHave4Elements()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        var min = new Vector2(_faker.Random.Float(), _faker.Random.Float());
        var max = new Vector2(_faker.Random.Float(), _faker.Random.Float());

        // Act
        var subTexture = new SubTexture2D(mockTexture, min, max);

        // Assert
        subTexture.TexCoords.Length.ShouldBe(4);
    }

    [Fact]
    public void SubTexture2D_TexCoords_ShouldFormCounterClockwiseQuad()
    {
        // Arrange
        var mockTexture = Substitute.For<Texture2D>();
        var min = new Vector2(0.2f, 0.3f);
        var max = new Vector2(0.7f, 0.8f);

        // Act
        var subTexture = new SubTexture2D(mockTexture, min, max);

        // Assert - Order should be: bottom-left, bottom-right, top-right, top-left
        var bl = subTexture.TexCoords[0];
        var br = subTexture.TexCoords[1];
        var tr = subTexture.TexCoords[2];
        var tl = subTexture.TexCoords[3];

        bl.X.ShouldBe(min.X);
        bl.Y.ShouldBe(min.Y);

        br.X.ShouldBe(max.X);
        br.Y.ShouldBe(min.Y);

        tr.X.ShouldBe(max.X);
        tr.Y.ShouldBe(max.Y);

        tl.X.ShouldBe(min.X);
        tl.Y.ShouldBe(max.Y);
    }

    [Fact]
    public void SubTexture2D_CreateFromCoords_SpriteSheet8x8_ShouldCalculateCorrectly()
    {
        // Arrange - Simulating a typical sprite sheet
        var mockTexture = Substitute.For<Texture2D>();
        mockTexture.Width.Returns(256); // 8 sprites wide
        mockTexture.Height.Returns(256); // 8 sprites tall

        var cellSize = new Vector2(32, 32);
        var spriteSize = Vector2.One;

        // Act - Get sprite at (3, 2) in an 8x8 grid
        var coords = new Vector2(3, 2);
        var subTexture = SubTexture2D.CreateFromCoords(mockTexture, coords, cellSize, spriteSize);

        // Assert
        // Expected min: (3*32/256, 2*32/256) = (0.375, 0.25)
        // Expected max: (4*32/256, 3*32/256) = (0.5, 0.375)
        subTexture.TexCoords[0].X.ShouldBe(0.375f, 0.0001f);
        subTexture.TexCoords[0].Y.ShouldBe(0.25f, 0.0001f);
        subTexture.TexCoords[2].X.ShouldBe(0.5f, 0.0001f);
        subTexture.TexCoords[2].Y.ShouldBe(0.375f, 0.0001f);
    }
}