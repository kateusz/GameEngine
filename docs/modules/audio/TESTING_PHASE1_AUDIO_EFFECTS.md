# Phase 1 Audio Effects Testing Guide

This document provides step-by-step instructions for testing the basic audio effects implemented in Phase 1 of the audio effects system.

## Overview

Phase 1 implements three categories of audio effects that don't require the OpenAL EFX extension:

1. **Doppler Effect** - Velocity-based pitch shifting
2. **Directional Audio** - Cone-based sound emission
3. **Distance Attenuation Control** - Rolloff factor

All of these effects use standard OpenAL source properties and work on all platforms.

---

## Prerequisites

Before testing, ensure you have:
- [ ] Built the solution successfully (`dotnet build`)
- [ ] An audio file to test with (WAV format recommended)
- [ ] The Editor running
- [ ] A scene with a camera that has an `AudioListenerComponent`

---

## Test 1: Doppler Effect (Velocity-Based Pitch Shifting)

### Overview
The Doppler effect simulates realistic pitch changes when sound sources or the listener are moving. When a sound source moves towards you, the pitch increases; when moving away, the pitch decreases.

### Setup

1. **Create a test scene:**
   - Open the Editor
   - Create a new empty scene or use an existing one

2. **Set up the audio listener:**
   - Select your camera entity (should already have `TransformComponent`)
   - Add `AudioListenerComponent` if not present
   - Set "Is Active" to `true`

3. **Create a moving audio source:**
   - Create a new empty entity (name it "Moving Sound Source")
   - Add `AudioSourceComponent`
   - Assign an audio clip (continuous sound like an engine loop or siren works best)
   - Set the following properties:
     - Volume: `1.0`
     - Pitch: `1.0`
     - Loop: `true`
     - Play On Awake: `true`
     - Is 3D: `true`
     - Min Distance: `1.0`
     - Max Distance: `100.0`

4. **Configure Doppler effect:**
   - Expand "Audio Effects" section
   - Under "Doppler Effect":
     - Set Velocity to `(10, 0, 0)` (moving right at 10 units/sec)

### Expected Results

**Static Listener Test:**
- ✓ Audio should start at normal pitch when source and listener velocities are zero
- ✓ Setting source velocity to positive X should make pitch slightly higher
- ✓ Setting source velocity to negative X should make pitch slightly lower
- ✓ The effect should be subtle but noticeable with velocity around 10-20 units/sec

**Moving Listener Test:**
1. Select the camera entity
2. Find `AudioListenerComponent`
3. Set Velocity to `(-10, 0, 0)` (listener moving left)
4. Result: Pitch should increase (source moving right + listener moving left = approaching)

**Moving Away Test:**
1. Set source Velocity to `(10, 0, 0)`
2. Set listener Velocity to `(10, 0, 0)` (both moving same direction)
3. Result: Pitch should return to normal (no relative motion)

### Testing with Scripts

For more realistic testing, create a script to animate velocity:

```csharp
public class DopplerTestScript : ScriptableEntity
{
    private float time = 0;
    private AudioSourceComponent audioSource;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        time += (float)deltaTime.TotalSeconds;

        // Oscillate velocity back and forth
        float velocityX = MathF.Sin(time * 0.5f) * 20.0f;
        audioSource.Velocity = new Vector3(velocityX, 0, 0);
    }
}
```

**Expected:** Pitch should oscillate smoothly as the velocity changes, creating a "whooshing" effect.

### Troubleshooting

**Problem:** No pitch change observed
- Check that both source and listener have velocity set
- Try increasing velocity magnitude to 20-50 units/sec
- Verify the audio clip is playing (check "Status: Playing")
- Ensure "Is 3D" is enabled

**Problem:** Effect too strong/weak
- Doppler effect strength depends on OpenAL implementation
- Try different velocity magnitudes
- Check if listener velocity is also set (both contribute to effect)

---

## Test 2: Directional Audio (Cone-Based)

### Overview
Directional audio simulates sound sources that emit sound in a specific direction, like speakers, megaphones, or spotlights. Sound is louder when you're in front of the source and quieter when behind or to the side.

### Setup

1. **Create a directional audio source:**
   - Create a new entity (name it "Directional Speaker")
   - Add `TransformComponent` - position it at `(0, 0, 0)`
   - Add `AudioSourceComponent`
   - Assign an audio clip
   - Configure:
     - Volume: `1.0`
     - Loop: `true`
     - Play On Awake: `true`
     - Is 3D: `true`
     - Min Distance: `1.0`
     - Max Distance: `50.0`

