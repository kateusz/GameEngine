using System.Runtime.InteropServices;

namespace Benchmark.Platform;

/// <summary>
/// Native folder selection on Windows; null elsewhere (use path text field).
/// </summary>
internal static class FolderPicker
{
    public static bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static string? PickFolder(string title = "Select Project Folder", string? initialPath = null)
    {
        if (!IsAvailable)
            return null;

        return WindowsFolderPicker.PickFolder(title, initialPath);
    }
}
