using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Audio;
using ImGuiNET;

namespace Editor.Panels.Elements;

/// <summary>
/// UI element that provides drag-and-drop functionality for audio files.
/// Allows users to drag audio files (.wav, .ogg) from the content browser onto audio properties.
/// </summary>
public class AudioDropTarget
{
    private readonly IAudioEngine _audioEngine;

    public AudioDropTarget(IAudioEngine audioEngine)
    {
        _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
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
            if (ImGui.Button(buttonLabel, new Vector2(-1, 0.0f)))
            {
                // Optional: Could add a file picker dialog here in the future
            }

            // Handle drag-and-drop
            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                    if (payload.NativePtr != null)
                    {
                        var path = Marshal.PtrToStringUni(payload.Data);
                        if (path is not null)
                        {
                            string audioPath = Path.Combine(AssetsManager.AssetsPath, path);

                            // Validate that the file exists and is a supported audio format
                            if (File.Exists(audioPath) && AudioClipFactory.IsSupportedFormat(audioPath))
                            {
                                try
                                {
                                    // Load the audio clip using the audio engine
                                    var audioClip = _audioEngine.LoadAudioClip(audioPath);
                                    onAudioChanged(audioClip);
                                }
                                catch (Exception ex)
                                {
                                    // Log error if audio loading fails
                                    Console.WriteLine($"Failed to load audio clip: {ex.Message}");
                                }
                            }
                        }
                    }

                    ImGui.EndDragDropTarget();
                }
            }
        });
    }
}
