using System.Numerics;
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

    float ZoomLevel { get; }

    void SetZoom(float zoom);

    /// <summary>
    /// Sets the camera position and synchronizes the controller's internal position state.
    /// Use this method when programmatically moving the camera (e.g., focusing on an entity).
    /// </summary>
    void SetPosition(Vector3 position);

    /// <summary>
    /// Sets the camera rotation and synchronizes the controller's internal rotation state.
    /// </summary>
    void SetRotation(float rotation);
}
