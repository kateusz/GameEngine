using System.Numerics;
using Engine.Animation;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for AnimationAsset class, focusing on clip management and disposal.
/// </summary>
public class AnimationAssetTests
{
    /// <summary>
    /// Helper method to create and initialize an AnimationAsset for testing.
    /// </summary>
    private static AnimationAsset CreateTestAsset(string id, params AnimationClip[] clips)
    {
        var asset = new AnimationAsset
        {
            Id = id,
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = clips
        };
        asset.InitializeClipLookup();
        return asset;
    }

    [Fact]
    public void HasClip_WithExistingClip_ReturnsTrue()
    {
        // Arrange
        var asset = CreateTestAsset("test",
            new AnimationClip { Name = "idle", Fps = 10.0f, Loop = true, Frames = [], Duration = 0, FrameDuration = 0.1f },
            new AnimationClip { Name = "walk", Fps = 10.0f, Loop = true, Frames = [], Duration = 0, FrameDuration = 0.1f }
        );

        // Act
        var hasIdle = asset.HasClip("idle");
        var hasWalk = asset.HasClip("walk");

        // Assert
        Assert.True(hasIdle);
        Assert.True(hasWalk);
    }

    [Fact]
    public void HasClip_WithNonExistingClip_ReturnsFalse()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["idle"] = new AnimationClip { Name = "idle", Fps = 10.0f, Loop = true, Frames = [] }
            }
        };

        // Act
        var hasRun = asset.HasClip("run");

        // Assert
        Assert.False(hasRun);
    }

    [Fact]
    public void HasClip_WithEmptyClips_ReturnsFalse()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>()
        };

        // Act
        var hasAny = asset.HasClip("anything");

        // Assert
        Assert.False(hasAny);
    }

    [Fact]
    public void GetClip_WithExistingClip_ReturnsClip()
    {
        // Arrange
        var idleClip = new AnimationClip { Name = "idle", Fps = 10.0f, Loop = true, Frames = [] };
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["idle"] = idleClip
            }
        };

        // Act
        var result = asset.GetClip("idle");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("idle", result.Name);
        Assert.Equal(10.0f, result.Fps);
    }

    [Fact]
    public void GetClip_WithNonExistingClip_ReturnsNull()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["idle"] = new AnimationClip { Name = "idle", Fps = 10.0f, Loop = true, Frames = [] }
            }
        };

        // Act
        var result = asset.GetClip("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetClip_WithEmptyClips_ReturnsNull()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>()
        };

        // Act
        var result = asset.GetClip("anything");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AnimationAsset_DefaultProperties_HaveCorrectValues()
    {
        // Arrange & Act
        var asset = new AnimationAsset
        {
            Id = "",
            AtlasPath = "",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>()
        };

        // Assert
        Assert.Equal(string.Empty, asset.Id);
        Assert.Equal(string.Empty, asset.AtlasPath);
        Assert.Null(asset.Atlas);
        Assert.Equal(32, asset.CellSize.X);
        Assert.Equal(32, asset.CellSize.Y);
        Assert.Empty(asset.Clips);
    }

    [Fact]
    public void AnimationAsset_CustomProperties_CanBeSet()
    {
        // Arrange & Act
        var asset = new AnimationAsset
        {
            Id = "player_animations",
            AtlasPath = "Assets/Characters/player.png",
            CellSize = new Vector2(64, 64),
            Clips = new Dictionary<string, AnimationClip>()
        };

        // Assert
        Assert.Equal("player_animations", asset.Id);
        Assert.Equal("Assets/Characters/player.png", asset.AtlasPath);
        Assert.Equal(64, asset.CellSize.X);
        Assert.Equal(64, asset.CellSize.Y);
    }

    [Fact]
    public void AnimationAsset_WithMultipleClips_ManagesAllClips()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["idle"] = new AnimationClip { Name = "idle", Fps = 8.0f, Loop = true, Frames = [] },
                ["walk"] = new AnimationClip { Name = "walk", Fps = 12.0f, Loop = true, Frames = [] },
                ["jump"] = new AnimationClip { Name = "jump", Fps = 24.0f, Loop = false, Frames = [] },
                ["attack"] = new AnimationClip { Name = "attack", Fps = 30.0f, Loop = false, Frames = [] }
            }
        };

        // Act & Assert
        Assert.Equal(4, asset.Clips.Count);
        Assert.True(asset.HasClip("idle"));
        Assert.True(asset.HasClip("walk"));
        Assert.True(asset.HasClip("jump"));
        Assert.True(asset.HasClip("attack"));

        var idleClip = asset.GetClip("idle");
        Assert.NotNull(idleClip);
        Assert.Equal(8.0f, idleClip.Fps);
        Assert.True(idleClip.Loop);

        var jumpClip = asset.GetClip("jump");
        Assert.NotNull(jumpClip);
        Assert.Equal(24.0f, jumpClip.Fps);
        Assert.False(jumpClip.Loop);
    }

    [Fact]
    public void AnimationAsset_ClipDictionary_CanBeModified()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>()
        };

        // Act - Add clips
        asset.Clips.Add("idle", new AnimationClip { Name = "idle", Fps = 10.0f, Loop = true, Frames = [] });
        asset.Clips.Add("walk", new AnimationClip { Name = "walk", Fps = 10.0f, Loop = true, Frames = [] });

        // Assert - Clips added
        Assert.Equal(2, asset.Clips.Count);
        Assert.True(asset.HasClip("idle"));
        Assert.True(asset.HasClip("walk"));

        // Act - Remove a clip
        asset.Clips.Remove("idle");

        // Assert - Clip removed
        Assert.Single(asset.Clips);
        Assert.False(asset.HasClip("idle"));
        Assert.True(asset.HasClip("walk"));
    }

    [Fact]
    public void Dispose_WithNullAtlas_DoesNotThrow()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>()
        };

        // Act & Assert - Should not throw
        asset.Dispose();
        Assert.Null(asset.Atlas);
    }

    [Fact]
    public void Dispose_SetsAtlasToNull()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>()
        };
        // We can't create a real texture without OpenGL context

        // Act
        asset.Dispose();

        // Assert
        Assert.Null(asset.Atlas);
    }

    [Fact]
    public void HasClip_IsCaseSensitive()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["Idle"] = new AnimationClip { Name = "Idle", Fps = 10.0f, Loop = true, Frames = [] }
            }
        };

        // Act
        var hasExactCase = asset.HasClip("Idle");
        var hasLowerCase = asset.HasClip("idle");
        var hasUpperCase = asset.HasClip("IDLE");

        // Assert
        Assert.True(hasExactCase);
        Assert.False(hasLowerCase);
        Assert.False(hasUpperCase);
    }

    [Fact]
    public void GetClip_IsCaseSensitive()
    {
        // Arrange
        var clip = new AnimationClip { Name = "Walk", Fps = 10.0f, Loop = true, Frames = [] };
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["Walk"] = clip
            }
        };

        // Act
        var exactMatch = asset.GetClip("Walk");
        var lowerCase = asset.GetClip("walk");

        // Assert
        Assert.NotNull(exactMatch);
        Assert.Equal("Walk", exactMatch.Name);
        Assert.Null(lowerCase);
    }

    [Fact]
    public void AnimationAsset_WithComplexClipData_StoresCorrectly()
    {
        // Arrange
        var frames = new[]
        {
            new AnimationFrame 
            { 
                Rect = new Rectangle(0, 0, 32, 32), 
                Pivot = new Vector2(0.5f, 0.0f),
                Scale = Vector2.One,
                TexCoords = new Vector2[4],
                Events = new[] { "step" } 
            },
            new AnimationFrame 
            { 
                Rect = new Rectangle(32, 0, 32, 32), 
                Pivot = new Vector2(0.5f, 0.0f),
                Scale = Vector2.One,
                TexCoords = new Vector2[4],
                Events = new[] { "step" } 
            },
            new AnimationFrame 
            { 
                Rect = new Rectangle(64, 0, 32, 32), 
                Pivot = new Vector2(0.5f, 0.0f),
                Scale = Vector2.One,
                TexCoords = new Vector2[4],
                Events = Array.Empty<string>() 
            }
        };

        var clip = new AnimationClip
        {
            Name = "walk",
            Fps = 12.0f,
            Loop = true,
            Frames = frames
        };

        var asset = new AnimationAsset
        {
            Id = "character",
            AtlasPath = "textures/character.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["walk"] = clip
            }
        };

        // Act
        var retrievedClip = asset.GetClip("walk");

        // Assert
        Assert.NotNull(retrievedClip);
        Assert.Equal("walk", retrievedClip.Name);
        Assert.Equal(12.0f, retrievedClip.Fps);
        Assert.True(retrievedClip.Loop);
        Assert.Equal(3, retrievedClip.Frames.Length);
        Assert.Single(retrievedClip.Frames[0].Events);
        Assert.Equal("step", retrievedClip.Frames[0].Events[0]);
    }

    [Fact]
    public void AnimationAsset_EmptyClipName_CanBeStored()
    {
        // Arrange
        var asset = new AnimationAsset
        {
            Id = "test",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                [""] = new AnimationClip { Name = "", Fps = 10.0f, Loop = true, Frames = [] }
            }
        };

        // Act
        var hasEmpty = asset.HasClip("");
        var clip = asset.GetClip("");

        // Assert
        Assert.True(hasEmpty);
        Assert.NotNull(clip);
        Assert.Equal(string.Empty, clip.Name);
    }
}
