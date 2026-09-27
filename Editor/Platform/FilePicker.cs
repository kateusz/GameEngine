using Engine.Platform;

namespace Editor.Platform;

/// <summary>
/// Cross-platform entry point for native file selection.
/// On Windows uses the WinForms OpenFileDialog; elsewhere returns null.
/// </summary>
internal static class FilePicker
{
    public static bool IsAvailable => OSInfo.IsWindows;

    public static string? PickFile(
        string title,
        string filter,
        string? initialPath = null)
    {
        if (!OSInfo.IsWindows)
            return null;

        return WindowsFilePicker.PickFile(title, filter, initialPath);
    }
}
