using Bogus;
using Engine.Audio;
using Shouldly;

namespace Engine.Tests.Audio;

public class AudioUtilsTests
{
    private readonly Faker _faker = new();

    #region Decibel to Linear Conversion Tests

    [Fact]
    public void DecibelToLinear_ZeroDecibels_ShouldReturnOne()
    {
        // Act
        var result = AudioUtils.DecibelToLinear(0f);

        // Assert
        result.ShouldBe(1f, 0.0001f);
    }

    [Theory]
    [InlineData(-6f, 0.5012f)]   // -6dB is approximately half volume
    [InlineData(-12f, 0.2512f)]  // -12dB is approximately quarter volume
    [InlineData(-20f, 0.1f)]     // -20dB is 0.1x volume
    [InlineData(-40f, 0.01f)]    // -40dB is 0.01x volume
    [InlineData(6f, 1.9953f)]    // +6dB is approximately double volume
    [InlineData(20f, 10f)]       // +20dB is 10x volume
    public void DecibelToLinear_CommonValues_ShouldConvertCorrectly(float decibel, float expectedLinear)
    {
        // Act
        var result = AudioUtils.DecibelToLinear(decibel);

        // Assert
        result.ShouldBe(expectedLinear, 0.01f);
    }

    [Fact]
    public void DecibelToLinear_PositiveDecibels_ShouldReturnGreaterThanOne()
    {
        // Act
        var result = AudioUtils.DecibelToLinear(10f);

        // Assert
        result.ShouldBeGreaterThan(1f);
    }

    [Fact]
    public void DecibelToLinear_NegativeDecibels_ShouldReturnLessThanOne()
    {
        // Act
        var result = AudioUtils.DecibelToLinear(-10f);

        // Assert
        result.ShouldBeLessThan(1f);
        result.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void DecibelToLinear_VeryLargeNegative_ShouldApproachZero()
    {
        // Act
        var result = AudioUtils.DecibelToLinear(-100f);

        // Assert
        result.ShouldBeLessThan(0.0001f);
        result.ShouldBeGreaterThan(0f);
    }

    #endregion

    #region Linear to Decibel Conversion Tests

    [Fact]
    public void LinearToDecibel_One_ShouldReturnZeroDecibels()
    {
        // Act
        var result = AudioUtils.LinearToDecibel(1f);

        // Assert
        result.ShouldBe(0f, 0.0001f);
    }

    [Theory]
    [InlineData(0.5f, -6.0206f)]   // Half volume is approximately -6dB
    [InlineData(0.25f, -12.0412f)] // Quarter volume is approximately -12dB
    [InlineData(0.1f, -20f)]       // 0.1x is -20dB
    [InlineData(2f, 6.0206f)]      // Double volume is approximately +6dB
    [InlineData(10f, 20f)]         // 10x is +20dB
    public void LinearToDecibel_CommonValues_ShouldConvertCorrectly(float linear, float expectedDecibel)
    {
        // Act
        var result = AudioUtils.LinearToDecibel(linear);

        // Assert
        result.ShouldBe(expectedDecibel, 0.01f);
    }

    [Fact]
    public void LinearToDecibel_GreaterThanOne_ShouldReturnPositiveDecibels()
    {
        // Act
        var result = AudioUtils.LinearToDecibel(2f);

        // Assert
        result.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void LinearToDecibel_LessThanOne_ShouldReturnNegativeDecibels()
    {
        // Act
        var result = AudioUtils.LinearToDecibel(0.5f);

        // Assert
        result.ShouldBeLessThan(0f);
    }

    [Fact]
    public void LinearToDecibel_Zero_ShouldHandleGracefully()
    {
        // Act - Should clamp to minimum value (0.0001) to avoid log(0)
        var result = AudioUtils.LinearToDecibel(0f);

        // Assert - log10(0.0001) * 20 = -80dB
        result.ShouldBe(-80f, 0.01f);
    }

    [Fact]
    public void LinearToDecibel_VerySmallValue_ShouldClampToMinimum()
    {
        // Act
        var result = AudioUtils.LinearToDecibel(0.00001f);

        // Assert - Should be clamped to log10(0.0001) * 20
        result.ShouldBe(-80f, 0.01f);
    }

    #endregion

    #region Decibel Round-Trip Tests

    [Fact]
    public void DecibelToLinear_AndBack_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalDecibel = _faker.Random.Float(-40f, 20f);

        // Act
        var linear = AudioUtils.DecibelToLinear(originalDecibel);
        var backToDecibel = AudioUtils.LinearToDecibel(linear);

        // Assert
        backToDecibel.ShouldBe(originalDecibel, 0.001f);
    }

    [Fact]
    public void LinearToDecibel_AndBack_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalLinear = _faker.Random.Float(0.1f, 10f);

        // Act
        var decibel = AudioUtils.LinearToDecibel(originalLinear);
        var backToLinear = AudioUtils.DecibelToLinear(decibel);

        // Assert
        backToLinear.ShouldBe(originalLinear, 0.001f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-6f)]
    [InlineData(-12f)]
    [InlineData(6f)]
    [InlineData(12f)]
    public void DecibelConversions_RoundTrip_ShouldBeAccurate(float decibel)
    {
        // Act
        var linear = AudioUtils.DecibelToLinear(decibel);
        var backToDecibel = AudioUtils.LinearToDecibel(linear);

        // Assert
        backToDecibel.ShouldBe(decibel, 0.0001f);
    }

