using Engine.Events;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Interface for controlling an orthographic camera with keyboard/mouse input.
/// Handles camera movement, rotation, and zooming.
/// </summary>
public interface IOrthographicCameraController
{
    /// <summary>
    /// Gets the controlled orthographic camera.
    /// </summary>
    OrthographicCamera Camera { get; }

    /// <summary>
    /// Updates the camera controller, processing input and updating camera position.
    /// </summary>
    /// <param name="timeSpan">Time elapsed since last update</param>
    void OnUpdate(TimeSpan timeSpan);

    /// <summary>
    /// Processes input events (keyboard, mouse scroll, window resize).
    /// </summary>
    /// <param name="event">Event to process</param>
    void OnEvent(Event @event);

    /// <summary>
    /// Sets the camera movement speed multiplier.
    /// </summary>
    /// <param name="multiplier">Speed multiplier value</param>
    void SetSpeedMultiplier(float multiplier);

    /// <summary>
    /// Gets the current camera movement speed multiplier.
    /// </summary>
    /// <returns>Speed multiplier value</returns>
    float GetSpeedMultiplier();
}
