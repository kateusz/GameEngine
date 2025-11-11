using Engine.Events;
using Engine.Events.Input;
using Shouldly;

namespace Engine.Tests.Events;

public class MouseEventTests
{
    [Fact]
    public void MouseMovedEvent_Constructor_ShouldSetCoordinates()
    {
        // Act
        var @event = new MouseMovedEvent(100, 200);

        // Assert
        @event.X.ShouldBe(100u);
        @event.Y.ShouldBe(200u);
    }

    [Fact]
    public void MouseMovedEvent_IsInCategory_Mouse_ShouldReturnTrue()
    {
        // Arrange
        var @event = new MouseMovedEvent(50, 75);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeTrue();
    }

    [Fact]
    public void MouseMovedEvent_IsInCategory_Input_ShouldReturnTrue()
    {
        // Arrange
        var @event = new MouseMovedEvent(10, 20);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryInput).ShouldBeTrue();
    }

    [Fact]
    public void MouseMovedEvent_IsInCategory_Keyboard_ShouldReturnFalse()
    {
        // Arrange
        var @event = new MouseMovedEvent(0, 0);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryKeyboard).ShouldBeFalse();
    }

    [Fact]
    public void MouseScrolledEvent_Constructor_ShouldSetOffsets()
    {
        // Act
        var @event = new MouseScrolledEvent(1.5f, -0.5f);

        // Assert
        @event.XOffSet.ShouldBe(1.5f);
        @event.YOffset.ShouldBe(-0.5f);
    }

    [Fact]
    public void MouseScrolledEvent_IsInCategory_Mouse_ShouldReturnTrue()
    {
        // Arrange
        var @event = new MouseScrolledEvent(0f, 1f);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeTrue();
    }

    [Fact]
    public void MouseButtonPressedEvent_Constructor_ShouldSetButton()
    {
        // Act
        var @event = new MouseButtonPressedEvent(0); // Left button

        // Assert
        @event.Button.ShouldBe(0);
    }

    [Fact]
    public void MouseButtonPressedEvent_IsInCategory_Mouse_ShouldReturnTrue()
    {
        // Arrange
        var @event = new MouseButtonPressedEvent(1);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeTrue();
    }

    [Fact]
    public void MouseButtonReleasedEvent_Constructor_ShouldSetButton()
    {
        // Act
        var @event = new MouseButtonReleasedEvent(2); // Right button

        // Assert
        @event.Button.ShouldBe(2);
    }

    [Fact]
    public void MouseButtonReleasedEvent_IsInCategory_Mouse_ShouldReturnTrue()
    {
        // Arrange
        var @event = new MouseButtonReleasedEvent(0);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]  // Left button
    [InlineData(1)]  // Middle button
    [InlineData(2)]  // Right button
    public void MouseButtonEvents_VariousButtons_ShouldSetCorrectly(int button)
    {
        // Act
        var pressedEvent = new MouseButtonPressedEvent(button);
        var releasedEvent = new MouseButtonReleasedEvent(button);

        // Assert
        pressedEvent.Button.ShouldBe(button);
        releasedEvent.Button.ShouldBe(button);
    }
}