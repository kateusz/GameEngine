# ECS Overview

Understand the Entity Component System architecture that powers the engine.

## What is ECS

The engine uses an **Entity Component System** (ECS) architecture. This is a data-driven design pattern where game objects are built by composing small, reusable pieces rather than inheriting from deep class hierarchies.

The three core concepts:

- **Entities** are containers. Think of them as empty game objects with a name and a unique ID. On their own, they do nothing.
- **Components** are data. You attach components to entities to give them capabilities. A `TransformComponent` gives an entity a position. A `SpriteRendererComponent` makes it visible. Components hold data, not logic.
- **Systems** are logic. The engine runs systems automatically every frame, processing entities that have specific combinations of components. For example, the physics system processes all entities that have both a `RigidBody2DComponent` and a `BoxCollider2DComponent`.

## How It Differs from Traditional OOP

In a traditional object-oriented game engine, you might have a `Player` class that inherits from `Character`, which inherits from `GameObject`. Behavior and data are mixed together in the class hierarchy.

In ECS, a player is just an entity with components attached:
- `TransformComponent` -- position in the world
- `SpriteRendererComponent` -- how it looks
- `RigidBody2DComponent` -- physics behavior
- `NativeScriptComponent` -- custom game logic

This is composition over inheritance. You build game objects by mixing and matching components instead of designing class hierarchies.

## What You Need to Know

As a game developer using this engine:

- **You create entities** in the editor's Scene Hierarchy panel (or from scripts with `CreateEntity`)
- **You attach components** via the Properties panel's "Add Component" button
- **Systems run automatically** -- the engine handles physics, rendering, audio, and animation
- **For custom logic, you write scripts** -- subclass `ScriptableEntity` and override lifecycle methods

You write scripts, not systems. Scripts are the user-facing API for game logic.

## Entities

Entities are created in the Scene Hierarchy panel by right-clicking and selecting "Create Entity." Each entity has:

- A **name** (e.g., "Player", "Enemy", "MainCamera") -- for identification
- A **unique ID** -- assigned automatically, used internally

From scripts, you can create and find entities:

```csharp
var enemy = CreateEntity("Enemy");
var player = FindEntity("Player");
```

## Components

Components are added via the Properties panel. Select an entity, click "Add Component," and choose from the dropdown. Each entity can have at most one component of each type.

The engine provides 13 built-in component types covering transforms, rendering, physics, audio, animation, scripting, and more. See the [Component Inspector](../editor/component-inspector.md) for the full reference.

## Next Steps

- [Component Inspector](../editor/component-inspector.md) -- all component types and their properties
- [Scripting Getting Started](../scripting/getting-started.md) -- write custom game logic
