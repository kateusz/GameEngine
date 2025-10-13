# Input System Architecture

## Overview

The Input System is a core component of the game engine that provides platform-independent input handling through a layered architecture. It supports keyboard and mouse input with a robust event-driven system that ensures thread-safe operation and proper event distribution throughout the application layers.

## Architecture Overview

The Input System follows a layered architecture design that separates concerns and provides platform independence through well-defined interfaces and abstractions.

```mermaid
graph TB
    subgraph "Application Layer"
        App[Application]
        LayerStack[Layer Stack]
        GameLayer[Game Layers]
        UILayer[UI/ImGui Layers]
    end

    subgraph "Input System Core"
        InputSystem[Input System Interface]
        EventDispatcher[Event Dispatcher]
        InputDevices[Input Device Abstractions]
    end

    subgraph "Event System"
        EventQueue[Thread-Safe Event Queue]
        InputEvents[Input Events]
        WindowEvents[Window Events]
    end

    subgraph "Platform Abstraction"
        PlatformInput[Platform Input Layer]
        PlatformCallbacks[Platform Callbacks]
    end

    subgraph "Hardware Layer"
        Hardware[Hardware Input Devices]
    end

    Hardware --> PlatformCallbacks
    PlatformCallbacks --> PlatformInput
    PlatformInput --> EventQueue
    EventQueue --> InputSystem
    InputSystem --> EventDispatcher
    EventDispatcher --> App
    App --> LayerStack
    LayerStack --> UILayer
    LayerStack --> GameLayer

    InputEvents --> EventQueue
    WindowEvents --> EventQueue
    InputDevices --> InputSystem

    classDef coreLayer fill:#e1f5fe
    classDef eventLayer fill:#f3e5f5
    classDef platformLayer fill:#fff3e0
    classDef hardwareLayer fill:#e8f5e8

    class InputSystem,EventDispatcher,InputDevices coreLayer
    class EventQueue,InputEvents,WindowEvents eventLayer
    class PlatformInput,PlatformCallbacks platformLayer
    class Hardware hardwareLayer
```

## Core Components

### Input System Interface

The central coordinator that manages all input operations and provides the primary interface between the platform layer and the application. It handles periodic updates, event distribution, and resource management while maintaining platform independence.

**Key Responsibilities:**
- Manages the input context from the underlying platform
- Provides periodic updates to process queued input events
- Exposes events for input notifications
- Handles resource cleanup and lifecycle management

## Event Flow Architecture

The event processing follows a clear pipeline that ensures thread safety and proper event distribution:

```mermaid
sequenceDiagram
    participant HW as Hardware
    participant PL as Platform Layer
    participant EQ as Event Queue
    participant IS as Input System
    participant APP as Application
    participant LS as Layer Stack

    HW->>PL: Raw Input Event
    PL->>PL: Translate to Engine Event
    PL->>EQ: Enqueue Event (Thread-Safe)

    Note over EQ: Events queued until next update cycle

    APP->>IS: Update() Call
    IS->>EQ: Dequeue All Events
    EQ-->>IS: Batched Events
    IS->>APP: Dispatch Events

    APP->>LS: Distribute to Layers (Reverse Order)

    alt Event Handled by UI Layer
        LS->>LS: Mark Event as Handled
        LS->>LS: Stop Propagation
    else Event Not Handled
        LS->>LS: Continue to Game Layers
    end
```

### Event Processing Principles

1. **Hardware Input Capture**: Raw input events are captured by the platform layer
2. **Event Translation**: Platform events are converted to engine-specific event objects
3. **Thread-Safe Queuing**: Events are queued using thread-safe mechanisms for processing
4. **Processing**: All queued events are processed during the application update cycle
5. **Layer Distribution**: Events are distributed to application layers in priority order
6. **Event Handling**: Layers can mark events as handled to prevent further propagation

## Application Integration

### Layer System Architecture

The layer system provides a structured approach to event handling with clear priority ordering:

```mermaid
graph TD
    subgraph "Layer Processing Order"
        direction TB
        UI[UI/ImGui Layers]
        Overlay[Overlay Layers]
        Game[Game Logic Layers]
        Background[Background Layers]
    end

    EventInput[Input Events] --> UI
    UI --> |If Not Handled| Overlay
    Overlay --> |If Not Handled| Game
    Game --> |If Not Handled| Background

    UI --> |Event Handled| Stop[Stop Propagation]
    Overlay --> |Event Handled| Stop
    Game --> |Event Handled| Stop
    Background --> |Event Handled| Stop

    classDef processLayer fill:#e8f5e8
    classDef stopNode fill:#ffebee

    class UI,Overlay,Game,Background processLayer
    class Stop stopNode
```

**Event Handling Pattern:**
1. Events are processed in reverse layer order (overlay layers first)
2. UI/ImGui layers typically have highest priority
3. Game layers receive events after UI layers
4. Events can be marked as handled to stop propagation
5. This ensures UI interactions take precedence over game input

## Thread Safety and Performance

### Architectural Design for Performance

The Input System is designed with performance and thread safety as primary concerns:

**Thread Safety Measures:**
- Concurrent queuing mechanisms for thread-safe event handling
- Immutable event structures prevent accidental modification
- Proper disposal patterns ensure resource cleanup

**Performance Optimizations:**
- Minimal memory allocations through efficient event structures
- Lock-free queuing operations where possible
- Batch processing of events reduces per-frame overhead
- Early termination of event propagation when handled

## Extensibility and Future Considerations

### Architectural Extensibility

The current architecture provides several extension points for future enhancements:

1. **Input Device Expansion**: The interface-based design allows for additional input devices
2. **Platform Support**: New platforms can implement the platform abstraction layer
3. **Event Type Extensions**: The hierarchical event system supports new event types
4. **Processing Pipeline Customization**: The layered approach allows for custom processing stages

### Potential Enhancements

- **Controller Support**: Game controller integration through the existing interface patterns
- **Touch Input**: Mobile platform support with touch-specific event types
- **Input Mapping Systems**: Higher-level input mapping built on the current foundation
- **Custom Input Devices**: Support for specialized input hardware

---

*This documentation outlines the high-level architecture of the Input System, focusing on design principles, component relationships, and extensibility patterns rather than implementation details.*