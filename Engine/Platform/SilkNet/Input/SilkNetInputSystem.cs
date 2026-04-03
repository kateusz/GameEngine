using System.Collections.Concurrent;
using System.Numerics;
using Engine.Core.Input;
using Engine.Events.Input;
using Silk.NET.Input;

namespace Engine.Platform.SilkNet.Input;

internal sealed class SilkNetInputSystem : IInputSystem
{
    private readonly ConcurrentQueue<InputEvent> _inputQueue = new();
    private volatile bool _disposed;

    public SilkNetInputSystem(IInputContext inputContext)
    {
        Context = inputContext;

        var silkKeyboard = Context.Keyboards.FirstOrDefault()
                           ?? throw new InvalidOperationException("No keyboard found");
        var silkMouse = Context.Mice.FirstOrDefault()
                        ?? throw new InvalidOperationException("No mouse found");

        // Subscribe to SilkNet input events
        silkKeyboard.KeyDown += (_, key, _) => OnSilkKeyDown(key);
        silkKeyboard.KeyUp += (_, key, _) => OnSilkKeyUp(key);
        silkMouse.MouseDown += (_, button) => OnSilkMouseDown(button);
        silkMouse.MouseUp +=  (_, button) => OnSilkMouseUp(button);
        silkMouse.Scroll +=  (_, scrollWheel) => OnSilkMouseScroll(scrollWheel);
        silkMouse.MouseMove += (_, position) => OnSilkMouseMove(position);
    }

    public IInputContext Context { get; set; }

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
            Context?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}