2. **Configure directional cone:**
   - Expand "Audio Effects" section
   - Under "Directional Audio":
     - Direction: `(1, 0, 0)` (pointing right/+X axis)
     - Cone Inner Angle: `45.0` degrees
     - Cone Outer Angle: `90.0` degrees
     - Cone Outer Gain: `0.1` (10% volume outside cone)

3. **Set up listener positions:**
   - Position camera/listener at different angles around the source

### Expected Results

**Test Positions (Source at origin, Direction = (1, 0, 0)):**

| Listener Position | Expected Volume | Reason |
|------------------|-----------------|---------|
| `(5, 0, 0)` | **100%** | Directly in front (within inner cone) |
| `(3.5, 3.5, 0)` | **~50%** | At 45° (between inner and outer cone) |
| `(3, 5, 0)` | **~10%** | At ~60° (outside outer cone) |
| `(-5, 0, 0)` | **~10%** | Behind source (outside outer cone) |
| `(0, 5, 0)` | **~10%** | To the side (outside outer cone) |

**Visualization Test:**
1. Start with listener directly in front of source
2. Slowly move listener in a circle around the source
3. Expected: Volume should be loudest in front, fade as you move to the side, quietest behind

### Configuration Variations

**Narrow Beam (Flashlight/Laser):**
- Cone Inner Angle: `15.0`
- Cone Outer Angle: `30.0`
- Cone Outer Gain: `0.0`
- Result: Very focused sound, almost silent outside the cone

**Wide Speaker:**
- Cone Inner Angle: `120.0`
- Cone Outer Angle: `180.0`
- Cone Outer Gain: `0.3`
- Result: Sound covers wide area, still some sound to the sides

**Omnidirectional (Default):**
- Cone Inner Angle: `360.0`
- Cone Outer Angle: `360.0`
- Result: Sound equal in all directions (no directional effect)

### Testing with Scripts

Rotating directional source:

```csharp
public class RotatingDirectionalAudio : ScriptableEntity
{
    private float rotationSpeed = 45.0f; // degrees per second
    private AudioSourceComponent audioSource;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        float rotation = (float)deltaTime.TotalSeconds * rotationSpeed;

        // Rotate direction around Y axis
        var currentDir = audioSource.Direction;
        float angle = MathF.Atan2(currentDir.Z, currentDir.X);
        angle += MathF.PI / 180.0f * rotation;

        audioSource.Direction = new Vector3(
            MathF.Cos(angle),
            0,
            MathF.Sin(angle)
        );
    }
}
```

**Expected:** Sound should sweep around like a rotating beacon or radar.

### Troubleshooting

**Problem:** No directional effect observed
- Verify "Is 3D" is enabled
- Check Direction is not zero vector
- Ensure Cone Inner Angle < 360
- Move listener to clearly different angles (e.g., in front vs. behind)

**Problem:** Unexpected volume
- Check Cone Outer Gain (0.0 = silent, 1.0 = full volume outside)
- Verify Distance settings (Min/Max Distance affect volume too)
- Confirm Volume property is set to 1.0

**Problem:** Direction doesn't match visual rotation
- OpenAL uses right-handed coordinate system
- X = right, Y = up, Z = towards viewer
- Double-check your direction vector calculation

---

## Test 3: Rolloff Factor (Distance Attenuation Control)

### Overview
Rolloff factor controls how quickly sound volume decreases with distance. Higher values make sound fade faster (good for small spaces), lower values make sound carry farther (good for open areas).

### Setup

1. **Create a test audio source:**
   - Create entity "Rolloff Test Source"
   - Add `AudioSourceComponent`
   - Assign audio clip
   - Configure:
     - Volume: `1.0`
     - Loop: `true`
     - Play On Awake: `true`
     - Is 3D: `true`
     - Min Distance: `5.0`
     - Max Distance: `100.0`
   - Position at `(0, 0, 0)`

2. **Position listener:**
   - Place camera/listener at various distances
   - Test distances: 0, 5, 10, 20, 50, 100 units

### Expected Results

**Default Rolloff (Factor = 1.0):**

| Distance | Expected Volume | Note |
|----------|----------------|------|
| 0-5 units | 100% | Within Min Distance |
| 10 units | ~50% | Moderate attenuation |
| 20 units | ~25% | Clearly quieter |
| 50 units | ~10% | Barely audible |
| 100 units | ~5% | Very quiet (Max Distance) |

