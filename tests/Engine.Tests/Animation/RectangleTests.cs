using Engine.Animation;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for Rectangle struct.
/// </summary>
public class RectangleTests
{
    [Fact]
    public void Rectangle_Constructor_SetsAllProperties()
    {
        // Act
        var rect = new Rectangle(10, 20, 100, 200);

        // Assert
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void Rectangle_DefaultConstructor_SetsZeroValues()
    {
        // Act
        var rect = new Rectangle();

        // Assert
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
    }

    [Fact]
    public void Rectangle_WithNegativeValues_AllowsNegative()
    {
        // Act
        var rect = new Rectangle(-10, -20, -30, -40);

        // Assert
        Assert.Equal(-10, rect.X);
        Assert.Equal(-20, rect.Y);
        Assert.Equal(-30, rect.Width);
        Assert.Equal(-40, rect.Height);
    }

    [Fact]
    public void Rectangle_Equality_ComparesAllFields()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 30, 40);
        var rect2 = new Rectangle(10, 20, 30, 40);
        var rect3 = new Rectangle(10, 20, 30, 50); // Different height

        // Act & Assert
        Assert.Equal(rect1, rect2);
        Assert.NotEqual(rect1, rect3);
        Assert.True(rect1 == rect2);
        Assert.False(rect1 == rect3);
    }

    [Fact]
    public void Rectangle_GetHashCode_SameForEqualRectangles()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 30, 40);
        var rect2 = new Rectangle(10, 20, 30, 40);

        // Act
        var hash1 = rect1.GetHashCode();
        var hash2 = rect2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Rectangle_IsValueType()
    {
        // Arrange
        var rect1 = new Rectangle(10, 20, 30, 40);

        // Act - Assignment creates a copy
        var rect2 = rect1;
        rect2 = rect2 with { Width = 100 }; // Modify copy

        // Assert - Original unchanged
        Assert.Equal(30, rect1.Width);
        Assert.Equal(100, rect2.Width);
    }

    [Theory]
    [InlineData(0, 0, 32, 32)]
    [InlineData(32, 0, 32, 32)]
    [InlineData(64, 32, 16, 16)]
    [InlineData(128, 256, 64, 128)]
    public void Rectangle_WithVariousValues_StoresCorrectly(int x, int y, int width, int height)
    {
        // Act
        var rect = new Rectangle(x, y, width, height);

        // Assert
        Assert.Equal(x, rect.X);
        Assert.Equal(y, rect.Y);
        Assert.Equal(width, rect.Width);
        Assert.Equal(height, rect.Height);
    }

    [Fact]
    public void Rectangle_CanUseWithExpression()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 30, 40);

        // Act
        var modified = rect with { Width = 50, Height = 60 };

        // Assert
        Assert.Equal(10, modified.X);
        Assert.Equal(20, modified.Y);
        Assert.Equal(50, modified.Width);
        Assert.Equal(60, modified.Height);

        // Original unchanged
        Assert.Equal(30, rect.Width);
        Assert.Equal(40, rect.Height);
    }

    [Fact]
    public void Rectangle_Deconstruction_Works()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 30, 40);

        // Act
        var (x, y, width, height) = rect;

        // Assert
        Assert.Equal(10, x);
        Assert.Equal(20, y);
        Assert.Equal(30, width);
        Assert.Equal(40, height);
    }

    [Fact]
    public void Rectangle_ToString_ReturnsReadableFormat()
    {
        // Arrange
        var rect = new Rectangle(10, 20, 30, 40);

        // Act
        var str = rect.ToString();

        // Assert
        Assert.Contains("10", str);
        Assert.Contains("20", str);
        Assert.Contains("30", str);
        Assert.Contains("40", str);
    }
}
