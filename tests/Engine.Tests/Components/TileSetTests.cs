using System.Numerics;
using Engine.Renderer.Textures;
using Engine.Tiles;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Components;

public class TileSetTests
{
    [Fact]
    public void GetUniqueTiles_WithEmptyTiles_ShouldReturnEmptyList()
    {
        // Arrange
        var tileSet = new TileSet
        {
            TexturePath = "/nonexistent/path/to/texture.png",
            Columns = 8,
            Rows = 8
        };

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles.ShouldBeEmpty();
    }

    [Fact]
    public void GetUniqueTiles_WithoutTextureFile_ShouldReturnAllTilesAsUnique()
    {
        // Arrange
        var tileSet = new TileSet
        {
            TexturePath = "/nonexistent/path/to/texture.png",
            Columns = 8,
            Rows = 8
        };
        var mockTexture = Substitute.For<Texture2D>();
        
        // Create 4 tiles - without a texture file, all should be returned as unique
        for (var i = 0; i < 4; i++)
        {
            var min = new Vector2(i * 0.25f, 0f);
            var max = new Vector2((i + 1) * 0.25f, 0.25f);
            tileSet.Tiles.Add(new Tile
            {
                Id = i,
                Name = $"Tile_{i}",
                SubTexture = new SubTexture2D(mockTexture, min, max)
            });
        }

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert - Without texture file, fallback returns all tiles as unique
        uniqueTiles.Count.ShouldBe(4);
        for (var i = 0; i < 4; i++)
        {
            uniqueTiles[i].Id.ShouldBe(i);
        }
    }

    [Fact]
    public void GetUniqueTiles_WithNullSubTextures_ShouldSkipNullTiles()
    {
        // Arrange
        var tileSet = new TileSet
        {
            TexturePath = "/nonexistent/path/to/texture.png",
            Columns = 8,
            Rows = 8
        };
        var mockTexture = Substitute.For<Texture2D>();
        
        var min = new Vector2(0f, 0f);
        var max = new Vector2(0.25f, 0.25f);
        
        tileSet.Tiles.Add(new Tile { Id = 0, Name = "Tile_0", SubTexture = new SubTexture2D(mockTexture, min, max) });
        tileSet.Tiles.Add(new Tile { Id = 1, Name = "Tile_1", SubTexture = null });
        tileSet.Tiles.Add(new Tile { Id = 2, Name = "Tile_2", SubTexture = new SubTexture2D(mockTexture, min, max) });

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert - Without texture file, fallback returns all non-null tiles as unique
        uniqueTiles.Count.ShouldBe(2);
        uniqueTiles[0].Id.ShouldBe(0);
        uniqueTiles[1].Id.ShouldBe(2);
    }

    [Fact]
    public void GetUniqueTiles_PreservesSubTextureReference()
    {
        // Arrange
        var tileSet = new TileSet
        {
            TexturePath = "/nonexistent/path/to/texture.png",
            Columns = 8,
            Rows = 8
        };
        var mockTexture = Substitute.For<Texture2D>();
        
        var min = new Vector2(0f, 0f);
        var max = new Vector2(0.25f, 0.25f);
        var subTexture = new SubTexture2D(mockTexture, min, max);
        
        tileSet.Tiles.Add(new Tile { Id = 0, Name = "Tile_0", SubTexture = subTexture });

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles[0].SubTexture.ShouldBe(subTexture);
    }
    
    [Fact]
    public void GetUniqueTiles_WithInvalidTexturePath_ShouldReturnAllTilesAsUnique()
    {
        // Arrange
        var tileSet = new TileSet
        {
            TexturePath = "/nonexistent/path/to/texture.png",
            Columns = 8,
            Rows = 8
        };
        var mockTexture = Substitute.For<Texture2D>();
        
        for (var i = 0; i < 3; i++)
        {
            var min = new Vector2(i * 0.25f, 0f);
            var max = new Vector2((i + 1) * 0.25f, 0.25f);
            tileSet.Tiles.Add(new Tile
            {
                Id = i,
                Name = $"Tile_{i}",
                SubTexture = new SubTexture2D(mockTexture, min, max)
            });
        }

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert - With invalid path, fallback returns all tiles as unique
        uniqueTiles.Count.ShouldBe(3);
    }
}
