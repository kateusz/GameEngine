using System.Collections.Concurrent;
using System.Numerics;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using NLog;

namespace Engine.Renderer.Cameras;

public class OrthographicCameraController
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    private float _aspectRatio;
    private readonly bool _rotation;
    private Vector3 _cameraPosition = Vector3.Zero;
    private float _cameraTranslationSpeed = 0.5f;
    private float _cameraRotationSpeed = 10.0f;
    private float _cameraRotation;
    private float _zoomLevel = 20.0f;
    
    // Add a speed multiplier for better control
    private float _speedMultiplier = 0.1f; // Adjust this to make camera slower/faster

    // Thread-safe collection for tracking pressed keys (accessed from event thread and update thread)
    private readonly ConcurrentDictionary<KeyCodes, byte> _pressedKeys = new();

    public OrthographicCameraController(float aspectRatio, bool rotation = false)
    {
        _aspectRatio = aspectRatio;
        Camera = new OrthographicCamera(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        _rotation = rotation;
    }

    public OrthographicCameraController(OrthographicCamera camera, float aspectRatio, bool rotation = false)
    {
        _aspectRatio = aspectRatio;
        Camera = camera;
        _rotation = rotation;
    }

    public OrthographicCamera Camera { get; }

    public void OnUpdate(TimeSpan timeSpan)
    {
        // Calculate actual movement speed based on zoom level but with a reasonable multiplier
        float actualSpeed = _cameraTranslationSpeed * _speedMultiplier * _zoomLevel;

        if (_pressedKeys.ContainsKey(KeyCodes.A))
            _cameraPosition.X -= actualSpeed * (float)timeSpan.TotalSeconds;
        else if (_pressedKeys.ContainsKey(KeyCodes.D))
            _cameraPosition.X += actualSpeed * (float)timeSpan.TotalSeconds;
        else if (_pressedKeys.ContainsKey(KeyCodes.S))
            _cameraPosition.Y -= actualSpeed * (float)timeSpan.TotalSeconds;
        else if (_pressedKeys.ContainsKey(KeyCodes.W))
            _cameraPosition.Y += actualSpeed * (float)timeSpan.TotalSeconds;

        if (_rotation)
        {
            if (_pressedKeys.ContainsKey(KeyCodes.Q))
                _cameraRotation += _cameraRotationSpeed * (float)timeSpan.TotalSeconds;
            else if (_pressedKeys.ContainsKey(KeyCodes.E))
                _cameraRotation -= _cameraRotationSpeed * (float)timeSpan.TotalSeconds;
            
            Camera.SetRotation(_cameraRotation);
        }
        
        Camera.SetPosition(_cameraPosition);

        // Remove this line that was causing the high speed:
        // _cameraTranslationSpeed = _zoomLevel; 
    }

    public void OnEvent(Event @event)
    {
        switch (@event)
        {
            case KeyPressedEvent kpe:
                _pressedKeys.TryAdd((KeyCodes)kpe.KeyCode, 0);
                break;
            case KeyReleasedEvent kre:
                _pressedKeys.TryRemove((KeyCodes)kre.KeyCode, out _);
                break;
            case MouseScrolledEvent mse:
                OnMouseScrolled(mse);
                break;
            case WindowResizeEvent wre:
                OnWindowResized(wre);
                break;
        }
    }
    
    // Add methods to control camera speed at runtime
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }
    
    public float GetSpeedMultiplier() => _speedMultiplier;

    private bool OnMouseScrolled(MouseScrolledEvent @event)
    {
        _zoomLevel += @event.YOffset * 0.25f;  // Add delta, not replace
        _zoomLevel = System.Math.Max(_zoomLevel, 0.25f);
        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        return true;
    }

    private bool OnWindowResized(WindowResizeEvent @event)
    {
        // Validate dimensions
        if (@event.Width == 0 || @event.Height == 0)
        {
            Logger.Warn("[Camera] Invalid window dimensions: {Width}x{Height}, ignoring resize", @event.Width, @event.Height);
            return false;
        }

        // Ensure minimum dimensions to prevent extreme aspect ratios
        const uint minDimension = 1;
        uint width = (uint)System.Math.Max(@event.Width, minDimension);
        uint height = (uint)System.Math.Max(@event.Height, minDimension);

        _aspectRatio = (float)width / (float)height;

        // Validate result
        if (float.IsNaN(_aspectRatio) || float.IsInfinity(_aspectRatio))
        {
            Logger.Warn("[Camera] Invalid aspect ratio calculated, using fallback");
            _aspectRatio = 16.0f / 9.0f; // Fallback to common aspect ratio
        }

        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        return true;
    }
}