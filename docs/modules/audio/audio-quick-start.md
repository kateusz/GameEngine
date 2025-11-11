# Audio System Quick Start Tutorial

This tutorial will get you up and running with the Audio System in 5 minutes.

## Tutorial 1: Adding Background Music (2D Audio)

### Step 1: Prepare Your Audio File

Place a `.wav` file in your project:
```
YourProject/assets/sounds/music/background.wav
```

### Step 2: Set Up the Audio Listener

1. Open your scene in the Editor
2. Select the **Main Camera** entity in the Scene Hierarchy
3. In the Properties Panel, click **"Add Component"**
4. Select **"Audio Listener Component"**
5. Ensure **"Is Active"** is checked ✓

### Step 3: Create a Music Entity

1. Right-click in Scene Hierarchy → **"Create Empty Entity"**
2. Rename it to **"Background Music"**
3. Click **"Add Component"** → **"Audio Source Component"**
4. Configure the component:
   - **Audio Clip**: Click browse, select `assets/sounds/music/background.wav`
   - **Volume**: `0.6`
   - **Loop**: ✓ Check this
   - **Play On Awake**: ✓ Check this
   - **Is 3D**: ☐ Leave unchecked (2D audio)

### Step 4: Test

1. Click the **Play** button in the Editor toolbar
2. You should hear the music playing!
3. Click **Stop** to exit play mode

**Congratulations!** You've added background music to your game.

---

## Tutorial 2: Adding 3D Spatial Audio

### Step 1: Prepare Your Audio File

```
YourProject/assets/sounds/ambient/campfire.wav
```

### Step 2: Create a Sound Source Entity

1. Right-click in Scene Hierarchy → **"Create Empty Entity"**
2. Rename it to **"Campfire"**
3. Select the Transform Component and set:
   - **Translation**: `X: 10, Y: 0, Z: 5` (position it somewhere in your scene)

### Step 3: Add Audio Source Component

1. Click **"Add Component"** → **"Audio Source Component"**
2. Configure:
   - **Audio Clip**: `assets/sounds/ambient/campfire.wav`
   - **Volume**: `1.0`
   - **Loop**: ✓ Check this
   - **Play On Awake**: ✓ Check this
   - **Is 3D**: ✓ **CHECK THIS** (enables spatial audio)
   - **Min Distance**: `5.0`
   - **Max Distance**: `50.0`

### Step 4: Test 3D Audio

1. Click **Play**
2. Move the camera around the scene
3. Notice the sound gets:
   - **Louder** when you move toward (10, 0, 5)
   - **Quieter** when you move away
   - **Panned left/right** based on position

**You now have 3D spatial audio!**

---

## Tutorial 3: Triggering Sounds from Scripts

### Step 1: Create a Player Entity with Audio