**High Rolloff (Factor = 2.0):**
- Sound fades much faster
- At 10 units: ~25% volume (vs. 50% with factor 1.0)
- At 20 units: ~6% volume (vs. 25% with factor 1.0)
- Use case: Small rooms, confined spaces

**Low Rolloff (Factor = 0.5):**
- Sound carries much farther
- At 10 units: ~75% volume (vs. 50% with factor 1.0)
- At 20 units: ~60% volume (vs. 25% with factor 1.0)
- Use case: Open outdoor areas, large halls

**No Rolloff (Factor = 0.0):**
- Sound doesn't attenuate with distance (except beyond Max Distance)
- Volume remains constant
- Use case: Special effects, UI sounds in 3D space

### Distance Testing Chart

Test by moving the listener to these positions while the source is at origin:

```
Rolloff Factor = 0.5 (slow falloff):
Distance:  5     10    20    30    50    75    100
Volume:   100%   75%   60%   50%   40%   35%   30%

Rolloff Factor = 1.0 (realistic):
Distance:  5     10    20    30    50    75    100
Volume:   100%   50%   25%   17%   10%   7%    5%

Rolloff Factor = 2.0 (fast falloff):
Distance:  5     10    20    30    50    75    100
Volume:   100%   25%   6%    3%    1%   <1%   <1%
```

### Testing with Scripts

Animated distance test:

```csharp
public class RolloffTestScript : ScriptableEntity
{
    private AudioSourceComponent audioSource;
    private float minRolloff = 0.5f;
    private float maxRolloff = 2.0f;
    private float time = 0;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        time += (float)deltaTime.TotalSeconds;

        // Oscillate rolloff factor
        float t = (MathF.Sin(time * 0.5f) + 1.0f) * 0.5f; // 0 to 1
        audioSource.RolloffFactor = minRolloff + (maxRolloff - minRolloff) * t;

        // Display current value (if in editor with console)
        if (time % 1.0f < deltaTime.TotalSeconds)
        {
            Console.WriteLine($"Rolloff Factor: {audioSource.RolloffFactor:F2}");
        }
    }
}
```

**Expected:** Volume should oscillate as rolloff factor changes, even with static listener position.

### Practical Use Cases

| Scenario | Recommended Rolloff | Reasoning |
|----------|-------------------|-----------|
| Small indoor room | 1.5 - 2.5 | Sound reflects off walls, doesn't carry far |
| Large hall/cathedral | 0.5 - 0.8 | Sound carries and reverberates |
| Outdoor forest | 1.0 - 1.5 | Normal attenuation with some absorption |
| Open field | 0.3 - 0.7 | Sound carries far without obstacles |
| Underwater | 2.0 - 3.0 | Sound attenuates quickly in water |
| Space (no atmosphere) | 0.0 | No medium for sound (but games often fake this) |

### Troubleshooting

**Problem:** Rolloff factor has no effect
- Ensure "Is 3D" is enabled
- Verify source and listener are at different positions
- Check that distance is beyond Min Distance
- Try extreme values (0.1 vs 5.0) to see difference

**Problem:** Sound too quiet/loud everywhere
- Rolloff factor multiplies distance attenuation, not absolute volume
- Adjust Volume property for overall loudness
- Check Min Distance and Max Distance settings
- Verify listener is positioned correctly

---

## Combined Effects Testing

### Test Scenario: Moving Car with Directional Engine Sound

This test combines all three effects to create a realistic moving vehicle:

1. **Create Car Entity:**
   - Add `AudioSourceComponent`
   - Assign engine loop sound
   - Configure:
     - Volume: `0.8`
     - Loop: `true`
     - Play On Awake: `true`
     - Is 3D: `true`
     - Min Distance: `2.0`
     - Max Distance: `50.0`
     - Rolloff Factor: `1.2` (slightly faster falloff)

2. **Add Directional Exhaust:**
   - Direction: `(-1, 0, 0)` (sound emits from back)
   - Cone Inner Angle: `90.0`
   - Cone Outer Angle: `150.0`
   - Cone Outer Gain: `0.3`

3. **Add Movement Script:**

