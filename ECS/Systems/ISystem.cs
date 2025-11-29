namespace ECS.Systems;

/// <summary>
/// Defines the contract for a system in the Entity Component System.
/// Systems contain logic that operates on entities with specific component combinations.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Gets the priority of this system, which determines its execution order.
    /// Systems are executed in ascending priority order (lower values execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Called once when the system is registered and initialized.
    /// Use this method to perform any setup required by the system.
    /// </summary>
    void OnInit();

    /// <summary>
    /// Called every frame to update the system logic.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    void OnUpdate(TimeSpan deltaTime);

    /// <summary>
    /// Called when the system is being shut down.
    /// Use this method to perform cleanup and release resources.
    /// </summary>
    void OnShutdown();
}
