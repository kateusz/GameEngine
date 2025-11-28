using Engine.Animation;
using Engine.Renderer.Textures;
using NSubstitute;

namespace Engine.Tests.Animation;

/// <summary>
/// Test helper class to verify AnimationAssetManager behavior without actual file I/O.
/// These tests verify the manager's internal state management logic.
/// </summary>
public class AnimationAssetManagerStateTests
{
    private class MockAssetsManager : IAssetsManager
    {
        public string AssetsPath => Path.Combine(Environment.CurrentDirectory, "assets");
        public void SetAssetsPath(string path) { }
    }
    
    private readonly ITextureFactory _textureFactory;

    public AnimationAssetManagerStateTests()
    {
        _textureFactory = Substitute.For<ITextureFactory>();
    }

    private AnimationAssetManager CreateManager() => new(new MockAssetsManager(), _textureFactory);

    [Fact]
    public void Manager_StartsWithEmptyCache()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Assert
        Assert.Equal(0, manager.GetCachedAssetCount());
        Assert.False(manager.IsCached("any_path"));
        Assert.Equal(0, manager.GetReferenceCount("any_path"));
    }

    [Fact]
    public void ClearAllAssets_IsIdempotent()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.ClearAllAssets();
        manager.ClearAllAssets();
        manager.ClearAllAssets();

        // Assert
        Assert.Equal(0, manager.GetCachedAssetCount());
    }

    [Fact]
    public void UnloadAsset_OnNonExistentAsset_IsIdempotent()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act
        manager.UnloadAsset("fake.json");
        manager.UnloadAsset("fake.json");
        manager.UnloadAsset("fake.json");

        // Assert - Should not throw or cause issues
        Assert.Equal(0, manager.GetCachedAssetCount());
    }

    [Fact]
    public void ClearUnusedAssets_WithNoAssets_IsIdempotent()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act
        manager.ClearUnusedAssets();
        manager.ClearUnusedAssets();

        // Assert
        Assert.Equal(0, manager.GetCachedAssetCount());
    }

    [Fact]
    public void GetTotalMemoryUsage_WithEmptyCache_ReturnsZero()
    {
        // Arrange
        var manager = CreateManager();
        manager.ClearAllAssets();

        // Act
        var initialMemory = manager.GetTotalMemoryUsage();
        manager.ClearAllAssets();
        var afterClearMemory = manager.GetTotalMemoryUsage();

        // Assert
        Assert.Equal(0, initialMemory);
        Assert.Equal(0, afterClearMemory);
    }
}