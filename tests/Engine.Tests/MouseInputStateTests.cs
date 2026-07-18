using System.Numerics;
using Engine.Core.Input;
using Engine.Events.Input;
using Input;
using Shouldly;

namespace Engine.Tests;

public class MouseInputStateTests
{
    [Fact]
    public void Apply_MouseMoved_UpdatesPosition()
    {
        var state = new MouseInputState();
        state.Apply(new MouseMovedEvent(12.5f, 40f));

        state.Position.ShouldBe(new Vector2(12.5f, 40f));
    }

    [Fact]
    public void Apply_ButtonPress_SetsHeldAndWasPressed()
    {
        var state = new MouseInputState();
        state.Apply(new MouseButtonPressedEvent(MouseButtons.Left));

        state.IsButtonDown(MouseButtons.Left).ShouldBeTrue();
        state.WasButtonPressed(MouseButtons.Left).ShouldBeTrue();
    }

    [Fact]
    public void EndFrame_ClearsWasPressedButKeepsHeld()
    {
        var state = new MouseInputState();
        state.Apply(new MouseButtonPressedEvent(MouseButtons.Left));
        state.EndFrame();

        state.IsButtonDown(MouseButtons.Left).ShouldBeTrue();
        state.WasButtonPressed(MouseButtons.Left).ShouldBeFalse();
    }

    [Fact]
    public void Apply_ButtonRelease_ClearsHeld()
    {
        var state = new MouseInputState();
        state.Apply(new MouseButtonPressedEvent(MouseButtons.Left));
        state.Apply(new MouseButtonReleasedEvent(MouseButtons.Left));

        state.IsButtonDown(MouseButtons.Left).ShouldBeFalse();
    }

    [Fact]
    public void Apply_KeyEvent_IsIgnored()
    {
        var state = new MouseInputState();
        state.Apply(new KeyPressedEvent(KeyCodes.A, isRepeat: false));

        state.Position.ShouldBe(Vector2.Zero);
        state.IsButtonDown(MouseButtons.Left).ShouldBeFalse();
    }
}
