namespace Editor.Platform;

/// <summary>
/// Stub so <see cref="FolderPicker"/> compiles on non-Windows builds.
/// </summary>
internal static class WindowsFolderPicker
{
    public static string? PickFolder(string title = "Select Folder", string? initialPath = null) => null;
}
