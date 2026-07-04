using ECS;

namespace Audio;

public interface IAudioPlayback
{
    void Play(Entity entity);
    void Pause(Entity entity);
    void Stop(Entity entity);
}
