using Engine.Animation;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for AnimationAssetManager, focusing on caching and reference counting.
/// Note: These tests focus on the manager's internal logic without actually loading files.
/// Full integration tests with real files would require test fixtures.
/// </summary>
public class AnimationAssetManagerTests
{
    private class MockAssetsManager : IAssetsManager
    {
        public string AssetsPath => Path.Combine(Environment.CurrentDirectory, "assets");
        public void SetAssetsPath(string path) { }
    }

    private static AnimationAssetManager CreateManager() => new(new MockAssetsManager());

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

    private static AnimationAssetManager CreateManager() => new(new MockAssetsManager());

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
