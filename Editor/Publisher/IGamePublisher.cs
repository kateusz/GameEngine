using Engine.Core;

namespace Editor.Publisher;

/// <summary>
/// Interface for publishing/building game projects.
/// Handles compilation and asset packaging for distribution.
/// </summary>
public interface IGamePublisher
{
    Task<PublishResult> PublishAsync(
        PublishSettings settings,
        GameConfiguration gameConfig,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
