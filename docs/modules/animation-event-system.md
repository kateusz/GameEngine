# Animation Event System - Architecture & Usage

## Overview

The animation event system uses the **publish-subscribe** pattern via an `EventBus`, enabling loose coupling between the animation system and game logic.

## EventBus Architecture

### Publisher: AnimationSystem

`AnimationSystem` publishes events to the `EventBus` when:

* **AnimationFrameEvent** – the animation reaches a frame with defined events
* **AnimationCompleteEvent** – a non-looping animation finishes

### Consumers (Subscribers)

Animation event consumers should be:

#### 1. **ScriptableEntity (Game Scripts)** ⭐ PRIMARY USE CASE

Game scripts should listen to animation events for their own entity:

```csharp
public class PlayerController : ScriptableEntity
{
    private EventBus _eventBus;
    
    public override void OnCreate()
    {
        // Get EventBus reference from the application/scene
        _eventBus = Application.Instance.EventBus;
        
        // Subscribe to animation events
        _eventBus.Subscribe<AnimationFrameEvent>(OnAnimationFrame);
        _eventBus.Subscribe<AnimationCompleteEvent>(OnAnimationComplete);
    }
    
    private void OnAnimationFrame(AnimationFrameEvent evt)
    {
        // Only handle events from this entity
        if (evt.Entity != Entity)
            return;
            
        // Handle specific frame events
        switch (evt.EventName)
        {
            case "footstep":
                PlayFootstepSound();
                SpawnDustParticles();
                break;
                
            case "attack_hit":
                DealDamageToEnemies();
                break;
                
            case "camera_shake":
                TriggerCameraShake();
                break;
        }
    }
    
    private void OnAnimationComplete(AnimationCompleteEvent evt)
    {
        if (evt.Entity != Entity)
            return;
            
        // Handle animation completion
        if (evt.ClipName == "death")
        {
            DestroyEntity(Entity);
        }
        else if (evt.ClipName == "attack")
        {
            // Return to idle state
            GetComponent<AnimationComponent>().Play("idle");
        }
    }
    
    public override void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks!
        _eventBus?.Unsubscribe<AnimationFrameEvent>(OnAnimationFrame);
        _eventBus?.Unsubscribe<AnimationCompleteEvent>(OnAnimationComplete);
    }
}
```

#### 2. **Game Systems** (Audio, Particles, Camera)

Dedicated systems can listen to animation events globally:

```csharp
public class AudioSystem : ISystem
{
    private readonly EventBus _eventBus;
    private readonly Dictionary<string, Sound> _soundEffects;
    
    public AudioSystem(EventBus eventBus)
    {
        _eventBus = eventBus;
        // Subscribe to all animation frame events globally
        _eventBus.Subscribe<AnimationFrameEvent>(OnAnimationFrame);
    }
    
    private void OnAnimationFrame(AnimationFrameEvent evt)
    {
        // Play sound based on event name
        if (evt.EventName.StartsWith("footstep"))
        {
            PlayFootstepSound(evt.Entity);
        }
    }
    
    public void OnShutdown()
    {
        _eventBus.Unsubscribe<AnimationFrameEvent>(OnAnimationFrame);
    }
}
```

#### 3. **UI Systems**

UI can react to animation completions (e.g., combo counters):

```csharp
public class ComboUISystem
{
    private EventBus _eventBus;
    
    public void Initialize(EventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<AnimationFrameEvent>(OnAnimationFrame);
    }
    
    private void OnAnimationFrame(AnimationFrameEvent evt)
    {
        if (evt.EventName == "hit_landed")
        {
            IncrementComboCounter();
        }
    }
}
```

#### 4. **Game State Controllers**

Manage game state transitions based on animations:

```csharp
public class BossStateController : ScriptableEntity
{
    public override void OnCreate()
    {
        var eventBus = Application.Instance.EventBus;
        eventBus.Subscribe<AnimationCompleteEvent>(OnAnimationComplete);
    }
    
    private void OnAnimationComplete(AnimationCompleteEvent evt)
    {
        if (evt.Entity != Entity)
            return;
            
        // State machine transitions based on animation completion
        if (evt.ClipName == "phase1_intro")
        {
            TransitionToPhase1Combat();
        }
        else if (evt.ClipName == "phase1_death")
        {
            StartPhase2();
        }
    }
}
```

## Event Flow Diagram

```
┌─────────────────┐
│ AnimationSystem │ (Priority: 198)
└────────┬────────┘
         │ Detects frame change with events
         │
         ▼
┌─────────────────┐
│    EventBus     │ (Publish)
└────────┬────────┘
         │
         ├──────────────┬────────────────┬──────────────┐
         ▼              ▼                ▼              ▼
    ┌─────────┐   ┌──────────┐    ┌──────────┐   ┌──────────┐
    │ Scripts │   │  Audio   │    │ Particle │   │    UI    │
    │(Player) │   │  System  │    │  System  │   │  System  │
    └─────────┘   └──────────┘    └──────────┘   └──────────┘
         │              │               │              │
         ▼              ▼               ▼              ▼
    Play attack    Play sound     Spawn effect   Update combo
```

## Integration with Application

The EventBus should be a singleton available through the Application:

```csharp
public class Application
{
    public static Application Instance { get; private set; }
    public EventBus EventBus { get; private set; }
    
    public void Initialize()
    {
        EventBus = new EventBus();
        
        // Register systems with EventBus
        var animationSystem = new AnimationSystem(context, EventBus);
        systemManager.RegisterSystem(animationSystem);
    }
}
```

## Best Practices

### ✅ DO:

* **Always unsubscribe in OnDestroy()** to prevent memory leaks
* Filter events by `evt.Entity` when handling entity-specific logic
* Use event name string constants to avoid typos
* Keep event handlers fast and simple
* Log errors in handlers instead of crashing the game

### ❌ DON'T:

* Don’t subscribe inside OnUpdate() (causes duplicates)
* Don’t forget to unsubscribe (memory leaks!)
* Don’t do heavy computations in handlers
* Don’t modify event data (events should be immutable)
* Don’t throw exceptions from handlers

## Common Use Cases

### 1. Combat System

```json
{
  "frames": [
    { "rect": [0, 0, 64, 64] },
    { "rect": [64, 0, 64, 64], "events": ["attack_start"] },
    { "rect": [128, 0, 64, 64], "events": ["attack_hit", "camera_shake"] },
    { "rect": [192, 0, 64, 64] }
  ]
}
```

### 2. Character Movement

```json
{
  "frames": [
    { "rect": [0, 0, 32, 32] },
    { "rect": [32, 0, 32, 32], "events": ["footstep_left"] },
    { "rect": [64, 0, 32, 32] },
    { "rect": [96, 0, 32, 32], "events": ["footstep_right"] }
  ]
}
```

### 3. Cutscenes

```json
{
  "frames": [
    { "rect": [0, 0, 128, 128], "events": ["dialogue_line_1"] },
    { "rect": [128, 0, 128, 128], "events": ["dialogue_line_2"] },
    { "rect": [256, 0, 128, 128], "events": ["cutscene_end"] }
  ]
}
```

## Thread Safety

The EventBus is thread-safe and can be accessed from any thread. However, handlers are executed on the same thread that publishes the event. Since the AnimationSystem runs on the game loop thread, all handlers are executed on the game loop thread.
