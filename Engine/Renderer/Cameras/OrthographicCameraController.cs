using System.Numerics;
using Engine.Core.Input;
using Engine.Events;

namespace Engine.Renderer.Cameras;

public class OrthographicCameraController
{
    private float _aspectRatio;
    private readonly bool _rotation;
    private Vector3 _cameraPosition = Vector3.Zero;
    private float _cameraTranslationSpeed = 1.0f;
    private float _cameraRotation;
    private float _cameraRotationSpeed = 10.0f;
    private float _zoomLevel = 1.0f;

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
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.A))
            _cameraPosition.X -= _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;
        else if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.D))
            _cameraPosition.X += _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;
        else if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.S))
            _cameraPosition.Y -= _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;
        else if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.W))
            _cameraPosition.Y += _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;

        if (_rotation)
        {
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.Q))
                _cameraRotation += _cameraRotationSpeed * (float)timeSpan.TotalSeconds;
            else if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.E))
                _cameraRotation -= _cameraRotationSpeed * (float)timeSpan.TotalSeconds;
            
            Camera.SetRotation(_cameraRotation);
        }
        
        Camera.SetPosition(_cameraPosition);

        _cameraTranslationSpeed = _zoomLevel; 
    }

    public void OnEvent(Event @event)
    {
        if (@event is MouseScrolledEvent mouseScrolledEvent)
            OnMouseScrolled(mouseScrolledEvent);
        
        if (@event is WindowResizeEvent windowResizeEvent)
            OnWindowResized(windowResizeEvent);
    }

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