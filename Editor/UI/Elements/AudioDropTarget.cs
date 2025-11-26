using Editor.UI.Drawers;
using Engine;
using Engine.Audio;
using Serilog;

namespace Editor.UI.Elements;

/// <summary>
/// UI element that provides drag-and-drop functionality for audio files.
/// Allows users to drag audio files (.wav, .ogg) from the content browser onto audio properties.
/// </summary>
public class AudioDropTarget
{
    private readonly IAudioEngine _audioEngine;
    private readonly IAssetsManager _assetsManager;

    public AudioDropTarget(IAudioEngine audioEngine, IAssetsManager assetsManager)
    {
        _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
        _assetsManager = assetsManager ?? throw new ArgumentNullException(nameof(assetsManager));
    }

    /// <summary>
    /// Draws a drag-and-drop target button for audio clips.
    /// </summary>
    /// <param name="label">Label to display for the property</param>
    /// <param name="currentClip">Currently assigned audio clip (can be null)</param>
    /// <param name="onAudioChanged">Callback invoked when a new audio clip is dropped</param>
    public void Draw(string label, IAudioClip? currentClip, Action<IAudioClip> onAudioChanged)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            // Display current audio clip name or "None" if no clip is assigned
            string buttonLabel = currentClip != null
                ? Path.GetFileName(currentClip.Path)
                : "None (Drop audio here)";

            // Button acts as the drop target
            ButtonDrawer.DrawFullWidthButton(buttonLabel, () =>
            {
                // Optional: Could add a file picker popup here in the future
            });

            // Handle drag-and-drop
            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path =>
                {
                    var audioPath = Path.Combine(_assetsManager.AssetsPath, path);
                    return File.Exists(audioPath) && AudioClipFactory.IsSupportedFormat(audioPath);
                },
                path =>
                {
                    var audioPath = Path.Combine(_assetsManager.AssetsPath, path);
                    try
                    {
                        var audioClip = _audioEngine.LoadAudioClip(audioPath);
                        onAudioChanged(audioClip);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to load audio clip from {Path}", audioPath);
                    }
                });
        });
    }
}
