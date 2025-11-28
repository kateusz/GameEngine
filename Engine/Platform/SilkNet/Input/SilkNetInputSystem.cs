using System.Collections.Concurrent;
using Engine.Core.Input;
using Engine.Events.Input;
using Silk.NET.Input;

namespace Engine.Platform.SilkNet.Input;

public sealed class SilkNetInputSystem : IInputSystem
{
    private readonly ConcurrentQueue<InputEvent> _inputQueue = new();
    private volatile bool _disposed;

    public SilkNetInputSystem(IInputContext inputContext)
    {
        Context = inputContext ?? throw new ArgumentNullException(nameof(inputContext));
        
        var silkKeyboard = Context.Keyboards.FirstOrDefault()
                           ?? throw new InvalidOperationException("No keyboard found");
        var silkMouse = Context.Mice.FirstOrDefault()
                        ?? throw new InvalidOperationException("No mouse found");

        // Subscribe to SilkNet input events
        silkKeyboard.KeyDown += OnSilkKeyDown;
        silkKeyboard.KeyUp += OnSilkKeyUp;
        silkMouse.MouseDown += OnSilkMouseDown;
        silkMouse.MouseUp += OnSilkMouseUp;
        silkMouse.Scroll += OnSilkMouseScroll;
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

    private void OnSilkKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (_disposed) return;

        var inputEvent = new KeyPressedEvent((KeyCodes)key, false);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        if (_disposed) return;

        var inputEvent = new KeyReleasedEvent((KeyCodes)key);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseDown(IMouse mouse, MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonPressedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseUp(IMouse mouse, MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonReleasedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        if (_disposed) return;

        var inputEvent = new MouseScrolledEvent(scrollWheel.X, scrollWheel.Y);
        _inputQueue.Enqueue(inputEvent);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Context?.Dispose();
        }
    }
}