# Animation Event System

## Overview

The animation event system uses the **publish-subscribe** pattern via an internal `EventBus`, enabling loose coupling between the animation system and other engine systems.

## Architecture

### Event Types

- **AnimationFrameEvent** – Published when an animation reaches a frame with defined events. Contains entity, clip name, event name, frame index, and frame data.
- **AnimationCompleteEvent** – Published when a non-looping animation finishes. Contains entity and clip name.

### EventBus

The `EventBus` is an internal engine service registered via DI that provides subscribe, unsubscribe, and publish operations for typed events.

## Event Flow

```
AnimationSystem (Priority: 140)
         │
         │ Detects frame change with events
         ▼
      EventBus
         │
         ├──────────┬────────────┬──────────┐
         ▼          ▼            ▼          ▼
      Combat     Audio      Particle      UI
      System     System      System     System
```

## Consumers

Animation events are consumed by internal engine systems that receive `EventBus` via dependency injection. Typical subscribers include audio systems (footsteps), particle systems (hit effects), combat systems (attack timing), and camera systems (screen shake).

Systems subscribe during `OnInit()` and must unsubscribe during `OnShutdown()` to prevent memory leaks.

## Defining Animation Events

Animation events are defined per-frame in the animation JSON file using the `events` array property. Use consistent naming conventions across projects (e.g., `footstep_left`, `attack_hit`, `spawn_particles`).

## Best Practices

- Always unsubscribe in `OnShutdown()` to prevent memory leaks
- Filter events by entity when handling entity-specific logic
- Keep event handlers fast – no heavy computation
- Never subscribe inside `OnUpdate()` (causes duplicates)
- Treat events as immutable – don't modify event data

## Thread Safety

The EventBus is thread-safe. Handlers execute on the game loop thread since `AnimationSystem` runs there.
