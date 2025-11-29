using ECS.Systems;
using Engine.Renderer.Cameras;
using Serilog;

namespace Editor.Systems;

/// <summary>
/// System responsible for updating the editor camera controller.
/// This system only runs when the viewport is focused in Edit mode.
/// </summary>
public class EditorCameraSystem(IOrthographicCameraController cameraController) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<EditorCameraSystem>();

    private IOrthographicCameraController _cameraController = cameraController;
    private bool _isViewportFocused;

    /// <summary>
    /// Priority of 50 ensures this system updates before rendering systems.
    /// </summary>
    public int Priority => 50;

    /// <summary>
    /// Sets the camera controller to update.
    /// This allows rebinding the controller when the viewport is resized.
    /// </summary>
    /// <param name="cameraController">The new camera controller to update.</param>
    public void SetCameraController(IOrthographicCameraController cameraController)
    {
        _cameraController = cameraController ?? throw new ArgumentNullException(nameof(cameraController));
        Logger.Debug("Camera controller updated");
    }

    /// <summary>
    /// Sets whether the viewport is currently focused.
    /// The camera controller only updates when the viewport is focused.
    /// </summary>
    /// <param name="focused">True if the viewport is focused, false otherwise.</param>
    public void SetViewportFocused(bool focused)
    {
        _isViewportFocused = focused;
    }

    /// <summary>
    /// Initializes the editor camera system.
    /// </summary>
    public void OnInit()
    {
        Logger.Debug("EditorCameraSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Updates the camera controller when the viewport is focused.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        if (_isViewportFocused && _cameraController != null)
        {
            _cameraController.OnUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Cleans up the editor camera system.
    /// </summary>
    public void OnShutdown()
    {
        // No cleanup required
    }
}
