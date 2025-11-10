namespace Editor.Publisher;

/// <summary>
/// Interface for publishing/building game projects.
/// Handles compilation and asset packaging for distribution.
/// </summary>
public interface IGamePublisher
{
    /// <summary>
    /// Publishes the game, building the runtime and copying necessary assets.
    /// </summary>
    void Publish();
}
