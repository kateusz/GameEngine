using ECS;
using Engine.Core.Input;
using Engine.Events;
using Engine.Renderer.Cameras;
using Silk.NET.GLFW;

namespace Editor.Components;

public class EditorInputHandler
{
    private readonly OrthographicCameraController _cameraController;
    private bool _viewportFocused;

    public EditorInputHandler(OrthographicCameraController cameraController)
    {
        _cameraController = cameraController;
    }

    public bool ViewportFocused
    {
        get => _viewportFocused;
        set => _viewportFocused = value;
    }

    public OrthographicCameraController CameraController => _cameraController;

    public void HandleEvent(Event @event, Engine.Scene.SceneState sceneState)
    {
        // Always handle camera controller events in edit mode
        if (sceneState == Engine.Scene.SceneState.Edit)
        {
            _cameraController.OnEvent(@event);
        }
        else
        {
            Engine.Scripting.ScriptEngine.Instance.ProcessEvent(@event);
        }

        if (@event is KeyPressedEvent keyPressedEvent)
        {
            HandleKeyPressed(keyPressedEvent);
        }
        else if (@event is MouseButtonPressedEvent mouseButtonPressedEvent)
        {
            HandleMouseButtonPressed(mouseButtonPressedEvent);
        }
    }

    private void HandleKeyPressed(KeyPressedEvent keyPressedEvent)
    {
        // Shortcuts
        if (!keyPressedEvent.IsRepeat)
            return;

        var control = InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftControl) ||
                       InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.RightControl);
        var shift = InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.LeftShift) ||
                     InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.RightShift);
        
        switch (keyPressedEvent.KeyCode)
        {
            case (int)KeyCodes.N when control:
                OnNewSceneRequested?.Invoke();
                break;
            case (int)KeyCodes.O when control:
                OnOpenSceneRequested?.Invoke();
                break;
            case (int)KeyCodes.S when control:
                OnSaveSceneRequested?.Invoke();
                break;
            case (int)KeyCodes.D when control:
                OnDuplicateEntityRequested?.Invoke();
                break;
            case (int)KeyCodes.F when control:
                OnFocusOnSelectedRequested?.Invoke();
                break;
        }
    }

    private void HandleMouseButtonPressed(MouseButtonPressedEvent e)
    {
        if (e.Button == (int)MouseButton.Left)
        {
            OnLeftMousePressed?.Invoke();
        }
    }

    public void OnUpdate(TimeSpan timeSpan, Engine.Scene.SceneState sceneState)
    {
        // Update camera controller when viewport is focused and in edit mode
        if (_viewportFocused && sceneState == Engine.Scene.SceneState.Edit)
        {
            _cameraController.OnUpdate(timeSpan);
        }
    }

    public void UpdateCameraAspectRatio(float aspectRatio)
    {
        var currentCamera = _cameraController.Camera;
        var newController = new OrthographicCameraController(currentCamera, aspectRatio, true);
        
        // Copy the camera controller's camera reference
        // Note: This might need adjustment based on your OrthographicCameraController implementation
        OnCameraControllerUpdated?.Invoke(newController);
    }

    public void FocusOnEntity(Entity? entity)
    {
        if (entity != null && entity.HasComponent<Engine.Scene.Components.TransformComponent>())
        {
            var transform = entity.GetComponent<Engine.Scene.Components.TransformComponent>();
            _cameraController.Camera.SetPosition(transform.Translation);
        }
    }

    public void ResetCamera()
    {
        _cameraController.Camera.SetPosition(System.Numerics.Vector3.Zero);
        _cameraController.Camera.SetRotation(0.0f);
    }

    // Events for decoupling input handling from business logic
    public event Action? OnNewSceneRequested;
    public event Action? OnOpenSceneRequested;
    public event Action? OnSaveSceneRequested;
    public event Action? OnDuplicateEntityRequested;
    public event Action? OnFocusOnSelectedRequested;
    public event Action? OnLeftMousePressed;
    public event Action<OrthographicCameraController>? OnCameraControllerUpdated;
}