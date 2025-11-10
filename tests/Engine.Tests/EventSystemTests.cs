using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Shouldly;
using Xunit;

namespace Engine.Tests;

public class EventSystemTests
{
    #region Event Base Tests

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

    #endregion

    #region KeyEvent Tests

    [Fact]
    public void KeyPressedEvent_Constructor_ShouldSetProperties()
    {
        // Act
        var @event = new KeyPressedEvent(KeyCodes.Space, true);

        // Assert
        @event.KeyCode.ShouldBe(KeyCodes.Space);
        @event.IsRepeat.ShouldBeTrue();
    }

    [Fact]
    public void KeyPressedEvent_IsInCategory_Keyboard_ShouldReturnTrue()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.Enter, false);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryKeyboard).ShouldBeTrue();
    }

    [Fact]
    public void KeyPressedEvent_IsInCategory_Input_ShouldReturnTrue()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.Escape, false);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryInput).ShouldBeTrue();
    }

    [Fact]
    public void KeyPressedEvent_IsInCategory_Mouse_ShouldReturnFalse()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.A, false);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeFalse();
    }

    [Fact]
    public void KeyReleasedEvent_Constructor_ShouldSetKeyCode()
    {
        // Act
        var @event = new KeyReleasedEvent(KeyCodes.W);

        // Assert
        @event.KeyCode.ShouldBe(KeyCodes.W);
    }

    [Fact]
    public void KeyReleasedEvent_IsInCategory_Keyboard_ShouldReturnTrue()
    {
        // Arrange
        var @event = new KeyReleasedEvent(KeyCodes.S);

        // Act & Assert
        @event.IsInCategory(EventCategory.EventCategoryKeyboard).ShouldBeTrue();
    }

    [Theory]
    [InlineData(KeyCodes.A)]
    [InlineData(KeyCodes.Z)]
    [InlineData(KeyCodes.Space)]
    [InlineData(KeyCodes.Enter)]
    [InlineData(KeyCodes.Escape)]
    public void KeyPressedEvent_VariousKeyCodes_ShouldSetCorrectly(KeyCodes keyCode)
    {
        // Act
        var @event = new KeyPressedEvent(keyCode, false);

        // Assert
        @event.KeyCode.ShouldBe(keyCode);
    }

    [Fact]
    public void KeyPressedEvent_IsRepeatFalse_ShouldIndicateFirstPress()
    {
        // Act
        var @event = new KeyPressedEvent(KeyCodes.A, false);

        // Assert
        @event.IsRepeat.ShouldBeFalse();
    }

    [Fact]
    public void KeyPressedEvent_IsRepeatTrue_ShouldIndicateRepeatedPress()
    {
        // Act
        var @event = new KeyPressedEvent(KeyCodes.A, true);

        // Assert
        @event.IsRepeat.ShouldBeTrue();
    }

    #endregion

    #region MouseEvent Tests

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

    #endregion

    #region EventDispatcher Tests

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

    #endregion

    #region EventCategory Flag Tests

    [Fact]
    public void EventCategory_Flags_ShouldCombineCorrectly()
    {
        // Arrange
        var combined = EventCategory.EventCategoryKeyboard | EventCategory.EventCategoryInput;

        // Act & Assert
        combined.HasFlag(EventCategory.EventCategoryKeyboard).ShouldBeTrue();
        combined.HasFlag(EventCategory.EventCategoryInput).ShouldBeTrue();
        combined.HasFlag(EventCategory.EventCategoryMouse).ShouldBeFalse();
    }

    [Fact]
    public void EventCategory_MultipleCategories_ShouldBeDetectable()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.A, false);

        // Act & Assert - KeyEvent should be in both Keyboard and Input categories
        @event.IsInCategory(EventCategory.EventCategoryKeyboard).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryInput).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeFalse();
        @event.IsInCategory(EventCategory.EventCategoryApplication).ShouldBeFalse();
    }

    [Fact]
    public void MouseEvent_MultipleCategories_ShouldBeDetectable()
    {
        // Arrange
        var @event = new MouseMovedEvent(0, 0);

        // Act & Assert - MouseEvent should be in both Mouse and Input categories
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryInput).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryKeyboard).ShouldBeFalse();
        @event.IsInCategory(EventCategory.EventCategoryApplication).ShouldBeFalse();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void KeyPressedEvent_RecordEquality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var event1 = new KeyPressedEvent(KeyCodes.A, false);
        var event2 = new KeyPressedEvent(KeyCodes.A, false);

        // Act & Assert - Records have value-based equality
        event1.ShouldNotBeSameAs(event2);
        event1.Equals(event2).ShouldBeTrue();
    }

    [Fact]
    public void KeyPressedEvent_RecordEquality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var event1 = new KeyPressedEvent(KeyCodes.A, false);
        var event2 = new KeyPressedEvent(KeyCodes.B, false);

        // Act & Assert
        event1.Equals(event2).ShouldBeFalse();
    }

    [Fact]
    public void MouseMovedEvent_RecordEquality_SameCoords_ShouldBeEqual()
    {
        // Arrange
        var event1 = new MouseMovedEvent(100, 200);
        var event2 = new MouseMovedEvent(100, 200);

        // Act & Assert
        event1.ShouldNotBeSameAs(event2);
        event1.Equals(event2).ShouldBeTrue();
    }

    [Fact]
    public void MouseMovedEvent_RecordEquality_DifferentCoords_ShouldNotBeEqual()
    {
        // Arrange
        var event1 = new MouseMovedEvent(100, 200);
        var event2 = new MouseMovedEvent(150, 200);

        // Act & Assert
        event1.Equals(event2).ShouldBeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EventWorkflow_CreateKeyEvent_DispatchAndHandle_ShouldWorkCorrectly()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.W, false);
        var dispatcher = new EventDispatcher<KeyPressedEvent>(@event);
        var wasHandled = false;

        // Act
        dispatcher.Dispatch(e =>
        {
            if (e.KeyCode == KeyCodes.W)
            {
                wasHandled = true;
                return true;
            }
            return false;
        });

        // Assert
        wasHandled.ShouldBeTrue();
        @event.IsHandled.ShouldBeTrue();
    }

    [Fact]
    public void EventWorkflow_CreateMouseEvent_DispatchAndHandle_ShouldWorkCorrectly()
    {
        // Arrange
        var @event = new MouseButtonPressedEvent(0);
        var dispatcher = new EventDispatcher<MouseButtonPressedEvent>(@event);
        var clickedButton = -1;

        // Act
        dispatcher.Dispatch(e =>
        {
            clickedButton = e.Button;
            return true;
        });

        // Assert
        clickedButton.ShouldBe(0);
        @event.IsHandled.ShouldBeTrue();
    }

    [Fact]
    public void EventWorkflow_FilterEventsByCategory_ShouldWorkCorrectly()
    {
        // Arrange
        var events = new Event[]
        {
            new KeyPressedEvent(KeyCodes.A, false),
            new MouseMovedEvent(10, 20),
            new KeyReleasedEvent(KeyCodes.B),
            new MouseButtonPressedEvent(0)
        };

        // Act
        var keyboardEvents = events.Where(e => e.IsInCategory(EventCategory.EventCategoryKeyboard)).ToList();
        var mouseEvents = events.Where(e => e.IsInCategory(EventCategory.EventCategoryMouse)).ToList();

        // Assert
        keyboardEvents.Count.ShouldBe(2);
        mouseEvents.Count.ShouldBe(2);
    }

    #endregion
}
