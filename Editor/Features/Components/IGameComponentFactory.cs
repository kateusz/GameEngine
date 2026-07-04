using ECS;

namespace Editor.Features.Components;

public interface IGameComponentFactory
{
    string[] DiscoverComponentNames();

    Task<(bool Success, string? Error)> CreateFileAsync(string baseName);

    Task<(bool Success, string? Error)> CreateAndAttachAsync(Entity entity, string baseName);

    (bool Success, string? Error) AttachExisting(Entity entity, string typeName);
}
