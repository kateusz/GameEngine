namespace Editor.Publisher;

/// <summary>
/// Interface for publishing/building game projects.
/// Handles compilation and asset packaging for distribution.
/// </summary>
public interface IGamePublisher
{
    /// <summary>
    /// Publishes the game, building the runtime and copying necessary assets.
    /// Legacy synchronous method for backward compatibility.
    /// </summary>
    void Publish();
    
    /// <summary>
    /// Publishes the game asynchronously with the specified settings.
    /// </summary>
    /// <param name="settings">Publish configuration settings.</param>
    /// <param name="progress">Optional progress reporter for UI feedback.</param>
    /// <param name="cancellationToken">Optional cancellation token for cancellable builds.</param>
    /// <returns>A result indicating success or failure with relevant details.</returns>
    Task<PublishResult> PublishAsync(
        PublishSettings settings,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the game asynchronously with game configuration.
    /// </summary>
    /// <param name="settings">Publish configuration settings.</param>
    /// <param name="gameConfig">Game runtime configuration (startup scene, window settings, etc.).</param>
    /// <param name="progress">Optional progress reporter for UI feedback.</param>
    /// <param name="cancellationToken">Optional cancellation token for cancellable builds.</param>
    /// <returns>A result indicating success or failure with relevant details.</returns>
    Task<PublishResult> PublishAsync(
        PublishSettings settings,
        GameConfiguration gameConfig,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
