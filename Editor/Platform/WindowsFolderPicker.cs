using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Editor.Platform;

[SupportedOSPlatform("windows")]
internal static class WindowsFolderPicker
{
    public static string? PickFolder(string title = "Select Project Folder", string? initialPath = null)
    {
        string? result = null;
        var thread = new Thread(() => result = PickFolderOnStaThread(title, initialPath));
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }

    private static string? PickFolderOnStaThread(string title, string? initialPath)
    {
        var dialog = new FolderBrowserEx.FolderBrowserDialog
        {
            Title = title,
            InitialFolder = initialPath ?? Environment.CurrentDirectory,
            DefaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            AllowMultiSelect = false
        };

        try
        {
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedFolder : null;
        }
        finally
        {
            dialog.Dispose();
        }
    }
}
