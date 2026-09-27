namespace Editor.Platform;

/// <summary>
/// Stub so <see cref="FilePicker"/> compiles on non-Windows builds.
/// </summary>
internal static class WindowsFilePicker
{
    public static string? PickFile(
        string title,
        string filter,
        string? initialPath = null) => null;
}
