using SceneComponents.Audio;

namespace Engine.Audio;

internal interface IAudioEffectFactory
{
    IAudioEffect CreateEffect(AudioEffectType type);
}