1. Select your **Player** entity (or create one)
2. Add **Audio Source Component**:
   - **Audio Clip**: `assets/sounds/sfx/jump.wav`
   - **Volume**: `0.8`
   - **Is 3D**: ☐ Unchecked (2D for player sounds)
   - **Play On Awake**: ☐ **UNCHECKED** (we'll trigger it manually)

### Step 2: Create the Script

Create a new file: `YourProject/Scripts/PlayerController.cs`

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using System.Numerics;

public class PlayerController : ScriptableEntity
{
    private AudioSourceComponent? jumpSound;
    private bool isGrounded = true;
    private float jumpForce = 5.0f;

    public override void OnStart()
    {
        // Get reference to audio component
        jumpSound = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        // Simple jump logic
        if (Input.IsKeyPressed(Key.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void Jump()
    {
        // Play jump sound
        if (jumpSound != null)
        {
            jumpSound.Play();
        }

        // Apply jump physics
        var rigidBody = Entity.GetComponent<RigidBody2DComponent>();
        if (rigidBody != null)
        {
            rigidBody.ApplyLinearImpulse(new Vector2(0, jumpForce));
        }

        isGrounded = false;
        // Set isGrounded = true when landing (implement collision detection)
    }
}
```

### Step 3: Attach Script to Player

1. Select **Player** entity
2. Add **Script Component**
3. Select **PlayerController** script

### Step 4: Test

1. Click **Play**
2. Press **Space** to jump
3. You should hear the jump sound!

---

## Tutorial 4: One-Shot Sounds (Quick Fire-and-Forget)

For UI sounds or effects that don't need an entity:

```csharp
using Engine.Scene;
using Engine.Audio;

public class UIButton : ScriptableEntity
{
    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            // Quick one-shot - no entity needed!
            AudioEngine.Instance.PlayOneShot(
                "assets/sounds/ui/click.wav",
                volume: 0.7f
            );
        }
    }
}
```

**Use Case:** Button clicks, pickup sounds, collision sounds

---

## Common Patterns Cheat Sheet

### Pattern 1: Volume Fade

```csharp
public class MusicFader : ScriptableEntity
{
    private AudioSourceComponent? music;
    private float targetVolume = 0.0f;
    private float fadeSpeed = 0.5f;

    public override void OnStart()
    {
        music = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (music != null)
        {
            float step = fadeSpeed * (float)deltaTime.TotalSeconds;
            music.Volume = MoveTowards(music.Volume, targetVolume, step);
        }
    }

    public void FadeOut() => targetVolume = 0.0f;
    public void FadeIn() => targetVolume = 1.0f;

    private float MoveTowards(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
            return target;
        return current + Math.Sign(target - current) * maxDelta;
    }
}
```

### Pattern 2: Random Pitch Variation

```csharp
public class FootstepSystem : ScriptableEntity
{
    private AudioSourceComponent? footstep;
    private Random random = new Random();

    public override void OnStart()
    {
        footstep = Entity.GetComponent<AudioSourceComponent>();
    }

    private void PlayFootstep()
    {
        if (footstep != null)
        {
            // Randomize pitch for variety
            footstep.Pitch = 0.9f + (float)random.NextDouble() * 0.2f; // 0.9 to 1.1
            footstep.Play();
        }
    }
}
```

### Pattern 3: Audio Manager (Global Control)

```csharp
using Engine.Scene;
using Engine.Scene.Components;

public class AudioManager : ScriptableEntity
{
    private static AudioManager? _instance;
    public static AudioManager? Instance => _instance;

    private float masterVolume = 1.0f;
    private float musicVolume = 0.8f;
    private float sfxVolume = 1.0f;

    private AudioSourceComponent? currentMusic;

    public override void OnStart()
    {
        _instance = this;
    }

    public void PlayMusic(string clipPath, bool loop = true)
    {
        if (currentMusic != null)
        {
            currentMusic.Stop();
        }

        // Find or create music entity
        var musicEntity = FindEntityByName("Music");
        currentMusic = musicEntity?.GetComponent<AudioSourceComponent>();

        if (currentMusic != null)
        {
            var clip = AudioEngine.Instance.LoadAudioClip(clipPath);
            currentMusic.AudioClip = clip;
            currentMusic.Volume = musicVolume * masterVolume;
            currentMusic.Loop = loop;
            currentMusic.Play();
        }
    }

    public void PlaySFX(string clipPath, float volume = 1.0f)
    {
        AudioEngine.Instance.PlayOneShot(clipPath, volume * sfxVolume * masterVolume);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        UpdateAllVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Math.Clamp(volume, 0.0f, 1.0f);
        if (currentMusic != null)
        {
            currentMusic.Volume = musicVolume * masterVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Math.Clamp(volume, 0.0f, 1.0f);
    }

    private void UpdateAllVolumes()
    {
        if (currentMusic != null)
        {
            currentMusic.Volume = musicVolume * masterVolume;
        }
    }

    private Entity? FindEntityByName(string name)
    {
        // Implement entity search
        return null;
    }
}
```

**Usage:**
```csharp
// In any script
AudioManager.Instance?.PlayMusic("assets/sounds/music/menu.wav");
AudioManager.Instance?.PlaySFX("assets/sounds/sfx/explosion.wav");
AudioManager.Instance?.SetMasterVolume(0.5f);
```

---

## Troubleshooting Tips

### Problem: No sound playing

**Check:**
```csharp
// Add this to OnStart() to debug
public override void OnStart()
{
    var audioSource = Entity.GetComponent<AudioSourceComponent>();
    Console.WriteLine($"Audio Source: {audioSource != null}");
    Console.WriteLine($"Audio Clip: {audioSource?.AudioClip != null}");
    Console.WriteLine($"Volume: {audioSource?.Volume}");
    Console.WriteLine($"Is Playing: {audioSource?.IsPlaying}");
}
```

### Problem: 3D audio not working

Ensure:
1. ✓ AudioListenerComponent exists and is active
2. ✓ Entity has TransformComponent
3. ✓ `Is3D` is checked on AudioSourceComponent
4. ✓ Camera is within MaxDistance

### Problem: Audio is delayed

If you're loading clips during gameplay:
```csharp
// BAD - loads every time
public void Shoot()
{
    AudioEngine.Instance.PlayOneShot("assets/sounds/gun.wav"); // Loads file!
}

// GOOD - preload in OnStart
private IAudioClip? gunShotClip;

public override void OnStart()
{
    gunShotClip = AudioEngine.Instance.LoadAudioClip("assets/sounds/gun.wav");
}

public void Shoot()
{
    if (gunShotClip != null)
    {
        AudioEngine.Instance.PlayOneShot("assets/sounds/gun.wav"); // Uses cache
    }
}
```

---

## Next Steps

1. ✅ You now know how to add 2D and 3D audio
2. ✅ You can trigger sounds from scripts
3. ✅ You understand common audio patterns

**Explore More:**
- Read the full [Audio System Guide](audio-system.md)
- Implement an Audio Manager for your game
- Experiment with 3D audio distance settings
- Try adaptive music systems

Happy game development! 🎮🔊
