namespace Editor.Features.Settings;

/// <summary>
/// Represents a recently opened project with metadata.
/// </summary>
public record RecentProject
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required DateTime LastOpened { get; init; }
}