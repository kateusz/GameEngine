using Engine.Core;

namespace Editor.Publisher;

public interface IGamePublisher
{
    Task<PublishResult> PublishAsync(
        PublishSettings settings,
        GameConfiguration gameConfig,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
