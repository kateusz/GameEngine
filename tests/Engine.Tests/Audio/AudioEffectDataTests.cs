using System.Text.Json;
using Audio;
using SceneComponents.Audio;
using Shouldly;

namespace Engine.Tests.Audio;

public class AudioEffectDataTests
{
    [Fact]
    public void AudioEffectData_Serializes_RoundTrip()
    {
        var data = new AudioEffectData
        {
            Type = AudioEffectType.Reverb,
            Enabled = true,
            Amount = 0.7f
        };

        var json = JsonSerializer.Serialize(data);
        var deserialized = JsonSerializer.Deserialize<AudioEffectData>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Type.ShouldBe(AudioEffectType.Reverb);
        deserialized.Enabled.ShouldBeTrue();
        deserialized.Amount.ShouldBe(0.7f);
    }

    [Fact]
    public void AudioEffectData_DefaultValues_AreCorrect()
    {
        var data = new AudioEffectData();

        data.Type.ShouldBe(AudioEffectType.Reverb);
        data.Enabled.ShouldBeTrue();
        data.Amount.ShouldBe(0.5f);
    }

    [Fact]
    public void AudioSourceComponent_Clone_DeepCopiesEffects()
    {
        var original = new AudioSourceComponent
        {
            Volume = 0.8f,
            Effects =
            [
                new AudioEffectData { Type = AudioEffectType.Reverb, Amount = 0.7f },
                new AudioEffectData { Type = AudioEffectType.LowPass, Amount = 0.3f }
            ]
        };

        var clone = (AudioSourceComponent)original.Clone();

        clone.Effects[0].Amount = 0.1f;
        clone.Effects.RemoveAt(1);

        clone.Effects[0].Amount.ShouldBe(0.1f);
        clone.Effects.Count.ShouldBe(1);
        original.Effects[0].Amount.ShouldBe(0.7f);
        original.Effects.Count.ShouldBe(2);
    }
}
