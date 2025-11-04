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

        // Update camera projection to match the new aspect ratio
        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);

        Logger.Debug(
            "[CameraController] AspectRatio={AspectRatio:F3}, Zoom={ZoomLevel:F1}, Bounds=[{F:F1}, {AspectRatio1:F1}] x [{F1:F1}, {ZoomLevel1:F1}]"
            , aspectRatio, _zoomLevel, -_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
    }

    public OrthographicCamera Camera { get; }

    public float ZoomLevel => _zoomLevel;

    public void SetZoom(float zoom)
    {
        _zoomLevel = zoom;
        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
    }

    /// <summary>
    /// Sets the camera position and synchronizes the controller's internal position state.
    /// Use this method when programmatically moving the camera (e.g., focusing on an entity).
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        _cameraPosition = position;
        Camera.SetPosition(position);
    }

    /// <summary>
    /// Sets the camera rotation and synchronizes the controller's internal rotation state.
    /// </summary>
    public void SetRotation(float rotation)
    {
        _cameraRotation = rotation;
        Camera.SetRotation(rotation);
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        // Calculate actual movement speed based on zoom level but with a reasonable multiplier
        var actualSpeed = _cameraTranslationSpeed * _speedMultiplier * _zoomLevel;

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
            Logger.Warning("[Camera] Invalid window dimensions: {Width}x{Height}, ignoring resize", @event.Width,
                @event.Height);
            return false;
        }

        // Ensure minimum dimensions to prevent extreme aspect ratios
        const uint minDimension = 1;
        var width = (uint)System.Math.Max(@event.Width, minDimension);
        var height = (uint)System.Math.Max(@event.Height, minDimension);

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