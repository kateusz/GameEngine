# ECS/GameObject Architecture

## Core Concepts

### Entity as a Container

An entity is an empty vessel that you fill with capabilities. The entity itself has:

- **Identity**: A unique ID and a human-readable name
- **Component Storage**: An internal dictionary mapping component types to instances
- **Events**: Notification hooks when components are added
- **Query Interface**: Methods to check, add, remove, and retrieve components

An entity without components is valid but does nothing - it's the components that give entities meaning and functionality.

#### Entity ID Generation

Entity IDs use sequential generation maintained by each Scene instance:

- **Sequential Generation**: IDs start at 1 and increment for each new entity
- **Collision-Free**: Sequential generation eliminates ID collision possibilities
- **Deterministic**: Same entity creation order produces same IDs
- **Serialization-Safe**: When loading scenes, the Scene tracks the highest ID and continues from there
- **Scene-Local**: Each Scene instance maintains its own ID counter

### Component as Pure Data

Components are simple data holders implementing the `IComponent` marker interface. They describe "what something is" rather than "what something does":

| Component | Purpose |
|-----------|---------|
| Transform | Position, rotation, scale in 3D space |
| SpriteRenderer | Visual appearance (texture, color, tiling) |
| SubTextureRenderer | Sprite atlas region rendering |
| RigidBody2D | Physics simulation properties |
| BoxCollider2D | Collision shape for physics |
| Camera | View projection settings |
| NativeScript | Reference to behavioral logic |
| Animation | Sprite animation state |
| TileMap | Grid-based tilemap data with layers |
| AudioSource | Sound playback properties |
| AudioListener | 3D audio listener position |

### Context as the World Registry

The Context class (implementing `IContext`) acts as the "world database" of entities:

- Maintains a thread-safe collection of all living entities
- Provides query methods to find entities with specific component patterns
- Enables systems to efficiently iterate over relevant entities
- Serves as the single source of truth for entity existence

### Scriptable Entity as Behavior

While components store data, scripts provide behavior. The `ScriptableEntity` base class:

- Connects to an entity instance
- Receives lifecycle callbacks (OnCreate, OnUpdate, OnDestroy)
- Provides convenience methods to access and manipulate components
- Handles input events (keyboard, mouse) and physics events (collisions, triggers)
- Compiles dynamically at runtime for hot-reload support

Scripts are attached to entities via the `NativeScriptComponent`, bridging the pure data world (ECS) with the behavioral world (scripts).

### Query System: Views and Groups

The engine provides two patterns for querying entities:

**Group Query** (`GetGroup`): Returns all entities possessing a specific set of component types. Use for batch processing (rendering all sprites, updating all physics bodies).

**View Query** (`View<T>`): Returns entity-component pairs for a specific component type. Use when processing entities with a particular component and needing direct data access.

Both queries iterate the entity collection and check component presence via dictionary lookups.

---

## Entity Composition Patterns

### Common Entity Archetypes

| Archetype | Components | Purpose |
|-----------|------------|---------|
| Empty Entity | None | Placeholder or grouping node |
| Visual Entity | Transform + SpriteRenderer | Something visible in the world |
| 3D Model Entity | Transform + Mesh + ModelRenderer | 3D object with geometry |
| Camera Entity | Transform + Camera | Player's viewpoint |
| Physics Entity | Transform + RigidBody2D + BoxCollider2D | Physics simulation participant |
| Interactive Entity | Any + NativeScriptComponent | Object with custom behavior |
| Animated Entity | Transform + SubTextureRenderer + Animation | Frame-based animation |
| TileMap Entity | Transform + TileMapComponent | Grid-based level/map |
| Audio Entity | Transform + AudioSourceComponent | Positioned sound source |
| Complete Game Object | Transform + Sprite + RigidBody + Collider + Script | Full interactive physics object |

### Component Dependencies

Certain components implicitly depend on others:

- **Renderers require Transform**: Position/rotation/scale needed for drawing
- **Physics requires Transform**: Initial pose from transform
- **Colliders require RigidBody**: Box2D colliders attach to physics bodies
- **Scripts often need Transform**: Most behaviors manipulate position
- **Animation requires SubTextureRenderer**: AnimationSystem updates SubTextureRendererComponent
- **TileMap requires Transform**: World position from TransformComponent

The engine doesn't enforce these dependencies automatically - missing dependencies typically result in null references or no effect.
