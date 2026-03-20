using System.Text.Json;
using Engine.Audio;
using Shouldly;

namespace Engine.Tests.Audio;

public class AudioEffectConfigTests
{
    [Fact]
    public void AudioEffectConfig_Serializes_RoundTrip()
    {
        var config = new AudioEffectConfig
        {
            Type = AudioEffectType.Reverb,
            Enabled = true,
            Amount = 0.7f
        };

        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<AudioEffectConfig>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Type.ShouldBe(AudioEffectType.Reverb);
        deserialized.Enabled.ShouldBeTrue();
        deserialized.Amount.ShouldBe(0.7f);
    }

    [Fact]
    public void AudioEffectConfig_DefaultValues_AreCorrect()
    {
        var config = new AudioEffectConfig();

        config.Type.ShouldBe(AudioEffectType.Reverb);
        config.Enabled.ShouldBeTrue();
        config.Amount.ShouldBe(0.5f);
    }
}
