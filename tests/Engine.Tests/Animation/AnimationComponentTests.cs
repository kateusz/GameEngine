using System.Numerics;
using Engine.Animation;
using Engine.Scene.Components;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for AnimationComponent class, focusing on API methods and state management.
/// </summary>
public class AnimationComponentTests
{
    private static AnimationAsset CreateTestAsset()
    {
        var idleFrames = new[]
        {
            new AnimationFrame { Rect = new Rectangle(0, 0, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(32, 0, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] }
        };

        var walkFrames = new[]
        {
            new AnimationFrame { Rect = new Rectangle(0, 32, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(32, 32, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(64, 32, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(96, 32, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] }
        };

        var jumpFrames = new[]
        {
            new AnimationFrame { Rect = new Rectangle(0, 64, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(32, 64, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(64, 64, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] }
        };

        return new AnimationAsset
        {
            Id = "test_character",
            AtlasPath = "test.png",
            CellSize = new Vector2(32, 32),
            Clips = new Dictionary<string, AnimationClip>
            {
                ["idle"] = new AnimationClip { Name = "idle", Fps = 8.0f, Loop = true, Frames = idleFrames },
                ["walk"] = new AnimationClip { Name = "walk", Fps = 12.0f, Loop = true, Frames = walkFrames },
                ["jump"] = new AnimationClip { Name = "jump", Fps = 24.0f, Loop = false, Frames = jumpFrames }
            }
        };
    }

    [Fact]
    public void AnimationComponent_DefaultProperties_HaveCorrectValues()
    {
        // Arrange & Act
        var component = new AnimationComponent();

        // Assert
        Assert.Null(component.Asset);
        Assert.Null(component.AssetPath);
        Assert.Equal(string.Empty, component.CurrentClipName);
        Assert.False(component.IsPlaying);
        Assert.True(component.Loop);
        Assert.Equal(1.0f, component.PlaybackSpeed);
        Assert.Equal(0, component.CurrentFrameIndex);
        Assert.Equal(0.0f, component.FrameTimer);
        Assert.Equal(-1, component.PreviousFrameIndex);
        Assert.False(component.ShowDebugInfo);
    }

    // NOTE: The following tests are commented out because AnimationComponent is now a pure data component.
    // Animation logic has been moved to AnimationSystem. These tests would need to be rewritten
    // to test the AnimationSystem instead.

    /*
    [Fact]
    public void Play_WithValidClip_StartsPlayback()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        component.Play("idle");

        // Assert
        Assert.Equal("idle", component.CurrentClipName);
        Assert.True(component.IsPlaying);
        Assert.Equal(0, component.CurrentFrameIndex);
        Assert.Equal(0.0f, component.FrameTimer);
        Assert.Equal(-1, component.PreviousFrameIndex);
        Assert.True(component.Loop); // Should inherit from clip
    }

    [Fact]
    public void Play_WithNonLoopingClip_SetsLoopToFalse()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        component.Play("jump");

        // Assert
        Assert.Equal("jump", component.CurrentClipName);
        Assert.True(component.IsPlaying);
        Assert.False(component.Loop); // Jump animation doesn't loop
    }

    [Fact]
    public void Play_WithInvalidClip_DoesNotStartPlayback()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        component.Play("nonexistent");

        // Assert
        Assert.Equal(string.Empty, component.CurrentClipName);
        Assert.False(component.IsPlaying);
    }

    [Fact]
    public void Play_WithNullAsset_DoesNotThrow()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = null
        };

        // Act & Assert - Should not throw
        component.Play("anything");
        Assert.False(component.IsPlaying);
    }

    [Fact]
    public void Play_WithSameClip_WithoutForceRestart_DoesNotRestart()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };
        component.Play("walk");
        component.CurrentFrameIndex = 2;
        component.FrameTimer = 0.05f;

        // Act
        component.Play("walk", forceRestart: false);

        // Assert - Should maintain current playback position
        Assert.Equal(2, component.CurrentFrameIndex);
        Assert.Equal(0.05f, component.FrameTimer);
    }

    [Fact]
    public void Play_WithSameClip_WithForceRestart_RestartsPlayback()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };
        component.Play("walk");
        component.CurrentFrameIndex = 2;
        component.FrameTimer = 0.05f;

        // Act
        component.Play("walk", forceRestart: true);

        // Assert - Should reset playback position
        Assert.Equal(0, component.CurrentFrameIndex);
        Assert.Equal(0.0f, component.FrameTimer);
        Assert.Equal(-1, component.PreviousFrameIndex);
    }

    [Fact]
    public void Stop_ResetsPlaybackState()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };
        component.Play("walk");
        component.CurrentFrameIndex = 2;
        component.FrameTimer = 0.05f;

        // Act
        component.Stop();

        // Assert
        Assert.False(component.IsPlaying);
        Assert.Equal(0, component.CurrentFrameIndex);
        Assert.Equal(0.0f, component.FrameTimer);
        Assert.Equal(-1, component.PreviousFrameIndex);
    }

    [Fact]
    public void Pause_StopsPlaybackWithoutResetting()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };
        component.Play("walk");
        component.CurrentFrameIndex = 2;
        component.FrameTimer = 0.05f;

        // Act
        component.Pause();

        // Assert
        Assert.False(component.IsPlaying);
        Assert.Equal(2, component.CurrentFrameIndex); // Position preserved
        Assert.Equal(0.05f, component.FrameTimer); // Timer preserved
    }

    [Fact]
    public void Resume_RestoresPlayback()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };
        component.Play("walk");
        component.Pause();

        // Act
        component.Resume();

        // Assert
        Assert.True(component.IsPlaying);
    }

    [Fact]
    public void Resume_WithNoAsset_DoesNotStartPlayback()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = null,
            CurrentClipName = "walk"
        };

        // Act
        component.Resume();

        // Assert
        Assert.False(component.IsPlaying);
    }

    [Fact]
    public void Resume_WithInvalidClip_DoesNotStartPlayback()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "nonexistent"
        };

        // Act
        component.Resume();

        // Assert
        Assert.False(component.IsPlaying);
    }

    [Fact]
    public void SetSpeed_SetsPlaybackSpeed()
    {
        // Arrange
        var component = new AnimationComponent();

        // Act
        component.SetSpeed(2.0f);

        // Assert
        Assert.Equal(2.0f, component.PlaybackSpeed);
    }

    [Fact]
    public void SetSpeed_WithNegativeValue_ClampsToZero()
    {
        // Arrange
        var component = new AnimationComponent();

        // Act
        component.SetSpeed(-1.0f);

        // Assert
        Assert.Equal(0.0f, component.PlaybackSpeed);
    }

    [Fact]
    public void SetFrame_WithValidIndex_SetsFrameIndex()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk"
        };

        // Act
        component.SetFrame(2);

        // Assert
        Assert.Equal(2, component.CurrentFrameIndex);
        Assert.Equal(0.0f, component.FrameTimer);
    }

    [Fact]
    public void SetFrame_WithTooHighIndex_ClampsToLastFrame()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk" // 4 frames
        };

        // Act
        component.SetFrame(100);

        // Assert
        Assert.Equal(3, component.CurrentFrameIndex); // Last frame index
    }

    [Fact]
    public void SetFrame_WithNegativeIndex_ClampsToZero()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk"
        };

        // Act
        component.SetFrame(-5);

        // Assert
        Assert.Equal(0, component.CurrentFrameIndex);
    }

    [Fact]
    public void SetNormalizedTime_WithValidTime_SetsCorrectFrame()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk" // 4 frames
        };

        // Act
        component.SetNormalizedTime(0.5f); // Middle

        // Assert
        Assert.Equal(1, component.CurrentFrameIndex); // Frame 1 (0.5 * 3 = 1.5, int cast = 1)
    }

    [Fact]
    public void SetNormalizedTime_WithZero_SetsFirstFrame()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk"
        };

        // Act
        component.SetNormalizedTime(0.0f);

        // Assert
        Assert.Equal(0, component.CurrentFrameIndex);
    }

    [Fact]
    public void SetNormalizedTime_WithOne_SetsLastFrame()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk" // 4 frames (indices 0-3)
        };

        // Act
        component.SetNormalizedTime(1.0f);

        // Assert
        Assert.Equal(3, component.CurrentFrameIndex);
    }

    [Fact]
    public void SetNormalizedTime_WithOutOfRangeValue_Clamps()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk"
        };

        // Act & Assert - Above range
        component.SetNormalizedTime(2.0f);
        Assert.Equal(3, component.CurrentFrameIndex); // Last frame

        // Act & Assert - Below range
        component.SetNormalizedTime(-1.0f);
        Assert.Equal(0, component.CurrentFrameIndex); // First frame
    }

    [Fact]
    public void GetCurrentFrame_ReturnsCurrentFrameIndex()
    {
        // Arrange
        var component = new AnimationComponent
        {
            CurrentFrameIndex = 5
        };

        // Act
        var frame = component.GetCurrentFrame();

        // Assert
        Assert.Equal(5, frame);
    }

    [Fact]
    public void GetFrameCount_WithValidClip_ReturnsFrameCount()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk"
        };

        // Act
        var count = component.GetFrameCount();

        // Assert
        Assert.Equal(4, count);
    }

    [Fact]
    public void GetFrameCount_WithNoAsset_ReturnsZero()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = null,
            CurrentClipName = "walk"
        };

        // Act
        var count = component.GetFrameCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetFrameCount_WithInvalidClip_ReturnsZero()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "nonexistent"
        };

        // Act
        var count = component.GetFrameCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetNormalizedTime_ReturnsCorrectValue()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset(),
            CurrentClipName = "walk", // 4 frames
            CurrentFrameIndex = 1
        };

        // Act
        var normalizedTime = component.GetNormalizedTime();

        // Assert
        Assert.Equal(1.0f / 3.0f, normalizedTime, precision: 5); // Frame 1 out of 3 intervals
    }

    [Fact]
    public void GetNormalizedTime_WithZeroFrames_ReturnsZero()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = null
        };

        // Act
        var normalizedTime = component.GetNormalizedTime();

        // Assert
        Assert.Equal(0.0f, normalizedTime);
    }

    [Fact]
    public void GetCurrentClipName_ReturnsClipName()
    {
        // Arrange
        var component = new AnimationComponent
        {
            CurrentClipName = "walk"
        };

        // Act
        var clipName = component.GetCurrentClipName();

        // Assert
        Assert.Equal("walk", clipName);
    }

    [Fact]
    public void HasClip_WithValidClip_ReturnsTrue()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        var hasIdle = component.HasClip("idle");
        var hasWalk = component.HasClip("walk");
        var hasJump = component.HasClip("jump");

        // Assert
        Assert.True(hasIdle);
        Assert.True(hasWalk);
        Assert.True(hasJump);
    }

    [Fact]
    public void HasClip_WithInvalidClip_ReturnsFalse()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        var hasRun = component.HasClip("run");

        // Assert
        Assert.False(hasRun);
    }

    [Fact]
    public void HasClip_WithNullAsset_ReturnsFalse()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = null
        };

        // Act
        var hasAny = component.HasClip("anything");

        // Assert
        Assert.False(hasAny);
    }

    [Fact]
    public void GetAvailableClips_ReturnsAllClipNames()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        var clips = component.GetAvailableClips();

        // Assert
        Assert.Equal(3, clips.Length);
        Assert.Contains("idle", clips);
        Assert.Contains("walk", clips);
        Assert.Contains("jump", clips);
    }

    [Fact]
    public void GetAvailableClips_WithNullAsset_ReturnsEmptyArray()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = null
        };

        // Act
        var clips = component.GetAvailableClips();

        // Assert
        Assert.Empty(clips);
    }

    [Fact]
    public void GetClipDuration_WithValidClip_ReturnsDuration()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        var idleDuration = component.GetClipDuration("idle"); // 2 frames at 8 FPS
        var walkDuration = component.GetClipDuration("walk"); // 4 frames at 12 FPS

        // Assert
        Assert.Equal(0.25f, idleDuration, precision: 5); // 2/8 = 0.25
        Assert.Equal(0.333333f, walkDuration, precision: 5); // 4/12 = 0.333...
    }

    [Fact]
    public void GetClipDuration_WithInvalidClip_ReturnsZero()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = CreateTestAsset()
        };

        // Act
        var duration = component.GetClipDuration("nonexistent");

        // Assert
        Assert.Equal(0.0f, duration);
    }

    [Fact]
    public void GetClipDuration_WithNullAsset_ReturnsZero()
    {
        // Arrange
        var component = new AnimationComponent
        {
            Asset = null
        };

        // Act
        var duration = component.GetClipDuration("anything");

        // Assert
        Assert.Equal(0.0f, duration);
    }
    */

    [Fact]
    public void Clone_CopiesSerializableProperties()
    {
        // Arrange
        var component = new AnimationComponent
        {
            AssetPath = "Animations/player.json",
            CurrentClipName = "walk",
            IsPlaying = true,
            Loop = false,
            PlaybackSpeed = 2.0f,
            ShowDebugInfo = true,
            CurrentFrameIndex = 5,
            FrameTimer = 0.123f
        };

        // Act
        var clone = component.Clone() as AnimationComponent;

        // Assert
        Assert.NotNull(clone);
        Assert.Equal("Animations/player.json", clone.AssetPath);
        Assert.Equal("walk", clone.CurrentClipName);
        Assert.True(clone.IsPlaying);
        Assert.False(clone.Loop);
        Assert.Equal(2.0f, clone.PlaybackSpeed);
        Assert.True(clone.ShowDebugInfo);

        // Runtime state should not be cloned
        Assert.Equal(0, clone.CurrentFrameIndex);
        Assert.Equal(0.0f, clone.FrameTimer);
    }

    [Fact]
    public void Clone_DoesNotCopyRuntimeState()
    {
        // Arrange
        var component = new AnimationComponent
        {
            CurrentFrameIndex = 10,
            FrameTimer = 0.5f,
            PreviousFrameIndex = 9
        };

        // Act
        var clone = component.Clone() as AnimationComponent;

        // Assert
        Assert.NotNull(clone);
        Assert.Equal(0, clone.CurrentFrameIndex);
        Assert.Equal(0.0f, clone.FrameTimer);
        Assert.Equal(-1, clone.PreviousFrameIndex);
    }
}
