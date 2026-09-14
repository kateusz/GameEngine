using System.Collections.Concurrent;
using System.Numerics;
using Engine.Core.Input;
using Engine.Events.Input;
using Input;
using Silk.NET.Input;

namespace Engine.Platform.SilkNet.Input;

internal sealed class SilkNetInputSystem : IInputSystem
{
    private readonly ConcurrentQueue<InputEvent> _inputQueue = new();
    private readonly IInputContext _context;
    private volatile bool _disposed;

    public SilkNetInputSystem(IInputContext inputContext)
    {
        _context = inputContext;

        var silkKeyboard = _context.Keyboards.FirstOrDefault();
        if (silkKeyboard is not null)
        {
            silkKeyboard.KeyDown += (_, key, _) => OnSilkKeyDown(key);
            silkKeyboard.KeyUp += (_, key, _) => OnSilkKeyUp(key);
        }

        var silkMouse = _context.Mice.FirstOrDefault();
        if (silkMouse is not null)
        {
            silkMouse.MouseDown += (_, button) => OnSilkMouseDown(button);
            silkMouse.MouseUp += (_, button) => OnSilkMouseUp(button);
            silkMouse.Scroll += (_, scrollWheel) => OnSilkMouseScroll(scrollWheel);
            silkMouse.MouseMove += (_, position) => OnSilkMouseMove(position);
        }
    }

    public void Update(TimeSpan deltaTime)
    {
        // Process all queued input events
        while (_inputQueue.TryDequeue(out var inputEvent))
        {
            InputReceived?.Invoke(inputEvent);
        }
    }

    public event Action<InputEvent>? InputReceived;

    private void OnSilkKeyDown(Key key)
    {
        if (_disposed) return;

        var inputEvent = new KeyPressedEvent((KeyCodes)key, false);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkKeyUp(Key key)
    {
        if (_disposed) return;

        var inputEvent = new KeyReleasedEvent((KeyCodes)key);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseDown(MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonPressedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseUp(MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonReleasedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseScroll(ScrollWheel scrollWheel)
    {
        if (_disposed) return;

        var inputEvent = new MouseScrolledEvent(scrollWheel.X, scrollWheel.Y);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseMove(Vector2 position)
    {
        if (_disposed) return;

        var inputEvent = new MouseMovedEvent(position.X, position.Y);
        _inputQueue.Enqueue(inputEvent);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}