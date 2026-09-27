using Audio;
using Editor.AssetPicker;
using Engine.Project;
using Serilog;

namespace Editor.UI.Elements;

/// <summary>
/// Audio clip field: opens Asset Browser; Content Browser drop still supported.
/// Drop validation uses AssetKind.Audio.Extensions (shared source of truth);
/// AudioClipFactory stays only at load time (runtime audio detection).
/// </summary>
public class AudioDropTarget(IAudio audio, AssetPathField assetPathField)
{
    public void Draw(string label, Action<string> onAudioPathChanged, string? currentAudioPath = null) =>
        assetPathField.Draw(
            label,
            AssetKind.Audio,
            currentAudioPath,
            relative => TryAssign(relative, onAudioPathChanged));

    private void TryAssign(string relativePath, Action<string> onAudioPathChanged)
    {
        var audioPath = PathBuilder.Resolve(relativePath);
        try
        {
            audio.LoadAudioClip(audioPath);
            onAudioPathChanged(relativePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load audio clip from {Path}", audioPath);
        }
    }
}
