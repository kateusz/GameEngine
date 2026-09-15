using Engine.Core;
using Engine.Project;

namespace Editor.Publisher;

public interface IGamePublisher
{
    Task<PublishResult> PublishAsync(
        PublishSettings settings,
        GameConfiguration gameConfig,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
