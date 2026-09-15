using Engine.Events.Input;
using Input;
using Shouldly;

namespace Engine.Tests.Events;

public class KeyEventTests
{
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
    public void KeyReleasedEvent_Constructor_ShouldSetKeyCode()
    {
        // Act
        var @event = new KeyReleasedEvent(KeyCodes.W);

        // Assert
        @event.KeyCode.ShouldBe(KeyCodes.W);
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
}
