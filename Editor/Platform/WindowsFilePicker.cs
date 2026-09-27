using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Editor.Platform;

[SupportedOSPlatform("windows")]
internal static class WindowsFilePicker
{
    public static string? PickFile(
        string title,
        string filter,
        string? initialPath = null)
    {
        string? result = null;
        var thread = new Thread(() => result = PickFileOnStaThread(title, filter, initialPath));
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }

    private static string? PickFileOnStaThread(string title, string filter, string? initialPath)
    {
        using var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = ResolveInitialDirectory(initialPath)
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private static string ResolveInitialDirectory(string? initialPath)
    {
        if (string.IsNullOrWhiteSpace(initialPath))
            return Environment.CurrentDirectory;

        if (Directory.Exists(initialPath))
            return initialPath;

        var dir = Path.GetDirectoryName(initialPath);
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir)
            ? dir
            : Environment.CurrentDirectory;
    }
}
