namespace Engine.Audio;

/// <summary>
/// Interface for audio file loaders that can decode audio files into a standardized format.
/// </summary>
public interface IAudioLoader
{
    /// <summary>
    /// Determines whether this loader can handle the specified audio file.
    /// </summary>
    /// <param name="path">The absolute or project-relative path to the audio file.</param>
    /// <returns>True if this loader supports the file format; otherwise, false.</returns>
    bool CanLoad(string path);

    /// <summary>
    /// Loads and decodes the audio file into PCM or engine-native format.
    /// </summary>
    /// <param name="path">The absolute or project-relative path to the audio file.</param>
    /// <returns>Decoded audio data containing PCM samples and metadata.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="System.IO.InvalidDataException">Thrown when the file is corrupt or malformed.</exception>
    /// <exception cref="System.NotSupportedException">Thrown when the audio format is not supported by this loader.</exception>
    AudioData Load(string path);
}

