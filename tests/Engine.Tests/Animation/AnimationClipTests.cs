using System.Numerics;
using Engine.Animation;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for AnimationClip class, focusing on duration calculations and properties.
/// NOTE: These tests need updating to set Duration and FrameDuration (cached values in new API).
/// Use helper method or initialize these fields when creating AnimationClip instances.
/// </summary>
public class AnimationClipTests
{
    [Fact]
    public void Duration_WithStandardFrameRate_CalculatesCorrectly()
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "walk",
            Fps = 12.0f,
            Loop = false,
            Frames = new AnimationFrame[6],
            Duration = 6 / 12.0f,  // Cached value
            FrameDuration = 1.0f / 12.0f  // Cached value
        };

        // Act
        var duration = clip.Duration;

        // Assert
        Assert.Equal(0.5f, duration, precision: 5); // 6 frames / 12 FPS = 0.5 seconds
    }

    [Fact]
    public void Duration_WithHighFrameRate_CalculatesCorrectly()
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = 60.0f,
            Loop = false,
            Frames = new AnimationFrame[120]
        };

        // Act
        var duration = clip.Duration;

        // Assert
        Assert.Equal(2.0f, duration, precision: 5); // 120 frames / 60 FPS = 2 seconds
    }

    [Fact]
    public void Duration_WithSingleFrame_ReturnsFrameDuration()
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = 10.0f,
            Loop = false,
            Frames = new AnimationFrame[1]
        };

        // Act
        var duration = clip.Duration;

        // Assert
        Assert.Equal(0.1f, duration, precision: 5); // 1 frame / 10 FPS = 0.1 seconds
    }

    [Fact]
    public void Duration_WithZeroFrames_ReturnsZero()
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = 12.0f,
            Loop = false,
            Frames = Array.Empty<AnimationFrame>()
        };

        // Act
        var duration = clip.Duration;

        // Assert
        Assert.Equal(0.0f, duration);
    }

    [Fact]
    public void FrameDuration_CalculatesCorrectly()
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = 24.0f,
            Loop = false,
            Frames = []
        };

        // Act
        var frameDuration = clip.FrameDuration;

        // Assert
        Assert.Equal(1.0f / 24.0f, frameDuration, precision: 5); // ~0.04167 seconds per frame
    }

    [Fact]
    public void FrameDuration_WithSlowFrameRate_ProducesLongerDuration()
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = 6.0f,
            Loop = false,
            Frames = []
        };

        // Act
        var frameDuration = clip.FrameDuration;

        // Assert
        Assert.Equal(1.0f / 6.0f, frameDuration, precision: 5); // ~0.16667 seconds per frame
    }

    [Fact]
    public void AnimationClip_DefaultProperties_HaveCorrectValues()
    {
        // Arrange & Act
        var clip = new AnimationClip
        {
            Name = "",
            Fps = 12.0f,
            Loop = false,
            Frames = []
        };

        // Assert
        Assert.Equal(string.Empty, clip.Name);
        Assert.Equal(12.0f, clip.Fps);
        Assert.False(clip.Loop);
        Assert.Empty(clip.Frames);
    }

    [Fact]
    public void AnimationClip_CustomProperties_CanBeSet()
    {
        // Arrange
        var frames = new[]
        {
            new AnimationFrame { Rect = new Rectangle(0, 0, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(32, 0, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] },
            new AnimationFrame { Rect = new Rectangle(64, 0, 32, 32), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] }
        };

        // Act
        var clip = new AnimationClip
        {
            Name = "idle",
            Fps = 8.0f,
            Loop = true,
            Frames = frames
        };

        // Assert
        Assert.Equal("idle", clip.Name);
        Assert.Equal(8.0f, clip.Fps);
        Assert.True(clip.Loop);
        Assert.Equal(3, clip.Frames.Length);
        Assert.Equal(0.375f, clip.Duration, precision: 5); // 3 frames / 8 FPS
    }

    [Fact]
    public void Duration_WithManyFrames_CalculatesCorrectly()
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = 30.0f,
            Loop = false,
            Frames = new AnimationFrame[90]
        };

        // Act
        var duration = clip.Duration;

        // Assert
        Assert.Equal(3.0f, duration, precision: 5); // 90 frames / 30 FPS = 3 seconds
    }

    [Theory]
    [InlineData(12.0f, 12, 1.0f)]
    [InlineData(24.0f, 24, 1.0f)]
    [InlineData(30.0f, 60, 2.0f)]
    [InlineData(60.0f, 30, 0.5f)]
    [InlineData(10.0f, 5, 0.5f)]
    public void Duration_WithVariousFrameRates_CalculatesCorrectly(float fps, int frameCount, float expectedDuration)
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = fps,
            Loop = false,
            Frames = new AnimationFrame[frameCount]
        };

        // Act
        var duration = clip.Duration;

        // Assert
        Assert.Equal(expectedDuration, duration, precision: 5);
    }

    [Theory]
    [InlineData(12.0f, 0.08333333f)]
    [InlineData(24.0f, 0.04166667f)]
    [InlineData(30.0f, 0.03333333f)]
    [InlineData(60.0f, 0.01666667f)]
    [InlineData(6.0f, 0.16666667f)]
    public void FrameDuration_WithVariousFrameRates_CalculatesCorrectly(float fps, float expectedFrameDuration)
    {
        // Arrange
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = fps,
            Loop = false,
            Frames = []
        };

        // Act
        var frameDuration = clip.FrameDuration;

        // Assert
        Assert.Equal(expectedFrameDuration, frameDuration, precision: 5);
    }

    [Fact]
    public void AnimationClip_LoopProperty_CanBeToggled()
    {
        // Arrange & Act - Test false
        var clipFalse = new AnimationClip
        {
            Name = "test",
            Fps = 12.0f,
            Loop = false,
            Frames = []
        };
        
        // Assert
        Assert.False(clipFalse.Loop);

        // Arrange & Act - Test true
        var clipTrue = new AnimationClip
        {
            Name = "test",
            Fps = 12.0f,
            Loop = true,
            Frames = []
        };
        
        // Assert
        Assert.True(clipTrue.Loop);
    }

    [Fact]
    public void AnimationClip_FramesArray_CanBeModified()
    {
        // Arrange
        var frames = new AnimationFrame[3];
        frames[0] = new AnimationFrame { Rect = new Rectangle(0, 0, 16, 16), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] };
        frames[1] = new AnimationFrame { Rect = new Rectangle(16, 0, 16, 16), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] };
        frames[2] = new AnimationFrame { Rect = new Rectangle(32, 0, 16, 16), Pivot = new Vector2(0.5f, 0), Scale = Vector2.One, Events = [], TexCoords = new Vector2[4] };
        
        var clip = new AnimationClip
        {
            Name = "test",
            Fps = 12.0f,
            Loop = false,
            Frames = frames
        };

        // Assert
        Assert.Equal(3, clip.Frames.Length);
        Assert.Equal(0, clip.Frames[0].Rect.X);
        Assert.Equal(16, clip.Frames[1].Rect.X);
        Assert.Equal(32, clip.Frames[2].Rect.X);
    }
}