    #endregion

    #region Semitones to Pitch Conversion Tests

    [Fact]
    public void SemitonesToPitch_ZeroSemitones_ShouldReturnOne()
    {
        // Act
        var result = AudioUtils.SemitonesToPitch(0f);

        // Assert
        result.ShouldBe(1f, 0.0001f);
    }

    [Theory]
    [InlineData(12f, 2f)]      // One octave up is 2x pitch
    [InlineData(-12f, 0.5f)]   // One octave down is 0.5x pitch
    [InlineData(24f, 4f)]      // Two octaves up is 4x pitch
    [InlineData(-24f, 0.25f)]  // Two octaves down is 0.25x pitch
    [InlineData(7f, 1.4983f)]  // Perfect fifth (7 semitones)
    [InlineData(5f, 1.3348f)]  // Perfect fourth (5 semitones)
    public void SemitonesToPitch_CommonMusicalIntervals_ShouldConvertCorrectly(float semitones, float expectedPitch)
    {
        // Act
        var result = AudioUtils.SemitonesToPitch(semitones);

        // Assert
        result.ShouldBe(expectedPitch, 0.01f);
    }

    [Fact]
    public void SemitonesToPitch_PositiveSemitones_ShouldIncreasesPitch()
    {
        // Arrange
        var semitones = _faker.Random.Float(1f, 24f);

        // Act
        var result = AudioUtils.SemitonesToPitch(semitones);

        // Assert
        result.ShouldBeGreaterThan(1f);
    }

    [Fact]
    public void SemitonesToPitch_NegativeSemitones_ShouldDecreasePitch()
    {
        // Arrange
        var semitones = _faker.Random.Float(-24f, -1f);

        // Act
        var result = AudioUtils.SemitonesToPitch(semitones);

        // Assert
        result.ShouldBeLessThan(1f);
        result.ShouldBeGreaterThan(0f);
    }

    #endregion

    #region Pitch to Semitones Conversion Tests

    [Fact]
    public void PitchToSemitones_OnePitch_ShouldReturnZeroSemitones()
    {
        // Act
        var result = AudioUtils.PitchToSemitones(1f);

        // Assert
        result.ShouldBe(0f, 0.0001f);
    }

    [Theory]
    [InlineData(2f, 12f)]      // 2x pitch is one octave up
    [InlineData(0.5f, -12f)]   // 0.5x pitch is one octave down
    [InlineData(4f, 24f)]      // 4x pitch is two octaves up
    [InlineData(0.25f, -24f)]  // 0.25x pitch is two octaves down
    public void PitchToSemitones_CommonPitchValues_ShouldConvertCorrectly(float pitch, float expectedSemitones)
    {
        // Act
        var result = AudioUtils.PitchToSemitones(pitch);

        // Assert
        result.ShouldBe(expectedSemitones, 0.01f);
    }

