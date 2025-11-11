using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Shouldly;

namespace Engine.Tests.Events;

public class EventDispatcherTests
{
    [Fact]
    public void EventDispatcher_Dispatch_ShouldInvokeFunction()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.A, false);
        var dispatcher = new EventDispatcher<KeyPressedEvent>(@event);
        var wasCalled = false;

        // Act
        dispatcher.Dispatch(e =>
        {
            wasCalled = true;
            return true;
        });

        // Assert
        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public void EventDispatcher_Dispatch_ShouldPassCorrectEvent()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.Space, true);
        var dispatcher = new EventDispatcher<KeyPressedEvent>(@event);
        KeyPressedEvent? receivedEvent = null;

        // Act
        dispatcher.Dispatch(e =>
        {
            receivedEvent = e;
            return true;
        });

        // Assert
        receivedEvent.ShouldBe(@event);
        receivedEvent.KeyCode.ShouldBe(KeyCodes.Space);
        receivedEvent.IsRepeat.ShouldBeTrue();
    }

    [Fact]
    public void EventDispatcher_Dispatch_WhenHandlerReturnsTrue_ShouldMarkEventAsHandled()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.Enter, false);
        var dispatcher = new EventDispatcher<KeyPressedEvent>(@event);

        // Act
        dispatcher.Dispatch(e => true);

        // Assert
        @event.IsHandled.ShouldBeTrue();
    }

    [Fact]
    public void EventDispatcher_Dispatch_WhenHandlerReturnsFalse_ShouldNotMarkEventAsHandled()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.Escape, false);
        var dispatcher = new EventDispatcher<KeyPressedEvent>(@event);

        // Act
        dispatcher.Dispatch(e => false);

        // Assert
        @event.IsHandled.ShouldBeFalse();
    }

    [Fact]
    public void EventDispatcher_Dispatch_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.A, false);
        var dispatcher = new EventDispatcher<KeyPressedEvent>(@event);

        // Act
        var result = dispatcher.Dispatch(e => throw new Exception("Test exception"));

        // Assert
        result.ShouldBeFalse();
        @event.IsHandled.ShouldBeFalse();
    }

    [Fact]
    public void EventDispatcher_Dispatch_WhenNoException_ShouldReturnTrue()
    {
        // Arrange
        var @event = new MouseMovedEvent(10, 20);
        var dispatcher = new EventDispatcher<MouseMovedEvent>(@event);

        // Act
        var result = dispatcher.Dispatch(e => true);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void EventDispatcher_MultipleDispatches_ShouldExecuteAll()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.A, false);
        var dispatcher = new EventDispatcher<KeyPressedEvent>(@event);
        var callCount = 0;

        // Act
        dispatcher.Dispatch(e => { callCount++; return false; });
        dispatcher.Dispatch(e => { callCount++; return false; });
        dispatcher.Dispatch(e => { callCount++; return true; });

        // Assert
        callCount.ShouldBe(3);
        @event.IsHandled.ShouldBeTrue(); // Last dispatch returned true
    }
}