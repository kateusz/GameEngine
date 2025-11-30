using System.Numerics;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Components;

public class TileSetTests
{
    [Fact]
    public void GetUniqueTiles_WithEmptyTiles_ShouldReturnEmptyList()
    {
        // Arrange
        var tileSet = new TileSet();

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles.ShouldBeEmpty();
    }

    [Fact]
    public void GetUniqueTiles_WithAllUniqueTiles_ShouldReturnAllTiles()
    {
        // Arrange
        var tileSet = new TileSet();
        var mockTexture = Substitute.For<Texture2D>();
        
        // Create 4 tiles with different UV coordinates
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

        // Assert
        uniqueTiles.Count.ShouldBe(4);
        for (var i = 0; i < 4; i++)
        {
            uniqueTiles[i].PrimaryTileId.ShouldBe(i);
            uniqueTiles[i].AllTileIds.Count.ShouldBe(1);
            uniqueTiles[i].AllTileIds.ShouldContain(i);
        }
    }

    [Fact]
    public void GetUniqueTiles_WithDuplicateTiles_ShouldDeduplicateByUVCoords()
    {
        // Arrange
        var tileSet = new TileSet();
        var mockTexture = Substitute.For<Texture2D>();
        
        var min = new Vector2(0f, 0f);
        var max = new Vector2(0.25f, 0.25f);
        
        // Create 3 tiles with the same UV coordinates
        for (var i = 0; i < 3; i++)
        {
            tileSet.Tiles.Add(new Tile
            {
                Id = i,
                Name = $"Tile_{i}",
                SubTexture = new SubTexture2D(mockTexture, min, max)
            });
        }

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles.Count.ShouldBe(1);
        uniqueTiles[0].PrimaryTileId.ShouldBe(0);
        uniqueTiles[0].AllTileIds.Count.ShouldBe(3);
        uniqueTiles[0].AllTileIds.ShouldContain(0);
        uniqueTiles[0].AllTileIds.ShouldContain(1);
        uniqueTiles[0].AllTileIds.ShouldContain(2);
    }

    [Fact]
    public void GetUniqueTiles_WithMixedTiles_ShouldGroupDuplicatesCorrectly()
    {
        // Arrange
        var tileSet = new TileSet();
        var mockTexture = Substitute.For<Texture2D>();
        
        // Create 5 tiles: tiles 0, 2, 4 have same UVs; tiles 1, 3 have same UVs (but different from first group)
        var uvA = (min: new Vector2(0f, 0f), max: new Vector2(0.25f, 0.25f));
        var uvB = (min: new Vector2(0.25f, 0f), max: new Vector2(0.5f, 0.25f));
        
        tileSet.Tiles.Add(new Tile { Id = 0, SubTexture = new SubTexture2D(mockTexture, uvA.min, uvA.max) });
        tileSet.Tiles.Add(new Tile { Id = 1, SubTexture = new SubTexture2D(mockTexture, uvB.min, uvB.max) });
        tileSet.Tiles.Add(new Tile { Id = 2, SubTexture = new SubTexture2D(mockTexture, uvA.min, uvA.max) });
        tileSet.Tiles.Add(new Tile { Id = 3, SubTexture = new SubTexture2D(mockTexture, uvB.min, uvB.max) });
        tileSet.Tiles.Add(new Tile { Id = 4, SubTexture = new SubTexture2D(mockTexture, uvA.min, uvA.max) });

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles.Count.ShouldBe(2);
        
        // First unique tile should have IDs 0, 2, 4 (uvA group)
        var groupA = uniqueTiles.First(u => u.PrimaryTileId == 0);
        groupA.AllTileIds.Count.ShouldBe(3);
        groupA.AllTileIds.ShouldContain(0);
        groupA.AllTileIds.ShouldContain(2);
        groupA.AllTileIds.ShouldContain(4);
        
        // Second unique tile should have IDs 1, 3 (uvB group)
        var groupB = uniqueTiles.First(u => u.PrimaryTileId == 1);
        groupB.AllTileIds.Count.ShouldBe(2);
        groupB.AllTileIds.ShouldContain(1);
        groupB.AllTileIds.ShouldContain(3);
    }

    [Fact]
    public void GetUniqueTiles_WithNullSubTextures_ShouldSkipNullTiles()
    {
        // Arrange
        var tileSet = new TileSet();
        var mockTexture = Substitute.For<Texture2D>();
        
        var min = new Vector2(0f, 0f);
        var max = new Vector2(0.25f, 0.25f);
        
        tileSet.Tiles.Add(new Tile { Id = 0, SubTexture = new SubTexture2D(mockTexture, min, max) });
        tileSet.Tiles.Add(new Tile { Id = 1, SubTexture = null });
        tileSet.Tiles.Add(new Tile { Id = 2, SubTexture = new SubTexture2D(mockTexture, min, max) });

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles.Count.ShouldBe(1);
        uniqueTiles[0].AllTileIds.Count.ShouldBe(2);
        uniqueTiles[0].AllTileIds.ShouldContain(0);
        uniqueTiles[0].AllTileIds.ShouldContain(2);
    }

    [Fact]
    public void GetUniqueTiles_WithSlightlyDifferentUVs_ShouldNotBeDuplicates()
    {
        // Arrange
        var tileSet = new TileSet();
        var mockTexture = Substitute.For<Texture2D>();
        
        // Create tiles with UV coordinates that differ by more than epsilon (0.0001)
        var min1 = new Vector2(0f, 0f);
        var max1 = new Vector2(0.25f, 0.25f);
        
        var min2 = new Vector2(0.001f, 0f); // Differs by 0.001 > 0.0001
        var max2 = new Vector2(0.251f, 0.25f);
        
        tileSet.Tiles.Add(new Tile { Id = 0, SubTexture = new SubTexture2D(mockTexture, min1, max1) });
        tileSet.Tiles.Add(new Tile { Id = 1, SubTexture = new SubTexture2D(mockTexture, min2, max2) });

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles.Count.ShouldBe(2);
    }

    [Fact]
    public void GetUniqueTiles_PreservesSubTextureReference()
    {
        // Arrange
        var tileSet = new TileSet();
        var mockTexture = Substitute.For<Texture2D>();
        
        var min = new Vector2(0f, 0f);
        var max = new Vector2(0.25f, 0.25f);
        var subTexture = new SubTexture2D(mockTexture, min, max);
        
        tileSet.Tiles.Add(new Tile { Id = 0, SubTexture = subTexture });

        // Act
        var uniqueTiles = tileSet.GetUniqueTiles();

        // Assert
        uniqueTiles[0].SubTexture.ShouldBe(subTexture);
    }
}