```csharp
public class CarAudioTest : ScriptableEntity
{
    private float speed = 15.0f;
    private AudioSourceComponent audioSource;
    private TransformComponent transform;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
        transform = Entity.GetComponent<TransformComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        float dt = (float)deltaTime.TotalSeconds;

        // Move car forward (along its facing direction)
        Vector3 forward = new Vector3(1, 0, 0); // or calculate from rotation
        transform.Translation += forward * speed * dt;

        // Set velocity for Doppler effect
        audioSource.Velocity = forward * speed;

        // Update exhaust direction (opposite of movement)
        audioSource.Direction = -forward;

        // Optional: Vary pitch with speed
        audioSource.Pitch = 0.8f + (speed / 30.0f) * 0.4f;
    }
}
```

### Expected Behavior:

1. **As car approaches listener:**
   - ✓ Volume increases (distance)
   - ✓ Pitch increases (Doppler effect)
   - ✓ Sound louder from front than back (directional)

2. **As car passes listener:**
   - ✓ Pitch suddenly drops (Doppler shift changes)
   - ✓ Volume peaks at closest point
   - ✓ Sound shifts to back exhaust cone (louder)

3. **As car moves away:**
   - ✓ Volume decreases rapidly (distance + rolloff)
   - ✓ Pitch lower than original (Doppler effect)
   - ✓ Exhaust sound prominent (directional, facing listener)

---

## Performance Testing

### Frame Rate Impact

Test audio effects don't significantly impact performance:

1. Create a scene with multiple audio sources (10-20)
2. Enable all effects on each source
3. Monitor FPS in editor
4. Expected: Minimal impact (<1ms per frame for 20 sources)

### Memory Usage

Audio effects don't allocate additional memory - they set OpenAL source properties directly.

---

## Verification Checklist

After testing, verify:

- [ ] Doppler effect works with moving sources
- [ ] Doppler effect works with moving listener
- [ ] Directional audio correctly projects sound in cone
- [ ] Cone outer gain controls volume outside cone
- [ ] Rolloff factor affects distance attenuation
- [ ] All effects work simultaneously
- [ ] Editor UI shows all effect properties
- [ ] Properties persist when saving/loading scene
- [ ] No crashes or errors in console
- [ ] Performance remains acceptable with multiple sources

---

## Known Limitations

1. **Doppler Effect Strength:**
   - OpenAL implementation-dependent
   - Some platforms may have weaker/stronger effect
   - No direct control over Doppler shift amount

2. **Directional Audio:**
   - Cones are simplified models (real speakers have complex patterns)
   - No frequency-dependent directionality
   - Direction must be updated manually if entity rotates

3. **Rolloff Factor:**
   - Only affects inverse distance model
   - No exponential or linear distance models exposed
   - Interaction with Min/Max Distance can be complex

---

## Next Steps

After verifying Phase 1 works correctly:

1. **Phase 2:** OpenAL EFX support
   - Reverb with environment presets
   - Room simulation

2. **Phase 3:** Additional effects
   - Echo, chorus, distortion
   - Flanger, filters

3. **Phase 4:** Editor enhancements
   - Visual effect preview
   - Preset management

4. **Phase 5:** Environmental audio
   - Zone-based effects
   - Automatic reverb

---

## Troubleshooting Tips

### General Audio Issues

**No sound at all:**
- Check audio clip is loaded
- Verify Volume > 0
- Ensure "Is 3D" matches your spatial setup
- Check that AudioSystem is initialized
- Verify device audio output is working

**Inconsistent behavior:**
- Ensure AudioSystem is updating (priority 160)
- Check that component properties sync to runtime source
- Restart editor to clear cached state

**Performance problems:**
- Disable effects temporarily to isolate issue
- Check for excessive property updates
- Profile with multiple sources

### Effect-Specific Issues

**Doppler not working:**
- Requires both source AND listener velocity
- Effect may be subtle at low velocities
- OpenAL implementation varies by platform

**Directional audio wrong:**
- Verify coordinate system (right-handed)
- Check direction vector is normalized
- Ensure angles are in degrees (not radians)

**Rolloff too aggressive:**
- Check Min Distance (no attenuation within this)
- Try lower rolloff values (0.5-1.0)
- Adjust Max Distance for desired range

---

## Support

For issues or questions:
- Check the main audio documentation: `docs/modules/audio/AUDIO_README.md`
- Review OpenAL specification for technical details
- Test with simpler scenarios to isolate problems

---

**Document Version:** 1.0
**Phase:** 1 - Basic Audio Effects
**Date:** 2025-10-28
**Status:** Complete
