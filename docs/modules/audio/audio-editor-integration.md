# Audio System - Editor Integration Guide

Complete guide for using the Audio System within the GameEngine Editor.

## Table of Contents
- [Component Editors](#component-editors)
- [Inspector Workflow](#inspector-workflow)
- [Scene Setup](#scene-setup)
- [Testing Audio in Editor](#testing-audio-in-editor)
- [Asset Management](#asset-management)
- [Common Editor Workflows](#common-editor-workflows)
- [Keyboard Shortcuts](#keyboard-shortcuts)

---

## Component Editors

### AudioListenerComponent Editor

**Location**: `Editor/Panels/ComponentEditors/AudioListenerComponentEditor.cs`

**Inspector UI:**
```
┌─────────────────────────────────────┐
│ Audio Listener Component            │
├─────────────────────────────────────┤
│ Is Active  [ ✓ ]                    │
└─────────────────────────────────────┘
```

**Properties:**
- **Is Active** (Checkbox)
  - When checked: This listener is active and receives audio
  - When unchecked: Listener is disabled
  - ⚠️ **Important**: Only ONE listener should be active per scene

**Usage:**
1. Select entity (usually Main Camera)
2. Add Component → Audio Listener Component
3. Ensure "Is Active" is checked
4. Done - listener will follow entity's transform

---

### AudioSourceComponent Editor

**Location**: `Editor/Panels/ComponentEditors/AudioSourceComponentEditor.cs`

**Inspector UI:**
```
┌──────────────────────────────────────────────────┐
│ Audio Source Component                           │
├──────────────────────────────────────────────────┤
│ Audio Clip                                       │
│ [assets/sounds/music/background.wav] [Browse...] │
│                                                  │
│ Volume      [========|----] 0.80                 │
│ Pitch       [==========|--] 1.00                 │
│                                                  │
│ Loop         [ ✓ ]                               │
│ Play On Awake [ ✓ ]                              │
│                                                  │
│ Is 3D        [ ✓ ]                               │
│ Min Distance [====|------] 5.00                  │
│ Max Distance [========|--] 50.00                 │
│                                                  │
│ [ ▶ Play ] [ ⏸ Pause ] [ ⏹ Stop ]              │
│                                                  │
│ Is Playing: Yes                                  │
└──────────────────────────────────────────────────┘
```

**Properties:**

#### Audio Clip
- **Type**: File path (string)
- **UI**: Text field + Browse button
- **Accepts**: `.wav` files
- **Behavior**: Click "Browse..." to open file picker
- **Validation**: Red text if file doesn't exist
- **Default**: Empty (no clip)

#### Volume
- **Type**: Float (0.0 to 2.0)
- **UI**: Slider + numeric input
- **Recommended**: 0.0 to 1.0 for normal sounds
- **Values > 1.0**: Amplified (may cause clipping)
- **Default**: 1.0

#### Pitch
- **Type**: Float (0.1 to 3.0)
- **UI**: Slider + numeric input
- **Recommended**: 0.8 to 1.2 for variation
- **1.0**: Normal pitch
- **< 1.0**: Lower/slower
- **> 1.0**: Higher/faster
- **Default**: 1.0

#### Loop
- **Type**: Boolean
- **UI**: Checkbox
- **Checked**: Audio loops continuously
- **Unchecked**: Plays once and stops
- **Default**: False

#### Play On Awake
- **Type**: Boolean
- **UI**: Checkbox
- **Checked**: Auto-plays when scene starts
- **Unchecked**: Must be triggered manually (via script or Play button)
- **Default**: False

#### Is 3D
- **Type**: Boolean
- **UI**: Checkbox
- **Checked**: 3D spatial audio (position matters)
- **Unchecked**: 2D audio (same volume everywhere)
- **Shows/hides**: Min Distance and Max Distance sliders
- **Default**: False

#### Min Distance
- **Type**: Float (0.1 to 1000.0)
- **UI**: Slider + numeric input (only visible when Is3D = true)
- **Meaning**: Distance at which sound starts to attenuate
- **Smaller values**: Sound attenuates quickly
- **Larger values**: Sound travels further at full volume
- **Default**: 1.0

#### Max Distance
- **Type**: Float (1.0 to 1000.0)
- **UI**: Slider + numeric input (only visible when Is3D = true)
- **Meaning**: Maximum distance where sound is audible
- **Beyond this**: Sound is silent
- **Must be**: Greater than Min Distance
- **Default**: 100.0

#### Playback Controls (Play Mode Only)
- **▶ Play**: Start/resume playback
- **⏸ Pause**: Pause playback (can resume)
- **⏹ Stop**: Stop playback and reset position
- **Is Playing**: Read-only status indicator
- **Note**: Only visible when editor is in Play Mode

---

## Inspector Workflow

### Adding Components

**Method 1: Via Add Component Button**
1. Select entity in Scene Hierarchy
2. In Properties Panel, click **"Add Component"** button
3. Scroll or search for audio components:
   - "Audio Listener Component"
   - "Audio Source Component"
4. Click the component to add it

**Method 2: Via Component Selector Popup**
1. Right-click entity in Scene Hierarchy
2. Select "Add Component"
3. Choose from component list

### Editing Components

**Direct Value Entry:**
- Click on numeric fields and type values
- Press Enter to confirm

**Slider Adjustment:**
- Drag sliders for visual feedback
- Fine control: Hold Shift while dragging (slower movement)
- Reset to default: Double-click slider

**Checkbox Toggling:**
- Click checkbox to toggle
- Keyboard: Tab to focus + Space to toggle

**File Browsing:**
- Click "Browse..." button
- Navigate to audio file
- File must be `.wav` format
- Path is relative to executable

### Removing Components

1. Select entity with audio component
2. In Properties Panel, find component
3. Click **"Remove Component"** button (usually ❌ or trash icon)
4. Confirm deletion

---

## Scene Setup

### Typical Scene Hierarchy

```
Scene: "My Game Level"
│
├─ 📷 Main Camera
│  ├─ Transform Component
│  ├─ Camera Component
│  └─ 🔊 Audio Listener Component    ← Add this
│
├─ 🎮 Player
│  ├─ Transform Component
│  ├─ Sprite/Mesh Component
│  ├─ Script Component (PlayerController)
│  └─ 🔊 Audio Source Component     ← Footsteps, jump sounds
│     (Is 3D: false, Play On Awake: false)
│
├─ 🎵 Background Music
│  ├─ Transform Component
│  └─ 🔊 Audio Source Component     ← Music
│     (Loop: true, Play On Awake: true, Is 3D: false)
│
├─ 🔥 Campfire
│  ├─ Transform Component (pos: 10, 0, 5)
│  ├─ Sprite/Mesh Component
│  └─ 🔊 Audio Source Component     ← 3D ambient sound
│     (Loop: true, Play On Awake: true, Is 3D: true)
│
└─ 🏠 Audio Manager (Empty Entity)
   └─ Script Component (AudioManager, AudioPoolSystem, etc.)
```

### Setup Steps

**1. Set Up Listener (Once per scene)**
```
Select: Main Camera
Add: Audio Listener Component
Set: Is Active = true
```

**2. Add Background Music**
```
Create: New Entity "Background Music"
Add: Audio Source Component
Configure:
  - Audio Clip: assets/sounds/music/menu.wav
  - Loop: ✓
  - Play On Awake: ✓
  - Is 3D: ☐
  - Volume: 0.6
```

**3. Add 3D Positioned Sound**
```
Create: New Entity "Campfire"
Transform: Set Translation (e.g., 10, 0, 5)
Add: Audio Source Component
Configure:
  - Audio Clip: assets/sounds/ambient/fire.wav
  - Loop: ✓
  - Play On Awake: ✓
  - Is 3D: ✓
  - Min Distance: 5.0
  - Max Distance: 50.0
  - Volume: 1.0
```

**4. Add Player Sounds**
```
Select: Player Entity
Add: Audio Source Component
Configure:
  - Audio Clip: assets/sounds/sfx/jump.wav
  - Loop: ☐
  - Play On Awake: ☐  (script will trigger it)
  - Is 3D: ☐ (or ✓ depending on preference)
  - Volume: 0.8
```

---

## Testing Audio in Editor

### Play Mode Testing

**Entering Play Mode:**
1. Click **▶ Play** button in editor toolbar
2. Or press **F5** (if keyboard shortcut configured)
3. Editor enters play mode:
   - Scene becomes live
   - Audio sources with "Play On Awake" start
   - Scripts execute
   - Physics runs

**What Happens:**
- AudioListenerComponent tracks camera position
- AudioSourceComponents with PlayOnAwake=true start playing
- 3D audio responds to camera movement
- All audio systems update every frame

**Testing 3D Audio:**
1. Enter Play Mode
2. Use scene navigation controls to move camera:
   - **W/A/S/D**: Move camera
   - **Mouse + Right-Click**: Rotate camera
   - **Scroll**: Zoom
3. Notice:
   - Volume changes with distance
   - Stereo panning changes with position
   - Sound cuts off beyond MaxDistance

**Testing 2D Audio:**
1. Enter Play Mode
2. Music/UI sounds play at constant volume
3. Not affected by camera position

**Exiting Play Mode:**
1. Click **⏹ Stop** button
2. Or press **F5** again
3. Scene returns to edit state

### Real-Time Property Changes (Play Mode)

While in Play Mode, you can modify audio properties in the inspector:

**Volume/Pitch:**
- Adjust sliders → Hear changes immediately
- Great for finding the right balance

**Loop:**
- Toggle → Affects behavior on next Play()

**3D Settings:**
- Change Min/Max Distance → Affects attenuation curve
- Move entity → 3D position updates automatically

**Playback Controls:**
- Use ▶ Play / ⏸ Pause / ⏹ Stop buttons
- Test triggering sounds without restarting scene

---

## Asset Management

### Organizing Audio Files

**Recommended Folder Structure:**
```
YourProject/
└── assets/
    └── sounds/
        ├── music/
        │   ├── menu.wav
        │   ├── gameplay_calm.wav
        │   ├── gameplay_tension.wav
        │   └── gameplay_combat.wav
        ├── sfx/
        │   ├── player/
        │   │   ├── jump.wav
        │   │   ├── land.wav
        │   │   └── hurt.wav
        │   ├── weapons/
        │   │   ├── pistol_shot.wav
        │   │   └── reload.wav
        │   └── impacts/
        │       ├── hit_metal.wav
        │       └── hit_wood.wav
        ├── ambient/
        │   ├── wind.wav
        │   ├── water.wav
        │   └── fire.wav
        └── ui/
            ├── click.wav
            ├── hover.wav
            └── error.wav
```

### File Naming Conventions

**Recommended:**
- Lowercase with underscores: `player_jump.wav`
- Descriptive names: `explosion_large.wav` not `sound1.wav`
- Numbered variations: `footstep_grass_1.wav`, `footstep_grass_2.wav`
- Include surface/type: `impact_wood.wav`, `impact_metal.wav`

### Audio File Guidelines

**Format:**
- Use **WAV** (uncompressed)
- Sample Rate: 44100 Hz or 48000 Hz
- Bit Depth: 16-bit or 24-bit
- Channels: Mono for 3D audio, Stereo for 2D music

**File Size:**
- Background Music: 1-5 MB (long duration OK)
- Sound Effects: < 500 KB (keep short)
- Ambient Loops: 500 KB - 2 MB

**Quality vs Performance:**
- Higher sample rates = better quality, larger files
- 44100 Hz is sufficient for most game audio
- Compress/downgrade external assets if needed

---

## Common Editor Workflows

### Workflow 1: Create Footstep System

**Setup (Editor):**
1. Select Player entity
2. Add → Audio Source Component
3. Configure:
   - Audio Clip: `assets/sounds/sfx/footstep.wav`
   - Play On Awake: ☐
   - Is 3D: ✓ (or ☐ for first-person)
   - Volume: 0.6

**Script (Code):**
```csharp
public class PlayerFootsteps : ScriptableEntity
{
    private AudioSourceComponent? footstepAudio;

    public override void OnStart()
    {
        footstepAudio = Entity.GetComponent<AudioSourceComponent>();
    }

    public void PlayFootstep()
    {
        footstepAudio?.Play();
    }
}
```

**Test:**
1. Enter Play Mode
2. Move player
3. Footstep plays when called

### Workflow 2: Set Up Adaptive Music

**Setup (Editor):**
1. Create 3 entities: "Music_Calm", "Music_Tension", "Music_Combat"
2. For each entity:
   - Add Audio Source Component
   - Set respective audio clip
   - Loop: ✓
   - Play On Awake: ✓
   - Volume: 0.0 (start silent)

**Script (Code):**
- Implement AdaptiveMusicSystem (see [Advanced Examples](audio-advanced-examples.md#adaptive-music-system))

**Test:**
1. Enter Play Mode
2. Call `AdaptiveMusicSystem.Instance.SetMusicState("tension")`
3. Music crossfades smoothly

### Workflow 3: Add UI Sound

**Setup (Editor):**
- No entity needed for one-shot UI sounds!

**Script (Code):**
```csharp
public class UIButton : ScriptableEntity
{
    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (IsButtonClicked())
        {
            AudioEngine.Instance.PlayOneShot(
                "assets/sounds/ui/click.wav",
                volume: 0.7f
            );
        }
    }
}
```

**Test:**
1. Enter Play Mode
2. Click UI element
3. Sound plays instantly

### Workflow 4: Debug Audio Issues

**Steps:**
1. **Check Console:**
   - Look for audio loading errors
   - Verify file paths are correct

2. **Verify Listener:**
   - Select Main Camera
   - Ensure AudioListenerComponent exists
   - Check "Is Active" = true

3. **Test Audio Source:**
   - Select entity with AudioSourceComponent
   - Enter Play Mode
   - Use inspector's ▶ Play button
   - If it doesn't play → check Audio Clip path

4. **Test 3D Audio:**
   - Position camera far from source
   - Move closer gradually
   - Should hear volume increase

5. **Check Volume:**
   - All volumes > 0
   - Master system volume not muted

---

## Keyboard Shortcuts

### Editor Navigation (while testing audio)

| Key | Action |
|-----|--------|
| **F5** | Enter/Exit Play Mode |
| **W/A/S/D** | Move camera (Play Mode) |
| **Right-Click + Drag** | Rotate camera |
| **Scroll Wheel** | Zoom camera |
| **Ctrl+Z** | Undo |
| **Ctrl+Y** | Redo |
| **Ctrl+S** | Save scene |

### Component Editing

| Key | Action |
|-----|--------|
| **Tab** | Navigate between fields |
| **Enter** | Confirm value |
| **Esc** | Cancel edit |
| **Space** | Toggle checkbox (when focused) |

---

## Inspector Tips & Tricks

### Pro Tips

**Tip 1: Quick Volume Adjustment**
- Double-click volume slider to reset to 1.0
- Hold Shift while dragging for fine control

**Tip 2: Copy/Paste Components**
- Right-click component → Copy
- Right-click other entity → Paste Component
- Duplicates entire configuration

**Tip 3: Batch Configure**
- Select multiple entities with audio sources
- Use multi-edit to change common properties
- Great for adjusting all SFX volumes at once

**Tip 4: Preview Before Play**
- Use inspector's Play button (Play Mode only)
- Test single sound without triggering game logic

**Tip 5: Lock Inspector**
- Lock inspector to one entity
- Select different entities in hierarchy
- Locked inspector stays focused
- Good for comparing audio settings

**Tip 6: Color Coding (if supported)**
- Name music entities with "🎵" prefix
- Name SFX entities with "🔊" prefix
- Easy visual identification in hierarchy

---

## Component Selector Integration

The Audio Components appear in the Component Selector popup:

**Location:** `Editor/Panels/Elements/ComponentSelector.cs`

**How It Works:**
1. User clicks "Add Component" on entity
2. Component Selector shows available components
3. Audio components appear in list:
   - Audio Listener Component
   - Audio Source Component
4. User clicks one to add it

**Search:**
- Type "audio" to filter
- Shows both components

---

## Serialization

### Scene Saving

When you save a scene, audio component data is serialized to JSON:

```json
{
  "entities": [
    {
      "name": "Campfire",
      "components": [
        {
          "type": "AudioSourceComponent",
          "audioClipPath": "assets/sounds/ambient/fire.wav",
          "volume": 1.0,
          "pitch": 1.0,
          "loop": true,
          "playOnAwake": true,
          "is3D": true,
          "minDistance": 5.0,
          "maxDistance": 50.0
        }
      ]
    }
  ]
}
```

**Saved Properties:**
- Audio Clip path
- Volume, Pitch
- Loop, PlayOnAwake
- Is3D, MinDistance, MaxDistance

**Not Saved:**
- Is Playing (runtime state)
- RuntimeAudioSource (runtime-only)

---

## Best Practices for Editor Use

### ✅ DO

- **Use descriptive entity names** ("Background Music", not "Entity_23")
- **Organize hierarchy** with empty parents for audio groups
- **Test in Play Mode** before building
- **Save frequently** (Ctrl+S)
- **Use prefabs** for entities with complex audio setups
- **Document** non-obvious audio triggers in scene notes

### ❌ DON'T

- Don't forget to add AudioListenerComponent!
- Don't have multiple active listeners
- Don't use absolute paths for audio files
- Don't set Is3D=true for background music
- Don't skip testing 3D audio distances
- Don't set MaxDistance < MinDistance

---

## Troubleshooting in Editor

### Problem: Component doesn't appear in "Add Component" menu

**Solution:**
- Check that component is properly registered
- Restart editor
- Verify `ComponentSelector.cs` includes audio components

### Problem: Audio file path shows as red/invalid

**Solution:**
- File doesn't exist at specified path
- Check spelling and file extension
- Use relative path from executable
- Ensure file is `.wav` format

### Problem: Can't hear any sound in Play Mode

**Solution:**
1. Check AudioListenerComponent exists and IsActive=true
2. Check Volume > 0
3. Check system audio isn't muted
4. Verify Audio Clip is assigned
5. Check console for errors

### Problem: 3D audio not working correctly

**Solution:**
1. Ensure Is3D checkbox is checked
2. Entity must have TransformComponent
3. Check Min/Max distance values
4. Move camera within MaxDistance
5. Verify AudioListenerComponent is on camera

---

## Summary

The Audio System integrates seamlessly with the GameEngine Editor:

✅ **Visual component editors** for all audio settings
✅ **Real-time testing** in Play Mode
✅ **Scene serialization** preserves audio setups
✅ **Inspector controls** for immediate playback testing
✅ **Hierarchical organization** for complex audio setups

**Next Steps:**
1. Follow [Quick Start Tutorial](audio-quick-start.md) to add your first sounds
2. Experiment with inspector values in Play Mode
3. Build complex audio scenes using the hierarchy
4. Script dynamic audio behavior for your game

Happy audio design! 🎵🎮
