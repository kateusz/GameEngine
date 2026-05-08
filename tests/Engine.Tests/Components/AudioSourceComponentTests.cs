using System.Text.Json;
using SceneComponents.Audio;
using Shouldly;

namespace Engine.Tests.Components;

public class AudioSourceComponentTests
{
    [Fact]
    public void AudioSourceComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new AudioSourceComponent();

        // Assert
        component.Volume.ShouldBe(1.0f);
        component.Pitch.ShouldBe(1.0f);
        component.Loop.ShouldBeFalse();
        component.PlayOnAwake.ShouldBeFalse();
        component.Is3D.ShouldBeTrue();
        component.MinDistance.ShouldBe(1.0f);
        component.MaxDistance.ShouldBe(100.0f);
    }

    [Fact]
    public void AudioSourceComponent_ParameterizedConstructor_ShouldSetAllProperties()
    {
        // Act
        var component = new AudioSourceComponent
        {
            AudioClipPath = "audio/test.wav",
            Volume = 0.8f,
            Pitch = 1.2f,
            Loop = true,
            PlayOnAwake = true,
            Is3D = false,
            MinDistance = 5.0f,
            MaxDistance = 50.0f
        };

        // Assert
        component.AudioClipPath.ShouldBe("audio/test.wav");
        component.Volume.ShouldBe(0.8f);
        component.Pitch.ShouldBe(1.2f);
        component.Loop.ShouldBeTrue();
        component.PlayOnAwake.ShouldBeTrue();
        component.Is3D.ShouldBeFalse();
        component.MinDistance.ShouldBe(5.0f);
        component.MaxDistance.ShouldBe(50.0f);
    }

    [Fact]
    public void AudioSourceComponent_SetVolume_ShouldAcceptValidRange()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act & Assert
        component.Volume = 0.0f;
        component.Volume.ShouldBe(0.0f);

        component.Volume = 0.5f;
        component.Volume.ShouldBe(0.5f);

        component.Volume = 1.0f;
        component.Volume.ShouldBe(1.0f);
    }

    [Fact]
    public void AudioSourceComponent_SetPitch_ShouldUpdateValue()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act
        component.Pitch = 2.0f;

        // Assert
        component.Pitch.ShouldBe(2.0f);
    }

    [Fact]
    public void AudioSourceComponent_Set3DProperties_ShouldUpdateValues()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act
        component.MinDistance = 10.0f;
        component.MaxDistance = 200.0f;

        // Assert
        component.MinDistance.ShouldBe(10.0f);
        component.MaxDistance.ShouldBe(200.0f);
    }

    [Fact]
    public void AudioSourceComponent_Clone_ShouldCopyAllProperties()
    {
        // Arrange
        var original = new AudioSourceComponent
        {
            Volume = 0.7f,
            Pitch = 1.5f,
            Loop = true,
            PlayOnAwake = false,
            Is3D = true,
            MinDistance = 2.0f,
            MaxDistance = 150.0f,
            AudioClipPath = "audio/test-clip.wav"
        };

        // Act
        var clone = (AudioSourceComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Volume.ShouldBe(0.7f);
        clone.Pitch.ShouldBe(1.5f);
        clone.Loop.ShouldBeTrue();
        clone.PlayOnAwake.ShouldBeFalse();
        clone.Is3D.ShouldBeTrue();
        clone.MinDistance.ShouldBe(2.0f);
        clone.MaxDistance.ShouldBe(150.0f);
        clone.AudioClipPath.ShouldBe("audio/test-clip.wav");
    }

    [Fact]
    public void AudioSourceComponent_AudioClipPath_ShouldReturnNullWhenNoClip()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act
        var path = component.AudioClipPath;

        // Assert
        path.ShouldBeNull();
    }

    [Fact]
    public void AudioSourceComponent_AudioClipPath_ShouldReturnPathWhenExplicitlySet()
    {
        var component = new AudioSourceComponent
        {
            AudioClipPath = "audio/test.wav"
        };

        component.AudioClipPath.ShouldBe("audio/test.wav");
    }

    [Fact]
    public void AudioSourceComponent_ShouldSerializeWithoutRuntimeState()
    {
        var component = new AudioSourceComponent
        {
            AudioClipPath = "audio/test.wav",
            Volume = 0.5f
        };
        var json = JsonSerializer.Serialize(component);
        json.ShouldContain("AudioClipPath");
        json.ShouldContain("Volume");
    }
}