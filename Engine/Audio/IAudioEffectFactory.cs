using SceneComponents.Audio;

namespace Engine.Audio;

public interface IAudioEffectFactory
{
    IAudioEffect CreateEffect(AudioEffectType type);
}
