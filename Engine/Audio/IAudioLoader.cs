namespace Engine.Audio;

public interface IAudioLoader
{
    bool CanLoad(string path);
    AudioData Load(string path);
}

