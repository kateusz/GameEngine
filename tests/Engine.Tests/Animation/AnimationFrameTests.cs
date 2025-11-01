using System.Numerics;
using Engine.Animation;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for AnimationFrame class, focusing on UV coordinate calculation.
/// </summary>
public class AnimationFrameTests
{
    [Fact]
    public void CalculateUVCoords_WithBasicRect_ProducesCorrectUVs()
    {
        // Arrange
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(0, 0, 32, 32),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4]
        };

        // Act
        frame.CalculateUvCoords(128, 128);

        // Assert
        Assert.Equal(0.0f, frame.TexCoords[0].X, precision: 5); // Bottom-left X
        Assert.Equal(0.0f, frame.TexCoords[0].Y, precision: 5); // Bottom-left Y
        Assert.Equal(0.25f, frame.TexCoords[1].X, precision: 5); // Bottom-right X
        Assert.Equal(0.0f, frame.TexCoords[1].Y, precision: 5); // Bottom-right Y
        Assert.Equal(0.25f, frame.TexCoords[2].X, precision: 5); // Top-right X
        Assert.Equal(0.25f, frame.TexCoords[2].Y, precision: 5); // Top-right Y
        Assert.Equal(0.0f, frame.TexCoords[3].X, precision: 5); // Top-left X
        Assert.Equal(0.25f, frame.TexCoords[3].Y, precision: 5); // Top-left Y
    }

    [Fact]
    public void CalculateUVCoords_WithOffset_ProducesCorrectUVs()
    {
        // Arrange
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(64, 64, 32, 32),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4]
        };

        // Act
        frame.CalculateUvCoords(128, 128);

        // Assert
        Assert.Equal(0.5f, frame.TexCoords[0].X, precision: 5); // Bottom-left X
        Assert.Equal(0.5f, frame.TexCoords[0].Y, precision: 5); // Bottom-left Y
        Assert.Equal(0.75f, frame.TexCoords[1].X, precision: 5); // Bottom-right X
        Assert.Equal(0.5f, frame.TexCoords[1].Y, precision: 5); // Bottom-right Y
        Assert.Equal(0.75f, frame.TexCoords[2].X, precision: 5); // Top-right X
        Assert.Equal(0.75f, frame.TexCoords[2].Y, precision: 5); // Top-right Y
        Assert.Equal(0.5f, frame.TexCoords[3].X, precision: 5); // Top-left X
        Assert.Equal(0.75f, frame.TexCoords[3].Y, precision: 5); // Top-left Y
    }

    [Fact]
    public void CalculateUVCoords_WithHorizontalFlip_SwapsXCoordinates()
    {
        // Arrange
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(0, 0, 32, 32),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4],
            Flip = new Vector2(1.0f, 0.0f) // Horizontal flip
        };

        // Act
        frame.CalculateUvCoords(128, 128);

        // Assert - X coordinates should be swapped
        Assert.Equal(0.25f, frame.TexCoords[0].X, precision: 5); // Bottom-left X (swapped)
        Assert.Equal(0.0f, frame.TexCoords[1].X, precision: 5); // Bottom-right X (swapped)
        Assert.Equal(0.0f, frame.TexCoords[2].X, precision: 5); // Top-right X (swapped)
        Assert.Equal(0.25f, frame.TexCoords[3].X, precision: 5); // Top-left X (swapped)

        // Y coordinates should remain unchanged
        Assert.Equal(0.0f, frame.TexCoords[0].Y, precision: 5);
        Assert.Equal(0.0f, frame.TexCoords[1].Y, precision: 5);
        Assert.Equal(0.25f, frame.TexCoords[2].Y, precision: 5);
        Assert.Equal(0.25f, frame.TexCoords[3].Y, precision: 5);
    }

    [Fact]
    public void CalculateUVCoords_WithVerticalFlip_SwapsYCoordinates()
    {
        // Arrange
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(0, 0, 32, 32),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4],
            Flip = new Vector2(0.0f, 1.0f) // Vertical flip
        };

        // Act
        frame.CalculateUvCoords(128, 128);

        // Assert - Y coordinates should be swapped
        Assert.Equal(0.25f, frame.TexCoords[0].Y, precision: 5); // Bottom-left Y (swapped)
        Assert.Equal(0.25f, frame.TexCoords[1].Y, precision: 5); // Bottom-right Y (swapped)
        Assert.Equal(0.0f, frame.TexCoords[2].Y, precision: 5); // Top-right Y (swapped)
        Assert.Equal(0.0f, frame.TexCoords[3].Y, precision: 5); // Top-left Y (swapped)

        // X coordinates should remain unchanged
        Assert.Equal(0.0f, frame.TexCoords[0].X, precision: 5);
        Assert.Equal(0.25f, frame.TexCoords[1].X, precision: 5);
        Assert.Equal(0.25f, frame.TexCoords[2].X, precision: 5);
        Assert.Equal(0.0f, frame.TexCoords[3].X, precision: 5);
    }

    [Fact]
    public void CalculateUVCoords_WithBothFlips_SwapsBothCoordinates()
    {
        // Arrange
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(0, 0, 32, 32),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4],
            Flip = new Vector2(1.0f, 1.0f) // Both flips
        };

        // Act
        frame.CalculateUvCoords(128, 128);

        // Assert - Both X and Y coordinates should be swapped
        Assert.Equal(0.25f, frame.TexCoords[0].X, precision: 5); // Bottom-left X (swapped)
        Assert.Equal(0.25f, frame.TexCoords[0].Y, precision: 5); // Bottom-left Y (swapped)
        Assert.Equal(0.0f, frame.TexCoords[1].X, precision: 5); // Bottom-right X (swapped)
        Assert.Equal(0.25f, frame.TexCoords[1].Y, precision: 5); // Bottom-right Y (swapped)
        Assert.Equal(0.0f, frame.TexCoords[2].X, precision: 5); // Top-right X (swapped)
        Assert.Equal(0.0f, frame.TexCoords[2].Y, precision: 5); // Top-right Y (swapped)
        Assert.Equal(0.25f, frame.TexCoords[3].X, precision: 5); // Top-left X (swapped)
        Assert.Equal(0.0f, frame.TexCoords[3].Y, precision: 5); // Top-left Y (swapped)
    }

    [Fact]
    public void CalculateUVCoords_WithNonSquareRect_HandlesAspectRatio()
    {
        // Arrange
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(0, 0, 64, 32), // Wide rectangle
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4]
        };

        // Act
        frame.CalculateUvCoords(128, 128);

        // Assert
        Assert.Equal(0.0f, frame.TexCoords[0].X, precision: 5);
        Assert.Equal(0.0f, frame.TexCoords[0].Y, precision: 5);
        Assert.Equal(0.5f, frame.TexCoords[1].X, precision: 5); // Wider
        Assert.Equal(0.0f, frame.TexCoords[1].Y, precision: 5);
        Assert.Equal(0.5f, frame.TexCoords[2].X, precision: 5);
        Assert.Equal(0.25f, frame.TexCoords[2].Y, precision: 5); // Shorter
        Assert.Equal(0.0f, frame.TexCoords[3].X, precision: 5);
        Assert.Equal(0.25f, frame.TexCoords[3].Y, precision: 5);
    }

    [Fact]
    public void AnimationFrame_DefaultProperties_HaveCorrectValues()
    {
        // Arrange & Act
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(0, 0, 32, 32),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4]
        };

        // Assert
        Assert.Equal(0.5f, frame.Pivot.X);
        Assert.Equal(0.0f, frame.Pivot.Y);
        Assert.Null(frame.Flip);
        Assert.Null(frame.Rotation);
        Assert.Equal(Vector2.One, frame.Scale);
        Assert.Empty(frame.Events);
        Assert.Equal(4, frame.TexCoords.Length);
    }

    [Fact]
    public void AnimationFrame_CustomProperties_CanBeSet()
    {
        // Arrange & Act
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(10, 20, 30, 40),
            Pivot = new Vector2(0.25f, 0.75f),
            Flip = new Vector2(1.0f, 0.0f),
            Rotation = 45.0f,
            Scale = new Vector2(2.0f, 2.0f),
            Events = new[] { "footstep", "dust" },
            TexCoords = new Vector2[4]
        };

        // Assert
        Assert.Equal(10, frame.Rect.X);
        Assert.Equal(20, frame.Rect.Y);
        Assert.Equal(30, frame.Rect.Width);
        Assert.Equal(40, frame.Rect.Height);
        Assert.Equal(0.25f, frame.Pivot.X);
        Assert.Equal(0.75f, frame.Pivot.Y);
        Assert.NotNull(frame.Flip);
        Assert.Equal(1.0f, frame.Flip.Value.X);
        Assert.Equal(0.0f, frame.Flip.Value.Y);
        Assert.NotNull(frame.Rotation);
        Assert.Equal(45.0f, frame.Rotation.Value);
        Assert.Equal(2.0f, frame.Scale.X);
        Assert.Equal(2.0f, frame.Scale.Y);
        Assert.Equal(2, frame.Events.Length);
        Assert.Equal("footstep", frame.Events[0]);
        Assert.Equal("dust", frame.Events[1]);
    }

    [Fact]
    public void CalculateUVCoords_WithFullTextureRect_ProducesFullUVRange()
    {
        // Arrange
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(0, 0, 256, 256),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4]
        };

        // Act
        frame.CalculateUvCoords(256, 256);

        // Assert - Should span entire UV space (0,0) to (1,1)
        Assert.Equal(0.0f, frame.TexCoords[0].X, precision: 5);
        Assert.Equal(0.0f, frame.TexCoords[0].Y, precision: 5);
        Assert.Equal(1.0f, frame.TexCoords[1].X, precision: 5);
        Assert.Equal(0.0f, frame.TexCoords[1].Y, precision: 5);
        Assert.Equal(1.0f, frame.TexCoords[2].X, precision: 5);
        Assert.Equal(1.0f, frame.TexCoords[2].Y, precision: 5);
        Assert.Equal(0.0f, frame.TexCoords[3].X, precision: 5);
        Assert.Equal(1.0f, frame.TexCoords[3].Y, precision: 5);
    }
}
