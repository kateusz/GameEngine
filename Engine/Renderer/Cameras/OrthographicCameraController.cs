using System.Collections.Concurrent;
using System.Numerics;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using Serilog;

namespace Engine.Renderer.Cameras;

public class OrthographicCameraController : IOrthographicCameraController
{
    private static readonly ILogger Logger = Log.ForContext<OrthographicCameraController>();
    
    private float _aspectRatio;
    private readonly bool _rotation;
    private Vector3 _cameraPosition = Vector3.Zero;
    private readonly float _cameraTranslationSpeed = CameraConfig.DefaultTranslationSpeed;
    private readonly float _cameraRotationSpeed = CameraConfig.DefaultRotationSpeed;
    private float _cameraRotation;
    private float _zoomLevel = CameraConfig.DefaultZoomLevel;
    
    // Add a speed multiplier for better control
    private float _speedMultiplier = CameraConfig.DefaultSpeedMultiplier;

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
    
    public float ZoomLevel => _zoomLevel;

    public void SetZoom(float zoom)
    {
        _zoomLevel = zoom;
        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
    }

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
        _zoomLevel += @event.YOffset * CameraConfig.ZoomSensitivity;
        _zoomLevel = System.Math.Max(_zoomLevel, CameraConfig.MinZoomLevel);
        _zoomLevel = System.Math.Min(_zoomLevel, CameraConfig.MaxZoomLevel);
        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        return true;
    }

    private bool OnWindowResized(WindowResizeEvent @event)
    {
        // Validate dimensions
        if (@event.Width == 0 || @event.Height == 0)
        {
            Logger.Warning("[Camera] Invalid window dimensions: {Width}x{Height}, ignoring resize", @event.Width, @event.Height);
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
            Logger.Warning("[Camera] Invalid aspect ratio calculated, using fallback");
            _aspectRatio = CameraConfig.DefaultAspectRatio;
        }

        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        return true;
    }
}