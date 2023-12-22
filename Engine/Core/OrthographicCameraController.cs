using Engine.Core.Input;
using Engine.Events;
using Engine.Renderer;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Core;

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

    public OrthographicCamera Camera { get; }

    public void OnUpdate(TimeSpan timeSpan)
    {
        if (InputState.KeyboardState.IsKeyDown(Keys.A))
            _cameraPosition.X -= _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;
        else if (InputState.KeyboardState.IsKeyDown(Keys.D))
            _cameraPosition.X += _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;
        else if (InputState.KeyboardState.IsKeyDown(Keys.S))
            _cameraPosition.Y -= _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;
        else if (InputState.KeyboardState.IsKeyDown(Keys.W))
            _cameraPosition.Y += _cameraTranslationSpeed * (float)timeSpan.TotalSeconds;

        if (_rotation)
        {
            if (InputState.KeyboardState.IsKeyDown(Keys.Q))
                _cameraRotation += _cameraRotationSpeed * (float)timeSpan.TotalSeconds;
            else if (InputState.KeyboardState.IsKeyDown(Keys.E))
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
        _zoomLevel = Math.Max(_zoomLevel, 0.25f);
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