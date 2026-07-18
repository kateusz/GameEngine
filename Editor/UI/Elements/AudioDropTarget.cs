using Audio;
using Editor.UI.Drawers;
using Engine.Core;
using Serilog;

namespace Editor.UI.Elements;

/// <summary>
/// UI element that provides drag-and-drop functionality for audio files.
/// Allows users to drag audio files (.wav, .ogg) from the content browser onto audio properties.
/// </summary>
public class AudioDropTarget(IAudio audio)
{
    /// <summary>
    /// Draws a drag-and-drop target button for audio clips.
    /// </summary>
    /// <param name="label">Label to display for the property</param>
    /// <param name="onAudioPathChanged">Callback invoked with the dropped path when a new audio clip is assigned</param>
    /// <param name="currentAudioPath">Currently assigned audio path (can be null)</param>
    public void Draw(string label, Action<string> onAudioPathChanged, string? currentAudioPath = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentAudioPath)
                ? Path.GetFileName(currentAudioPath)
                : "None (Drop audio here)";

            ButtonDrawer.DrawFullWidthButton(buttonLabel, () =>
            {
                // Optional: Could add a file picker popup here in the future
            });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path =>
                {
                    var audioPath = PathBuilder.Build(path);
                    return File.Exists(audioPath) && AudioClipFactory.IsSupportedFormat(audioPath);
                },
                path =>
                {
                    var audioPath = PathBuilder.Build(path);
                    try
                    {
                        audio.LoadAudioClip(audioPath);
                        onAudioPathChanged(PathBuilder.ToAssetRelativePath(path));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to load audio clip from {Path}", audioPath);
                    }
                });
        });
    }
}
