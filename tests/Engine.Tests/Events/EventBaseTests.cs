using Engine.Core.Input;
using Engine.Events.Input;
using Shouldly;

namespace Engine.Tests.Events;

public class EventBaseTests
{
    [Fact]
    public void Event_IsHandled_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var keyEvent = new KeyPressedEvent(KeyCodes.A, false);

        // Assert
        keyEvent.IsHandled.ShouldBeFalse();
    }

    [Fact]
    public void Event_SetIsHandled_ShouldUpdateValue()
    {
        // Arrange
        var keyEvent = new KeyPressedEvent(KeyCodes.A, false);

        // Act
        keyEvent.IsHandled = true;

        // Assert
        keyEvent.IsHandled.ShouldBeTrue();
    }
}