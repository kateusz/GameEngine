using System.Runtime.InteropServices;
using ImGuiNET;

namespace Editor.UI.Drawers;

/// <summary>
/// Utility class for handling ImGui drag-and-drop operations.
/// Centralizes common drag-drop patterns to reduce code duplication across the editor.
/// </summary>
public static class DragDropDrawer
{
    /// <summary>
    /// Standard payload type for content browser items.
    /// </summary>
    public const string ContentBrowserItemPayload = "CONTENT_BROWSER_ITEM";

    /// <summary>
    /// Creates a drag-drop source with payload data.
    /// This simplifies creating drag sources by handling BeginDragDropSource/SetDragDropPayload/EndDragDropSource.
    /// </summary>
    /// <param name="dragDropId">Unique identifier for the drag-drop payload type</param>
    /// <param name="payloadData">Data to attach to the payload (typically a string path)</param>
    /// <param name="dragPreview">Optional action to render custom drag preview (e.g., text, icon)</param>
    /// <returns>True if drag source was successfully created</returns>
    public static unsafe bool CreateDragDropSource(string dragDropId, string payloadData, Action? dragPreview = null)
    {
        if (!ImGui.BeginDragDropSource())
            return false;

        // Convert string to UTF-8 bytes (ImGui expects UTF-8)
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(payloadData + '\0'); // null-terminated

        fixed (byte* bytesPtr = utf8Bytes)
        {
            // ImGui copies the data internally, so it's safe to unpin after this call
            ImGui.SetDragDropPayload(dragDropId, (IntPtr)bytesPtr, (uint)utf8Bytes.Length);
        }

        // Render custom preview or default text
        if (dragPreview != null)
            dragPreview();
        else
            ImGui.Text(Path.GetFileName(payloadData));

        ImGui.EndDragDropSource();
        return true;
    }

    /// <summary>
    /// Handles a drag-drop target for file drops with validation.
    /// </summary>
    /// <param name="payloadType">Type of payload to accept (default: CONTENT_BROWSER_ITEM)</param>
    /// <param name="validator">Optional function to validate the dropped file path</param>
    /// <param name="onDropped">Callback invoked with the dropped file path when validation passes</param>
    /// <returns>True if a valid item was dropped</returns>
    public unsafe static bool HandleFileDropTarget(
        string payloadType,
        Func<string, bool>? validator,
        Action<string> onDropped)
    {
        if (!ImGui.BeginDragDropTarget())
            return false;

        var itemDropped = false;

        var payload = ImGui.AcceptDragDropPayload(payloadType);
        if (payload.NativePtr != null)
        {
            var path = ExtractStringFromPayload(payload.Data);
            if (path != null)
            {
                if (validator == null || validator(path))
                {
                    onDropped(path);
                    itemDropped = true;
                }
            }
        }

        ImGui.EndDragDropTarget();
        return itemDropped;
    }
    
    /// <summary>
    /// Creates a file extension validator for common asset types.
    /// </summary>
    /// <param name="extensions">Allowed file extensions (e.g., ".png", ".jpg")</param>
    /// <param name="checkFileExists">If true, validates that the file exists on disk</param>
    /// <returns>A validator function for use with HandleFileDropTarget</returns>
    public static Func<string, bool> CreateExtensionValidator(string[] extensions, bool checkFileExists = false)
    {
        return path =>
        {
            if (checkFileExists && !File.Exists(path))
                return false;

            return extensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        };
    }

    /// <summary>
    /// Extracts a string from an ImGui payload.
    /// </summary>
    /// <param name="payload">The payload data pointer</param>
    /// <returns>The extracted string or null if extraction failed</returns>
    public static string? ExtractStringFromPayload(IntPtr payload)
    {
        if (payload == IntPtr.Zero)
            return null;

        // Decode UTF-8 null-terminated string
        return Marshal.PtrToStringUTF8(payload);
    }

    /// <summary>
    /// Validates that a file path has one of the specified extensions.
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <param name="extensions">Allowed extensions (case-insensitive)</param>
    /// <returns>True if the path has a valid extension</returns>
    public static bool HasValidExtension(string path, params string[] extensions)
    {
        return extensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates that a file exists and has one of the specified extensions.
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <param name="extensions">Allowed extensions (case-insensitive)</param>
    /// <returns>True if the file exists and has a valid extension</returns>
    public static bool IsValidFile(string path, params string[] extensions)
    {
        return File.Exists(path) && HasValidExtension(path, extensions);
    }
}