    [Fact]
    public void PitchToSemitones_GreaterThanOne_ShouldReturnPositiveSemitones()
    {
        // Act
        var result = AudioUtils.PitchToSemitones(1.5f);

        // Assert
        result.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void PitchToSemitones_LessThanOne_ShouldReturnNegativeSemitones()
    {
        // Act
        var result = AudioUtils.PitchToSemitones(0.8f);

        // Assert
        result.ShouldBeLessThan(0f);
    }

    #endregion

    #region Pitch/Semitones Round-Trip Tests

    [Fact]
    public void SemitonesToPitch_AndBack_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalSemitones = _faker.Random.Float(-24f, 24f);

        // Act
        var pitch = AudioUtils.SemitonesToPitch(originalSemitones);
        var backToSemitones = AudioUtils.PitchToSemitones(pitch);

        // Assert
        backToSemitones.ShouldBe(originalSemitones, 0.001f);
    }

    [Fact]
    public void PitchToSemitones_AndBack_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalPitch = _faker.Random.Float(0.25f, 4f);

        // Act
        var semitones = AudioUtils.PitchToSemitones(originalPitch);
        var backToPitch = AudioUtils.SemitonesToPitch(semitones);

        // Assert
        backToPitch.ShouldBe(originalPitch, 0.001f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(12f)]
    [InlineData(-12f)]
    [InlineData(7f)]
    [InlineData(-7f)]
    public void PitchConversions_RoundTrip_ShouldBeAccurate(float semitones)
    {
        // Act
        var pitch = AudioUtils.SemitonesToPitch(semitones);
        var backToSemitones = AudioUtils.PitchToSemitones(pitch);

        // Assert
        backToSemitones.ShouldBe(semitones, 0.0001f);
    }

    #endregion

    #region Volume Validation Tests

    [Theory]
    [InlineData(0f, true)]
    [InlineData(0.5f, true)]
    [InlineData(1f, true)]
    [InlineData(-0.1f, false)]
    [InlineData(1.1f, false)]
    [InlineData(-1f, false)]
    [InlineData(2f, false)]
    public void IsValidVolume_VariousValues_ShouldValidateCorrectly(float volume, bool expectedValid)
    {
        // Act
        var result = AudioUtils.IsValidVolume(volume);

        // Assert
        result.ShouldBe(expectedValid);
    }

    [Fact]
    public void IsValidVolume_BoundaryValues_ShouldReturnTrue()
    {
        // Assert
        AudioUtils.IsValidVolume(0f).ShouldBeTrue();
        AudioUtils.IsValidVolume(1f).ShouldBeTrue();
    }

    [Fact]
    public void IsValidVolume_JustOutsideBounds_ShouldReturnFalse()
    {
        // Assert
        AudioUtils.IsValidVolume(-0.0001f).ShouldBeFalse();
        AudioUtils.IsValidVolume(1.0001f).ShouldBeFalse();
    }

    [Fact]
    public void IsValidVolume_RandomValidValues_ShouldReturnTrue()
    {
        // Arrange & Act & Assert
        for (var i = 0; i < 100; i++)
        {
            var volume = _faker.Random.Float(0f, 1f);
            AudioUtils.IsValidVolume(volume).ShouldBeTrue();
        }
    }

    #endregion

    #region Pitch Validation Tests

    [Theory]
    [InlineData(0.1f, true)]
    [InlineData(1f, true)]
    [InlineData(2f, true)]
    [InlineData(4f, true)]
    [InlineData(0f, false)]      // Zero pitch is invalid
    [InlineData(-1f, false)]     // Negative pitch is invalid
    [InlineData(4.1f, false)]    // Above 4.0 is invalid
    [InlineData(10f, false)]     // Much too high
    public void IsValidPitch_VariousValues_ShouldValidateCorrectly(float pitch, bool expectedValid)
    {
        // Act
        var result = AudioUtils.IsValidPitch(pitch);

        // Assert
        result.ShouldBe(expectedValid);
    }

    [Fact]
    public void IsValidPitch_BoundaryValues_ShouldValidateCorrectly()
    {
        // Assert
        AudioUtils.IsValidPitch(0.0001f).ShouldBeTrue();   // Just above zero
        AudioUtils.IsValidPitch(4f).ShouldBeTrue();        // Maximum
        AudioUtils.IsValidPitch(0f).ShouldBeFalse();       // Zero
        AudioUtils.IsValidPitch(4.0001f).ShouldBeFalse();  // Just above maximum
    }

