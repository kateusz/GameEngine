using System.Collections.Concurrent;
using Engine.Core.Input;
using Engine.Events.Input;
using Silk.NET.Input;

namespace Engine.Platform.SilkNet.Input;

public class SilkNetInputSystem : IInputSystem
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

    private void OnSilkKeyDown(Silk.NET.Input.IKeyboard keyboard, Key key, int keyCode)
    {
        if (_disposed) return;

        var inputEvent = new KeyPressedEvent((int)key, false);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkKeyUp(Silk.NET.Input.IKeyboard keyboard, Key key, int keyCode)
    {
        if (_disposed) return;

        var inputEvent = new KeyReleasedEvent((int)key);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseDown(Silk.NET.Input.IMouse mouse, MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonPressedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseUp(Silk.NET.Input.IMouse mouse, MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonReleasedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseScroll(Silk.NET.Input.IMouse mouse, ScrollWheel scrollWheel)
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