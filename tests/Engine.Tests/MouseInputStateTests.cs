using System.Numerics;
using Engine.Events.Input;
using Engine.Input;
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

    [Fact]
    public void Apply_FirstMove_SetsPositionWithoutDelta()
    {
        var state = new MouseInputState();
        state.Apply(new MouseMovedEvent(10f, 20f));

        state.Position.ShouldBe(new Vector2(10f, 20f));
        state.Delta.ShouldBe(Vector2.Zero);
    }

    [Fact]
    public void Apply_SecondMove_SetsDeltaFromPreviousPosition()
    {
        var state = new MouseInputState();
        state.Apply(new MouseMovedEvent(10f, 20f));
        state.Apply(new MouseMovedEvent(13f, 24f));

        state.Position.ShouldBe(new Vector2(13f, 24f));
        state.Delta.ShouldBe(new Vector2(3f, 4f));
    }

    [Fact]
    public void Apply_Scroll_AccumulatesUntilEndFrame()
    {
        var state = new MouseInputState();
        state.Apply(new MouseScrolledEvent(1f, 2f));
        state.Apply(new MouseScrolledEvent(0f, 3f));

        state.Scroll.ShouldBe(new Vector2(1f, 5f));

        state.EndFrame();

        state.Scroll.ShouldBe(Vector2.Zero);
    }

    [Fact]
    public void EndFrame_ClearsDelta()
    {
        var state = new MouseInputState();
        state.Apply(new MouseMovedEvent(0f, 0f));
        state.Apply(new MouseMovedEvent(4f, 0f));
        state.EndFrame();

        state.Delta.ShouldBe(Vector2.Zero);
        state.Position.ShouldBe(new Vector2(4f, 0f));
    }

    [Fact]
    public void MouseButtons_MatchSilkAndImGuiIndices()
    {
        MouseButtons.Left.ShouldBe(0);
        MouseButtons.Right.ShouldBe(1);
        MouseButtons.Middle.ShouldBe(2);
    }
}
