using Engine.Animation;
using Engine.Core;
using Engine.Renderer.Textures;
using NSubstitute;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for AnimationAssetManager, focusing on caching and reference counting.
/// Note: These tests focus on the manager's internal logic without actually loading files.
/// </summary>
public class AnimationAssetManagerTests
{
    private class MockAssetsManager : IAssetsManager
    {
        public string AssetsPath => Path.Combine(Environment.CurrentDirectory, "assets");
        public void SetAssetsPath(string path) { }
    }
    
    private readonly ITextureFactory _textureFactory;

    public AnimationAssetManagerTests()
    {
        _textureFactory = Substitute.For<ITextureFactory>();
    }

    private AnimationAssetManager CreateManager() => new(new MockAssetsManager(), _textureFactory);

    [Fact]
    public void IsCached_WithNonLoadedAsset_ReturnsFalse()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act
        var isCached = manager.IsCached("nonexistent.json");

        // Assert
        Assert.False(isCached);
    }

    [Fact]
    public void GetReferenceCount_WithNonLoadedAsset_ReturnsZero()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act
        var refCount = manager.GetReferenceCount("nonexistent.json");

        // Assert
        Assert.Equal(0, refCount);
    }

    [Fact]
    public void GetCachedAssetCount_WithNoAssets_ReturnsZero()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act
        var count = manager.GetCachedAssetCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetTotalMemoryUsage_WithNoAssets_ReturnsZero()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act
        var memory = manager.GetTotalMemoryUsage();

        // Assert
        Assert.Equal(0, memory);
    }

    [Fact]
    public void ClearAllAssets_RemovesAllCachedAssets()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.ClearAllAssets();
        var count = manager.GetCachedAssetCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void UnloadAsset_WithNonExistentAsset_DoesNotThrow()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act & Assert - Should not throw
        manager.UnloadAsset("nonexistent.json");
    }

    [Fact]
    public void ClearUnusedAssets_WithNoAssets_DoesNotThrow()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act & Assert - Should not throw
        manager.ClearUnusedAssets();
    }
}