using Engine.Platform;

namespace Editor.Platform;

/// <summary>
/// Cross-platform entry point for native folder selection.
/// On Windows uses the WinForms picker; elsewhere returns null.
/// </summary>
internal static class FolderPicker
{
    public static bool IsAvailable => OSInfo.IsWindows;

    public static string? PickFolder(string title = "Select Folder", string? initialPath = null)
    {
        if (!OSInfo.IsWindows)
            return null;

        return WindowsFolderPicker.PickFolder(title, initialPath);
    }
}
