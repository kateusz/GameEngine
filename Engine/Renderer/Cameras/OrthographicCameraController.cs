using System.Numerics;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;

namespace Engine.Renderer.Cameras;

public class OrthographicCameraController
{
    private float _aspectRatio;
    private readonly bool _rotation;
    private Vector3 _cameraPosition = Vector3.Zero;
    private float _cameraTranslationSpeed = 0.5f;
    private float _cameraRotationSpeed = 10.0f;
    private float _cameraRotation;
    private float _zoomLevel = 20.0f;
    
    // Add a speed multiplier for better control
    private float _speedMultiplier = 0.1f; // Adjust this to make camera slower/faster
    
    // TODO: check concurrency
    private readonly HashSet<KeyCodes> _pressedKeys = [];

    public OrthographicCameraController(float aspectRatio, bool rotation = false)
    {
        _aspectRatio = aspectRatio;
        Camera = new OrthographicCamera(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        _aspectRatio = aspectRatio;
        _rotation = rotation;
    }

    public OrthographicCameraController(OrthographicCamera camera, float aspectRatio, bool rotation = false)
    {
        _aspectRatio = aspectRatio;
        Camera = camera;
        _aspectRatio = aspectRatio;
        _rotation = rotation;
    }

    public OrthographicCamera Camera { get; }

    public void OnUpdate(TimeSpan timeSpan)
    {
        // Calculate actual movement speed based on zoom level but with a reasonable multiplier
        float actualSpeed = _cameraTranslationSpeed * _speedMultiplier * _zoomLevel;
        
        if (_pressedKeys.Contains(KeyCodes.A))
            _cameraPosition.X -= actualSpeed * (float)timeSpan.TotalSeconds;
        else if (_pressedKeys.Contains(KeyCodes.D))
            _cameraPosition.X += actualSpeed * (float)timeSpan.TotalSeconds;
        else if (_pressedKeys.Contains(KeyCodes.S))
            _cameraPosition.Y -= actualSpeed * (float)timeSpan.TotalSeconds;
        else if (_pressedKeys.Contains(KeyCodes.W))
            _cameraPosition.Y += actualSpeed * (float)timeSpan.TotalSeconds;

        if (_rotation)
        {
            if (_pressedKeys.Contains(KeyCodes.Q))
                _cameraRotation += _cameraRotationSpeed * (float)timeSpan.TotalSeconds;
            else if (_pressedKeys.Contains(KeyCodes.E))
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
                _pressedKeys.Add((KeyCodes)kpe.KeyCode);
                break;
            case KeyReleasedEvent kre:
                _pressedKeys.Remove((KeyCodes)kre.KeyCode);
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
        _zoomLevel = @event.YOffset * 0.25f;
        _zoomLevel = System.Math.Max(_zoomLevel, 0.25f);
        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        return true;
    }

    private bool OnWindowResized(WindowResizeEvent @event)
    {
        _aspectRatio = (float)@event.Width / (float)@event.Height;
        Camera.SetProjection(-_aspectRatio * _zoomLevel, _aspectRatio * _zoomLevel, -_zoomLevel, _zoomLevel);
        return true;
    }
}