    [Fact]
    public void IsValidPitch_NegativeValues_ShouldReturnFalse()
    {
        // Arrange
        var negativePitch = _faker.Random.Float(-10f, -0.0001f);

        // Act & Assert
        AudioUtils.IsValidPitch(negativePitch).ShouldBeFalse();
    }

    [Fact]
    public void IsValidPitch_RandomValidValues_ShouldReturnTrue()
    {
        // Arrange & Act & Assert
        for (var i = 0; i < 100; i++)
        {
            var pitch = _faker.Random.Float(0.1f, 4f);
            AudioUtils.IsValidPitch(pitch).ShouldBeTrue();
        }
    }

    #endregion

    #region Musical Application Tests

    [Fact]
    public void SemitonesToPitch_FullChromaticScale_ShouldProduceIncreasingPitches()
    {
        // Arrange
        var pitches = new List<float>();

        // Act - Generate pitches for full chromatic scale (12 semitones)
        for (var semitones = 0; semitones <= 12; semitones++)
        {
            pitches.Add(AudioUtils.SemitonesToPitch(semitones));
        }

        // Assert - Each pitch should be greater than the previous
        for (var i = 1; i < pitches.Count; i++)
        {
            pitches[i].ShouldBeGreaterThan(pitches[i - 1]);
        }
    }

    [Fact]
    public void AudioConversions_TypicalGameAudioScenario_ShouldWorkCorrectly()
    {
        // Arrange - Simulating game audio with -3dB background music
        var backgroundMusicDb = -3f;

        // Act
        var linearVolume = AudioUtils.DecibelToLinear(backgroundMusicDb);
        var pitchShift = AudioUtils.SemitonesToPitch(-2); // Two semitones down

        // Assert
        linearVolume.ShouldBeLessThan(1f);
        linearVolume.ShouldBeGreaterThan(0.5f);
        AudioUtils.IsValidVolume(linearVolume).ShouldBeTrue();
        AudioUtils.IsValidPitch(pitchShift).ShouldBeTrue();
        pitchShift.ShouldBeLessThan(1f); // Lower pitch
    }

    [Theory]
    [InlineData(0, 1.0f)]      // Unison
    [InlineData(1, 1.0595f)]   // Minor second
    [InlineData(2, 1.1225f)]   // Major second
    [InlineData(3, 1.1892f)]   // Minor third
    [InlineData(4, 1.2599f)]   // Major third
    [InlineData(5, 1.3348f)]   // Perfect fourth
    [InlineData(6, 1.4142f)]   // Tritone
    [InlineData(7, 1.4983f)]   // Perfect fifth
    [InlineData(12, 2.0f)]     // Octave
    public void SemitonesToPitch_MusicalIntervals_ShouldMatchExpectedRatios(int semitones, float expectedRatio)
    {
        // Act
        var result = AudioUtils.SemitonesToPitch(semitones);

        // Assert
        result.ShouldBe(expectedRatio, 0.01f);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DecibelToLinear_ExtremelyLargePositive_ShouldNotOverflow()
    {
        // Act
        var result = AudioUtils.DecibelToLinear(100f);

        // Assert
        float.IsInfinity(result).ShouldBeFalse();
        float.IsNaN(result).ShouldBeFalse();
        result.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void SemitonesToPitch_LargeValues_ShouldNotOverflow()
    {
        // Act
        var result = AudioUtils.SemitonesToPitch(48f); // 4 octaves

        // Assert
        float.IsInfinity(result).ShouldBeFalse();
        float.IsNaN(result).ShouldBeFalse();
        result.ShouldBe(16f, 0.01f); // 2^4 = 16
    }

    [Fact]
    public void PitchToSemitones_VerySmallPitch_ShouldNotThrow()
    {
        // Act
        var result = AudioUtils.PitchToSemitones(0.01f);

        // Assert
        float.IsNaN(result).ShouldBeFalse();
        float.IsInfinity(result).ShouldBeFalse();
        result.ShouldBeLessThan(0f); // Should be negative semitones
    }

    #endregion
}
