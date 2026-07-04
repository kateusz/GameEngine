using Engine.Core.Input;
using Engine.Events.Input;
using Input;
using Shouldly;

namespace Engine.Tests;

public class KeyboardInputStateTests
{
    [Fact]
    public void Apply_KeyPress_SetsHeldAndWasPressed()
    {
        var state = new KeyboardInputState();
        state.Apply(new KeyPressedEvent(KeyCodes.A, isRepeat: false));

        state.IsKeyDown(KeyCodes.A).ShouldBeTrue();
        state.WasKeyPressed(KeyCodes.A).ShouldBeTrue();
    }

    [Fact]
    public void Apply_KeyRepeat_DoesNotSetWasPressed()
    {
        var state = new KeyboardInputState();
        state.Apply(new KeyPressedEvent(KeyCodes.A, isRepeat: true));

        state.IsKeyDown(KeyCodes.A).ShouldBeTrue();
        state.WasKeyPressed(KeyCodes.A).ShouldBeFalse();
    }

    [Fact]
    public void EndFrame_ClearsWasPressedButKeepsHeld()
    {
        var state = new KeyboardInputState();
        state.Apply(new KeyPressedEvent(KeyCodes.A, isRepeat: false));
        state.EndFrame();

        state.IsKeyDown(KeyCodes.A).ShouldBeTrue();
        state.WasKeyPressed(KeyCodes.A).ShouldBeFalse();
    }

    [Fact]
    public void Apply_KeyRelease_ClearsHeld()
    {
        var state = new KeyboardInputState();
        state.Apply(new KeyPressedEvent(KeyCodes.A, isRepeat: false));
        state.Apply(new KeyReleasedEvent(KeyCodes.A));

        state.IsKeyDown(KeyCodes.A).ShouldBeFalse();
    }
}
