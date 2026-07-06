using System.Numerics;
using Engine.Events.Input;
using Engine.Platform.SilkNet.Input;
using Input;
using NSubstitute;
using Shouldly;
using Silk.NET.Input;

namespace Engine.Tests.Platform.SilkNet.Input;

public class SilkNetInputSystemTests
{
    private readonly IInputContext _context;
    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;

    public SilkNetInputSystemTests()
    {
        _keyboard = Substitute.For<IKeyboard>();
        _mouse = Substitute.For<IMouse>();
        _context = Substitute.For<IInputContext>();
        _context.Keyboards.Returns([_keyboard]);
        _context.Mice.Returns([_mouse]);
    }

    [Fact]
    public void Constructor_NoKeyboard_ThrowsInvalidOperationException()
    {
        var ctx = Substitute.For<IInputContext>();
        ctx.Keyboards.Returns([]);
        ctx.Mice.Returns([_mouse]);

        Should.Throw<InvalidOperationException>(() => new SilkNetInputSystem(ctx));
    }

    [Fact]
    public void Constructor_NoMouse_ThrowsInvalidOperationException()
    {
        var ctx = Substitute.For<IInputContext>();
        ctx.Keyboards.Returns([_keyboard]);
        ctx.Mice.Returns([]);

        Should.Throw<InvalidOperationException>(() => new SilkNetInputSystem(ctx));
    }

    [Fact]
    public void OnKeyDown_EnqueuesKeyPressedEvent()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        _keyboard.KeyDown += Raise.Event<Action<IKeyboard, Key, int>>(_keyboard, Key.A, 0);
        system.Update(TimeSpan.Zero);

        var evt = received.ShouldHaveSingleItem().ShouldBeOfType<KeyPressedEvent>();
        evt.KeyCode.ShouldBe(KeyCodes.A);
        evt.IsRepeat.ShouldBeFalse();
    }

    [Fact]
    public void OnKeyUp_EnqueuesKeyReleasedEvent()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        _keyboard.KeyUp += Raise.Event<Action<IKeyboard, Key, int>>(_keyboard, Key.B, 0);
        system.Update(TimeSpan.Zero);

        var evt = received.ShouldHaveSingleItem().ShouldBeOfType<KeyReleasedEvent>();
        evt.KeyCode.ShouldBe(KeyCodes.B);
    }

    [Fact]
    public void OnMouseDown_EnqueuesMouseButtonPressedEvent()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        _mouse.MouseDown += Raise.Event<Action<IMouse, MouseButton>>(_mouse, MouseButton.Left);
        system.Update(TimeSpan.Zero);

        var evt = received.ShouldHaveSingleItem().ShouldBeOfType<MouseButtonPressedEvent>();
        evt.Button.ShouldBe((int)MouseButton.Left);
    }

    [Fact]
    public void OnMouseUp_EnqueuesMouseButtonReleasedEvent()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        _mouse.MouseUp += Raise.Event<Action<IMouse, MouseButton>>(_mouse, MouseButton.Right);
        system.Update(TimeSpan.Zero);

        var evt = received.ShouldHaveSingleItem().ShouldBeOfType<MouseButtonReleasedEvent>();
        evt.Button.ShouldBe((int)MouseButton.Right);
    }

    [Fact]
    public void OnMouseScroll_EnqueuesMouseScrolledEvent()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        var scrollWheel = new ScrollWheel(10f, 20f);
        _mouse.Scroll += Raise.Event<Action<IMouse, ScrollWheel>>(_mouse, scrollWheel);
        system.Update(TimeSpan.Zero);

        var evt = received.ShouldHaveSingleItem().ShouldBeOfType<MouseScrolledEvent>();
        evt.XOffSet.ShouldBe(10f);
        evt.YOffset.ShouldBe(20f);
    }

    [Fact]
    public void OnMouseMove_EnqueuesMouseMovedEvent()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        var position = new Vector2(100f, 200f);
        _mouse.MouseMove += Raise.Event<Action<IMouse, Vector2>>(_mouse, position);
        system.Update(TimeSpan.Zero);

        var evt = received.ShouldHaveSingleItem().ShouldBeOfType<MouseMovedEvent>();
        evt.X.ShouldBe(100f);
        evt.Y.ShouldBe(200f);
    }

    [Fact]
    public void Update_DispatchesQueuedEvents()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        _keyboard.KeyDown += Raise.Event<Action<IKeyboard, Key, int>>(_keyboard, Key.A, 0);
        _mouse.MouseDown += Raise.Event<Action<IMouse, MouseButton>>(_mouse, MouseButton.Left);
        system.Update(TimeSpan.Zero);

        received.Count.ShouldBe(2);
    }

    [Fact]
    public void Update_AfterDispose_DoesNotDispatchEvents()
    {
        var system = CreateSystem();
        var received = CaptureEvents(system);

        system.Dispose();
        _keyboard.KeyDown += Raise.Event<Action<IKeyboard, Key, int>>(_keyboard, Key.A, 0);
        system.Update(TimeSpan.Zero);

        received.ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_DisposesInputContext()
    {
        var system = CreateSystem();
        system.Dispose();

        _context.Received(1).Dispose();
    }

    private SilkNetInputSystem CreateSystem() => new(_context);

    private List<InputEvent> CaptureEvents(SilkNetInputSystem system)
    {
        var received = new List<InputEvent>();
        system.InputReceived += received.Add;
        return received;
    }
}
