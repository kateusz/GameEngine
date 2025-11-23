---
name: component-workflow
description: Guide the creation of new ECS components following established architectural patterns including component class creation, JSON serialization support, editor UI implementation, dependency injection registration, and documentation. Use when adding new component types to the engine or extending existing component functionality.
---

# Component Workflow

## Overview
This skill provides step-by-step guidance for adding new ECS components to the game engine, ensuring consistency with architectural patterns, serialization, editor integration, and dependency injection.

## When to Use
Invoke this skill when:
- Adding a new component type to the engine
- Extending existing component functionality
- Questions about component implementation patterns
- Implementing component serialization
- Creating component editors for the Properties panel
- Registering components in the dependency injection container

## Component Creation Workflow

### Step 1: Create Component Class
**Location**: `Engine/Scene/Components/`

**Guidelines**:
- Components should be data-only classes (minimal logic)
- Use properties for data fields
- Provide sensible defaults
- Keep components small and focused
- Prefer value types (structs/record structs) for small components
- Use reference types (classes/records) for larger components
- Matrix/transform calculations are acceptable in components

**Naming Convention**:
- Suffix with "Component": `MyNewComponent`
- Use PascalCase: `AudioSourceComponent`, `TransformComponent`

**Example Component**:
```csharp
namespace Engine.Scene.Components;

/// <summary>
/// Represents a particle emitter component for visual effects.
/// </summary>
public class ParticleEmitterComponent
{
    /// <summary>
    /// Maximum number of particles to emit simultaneously.
    /// </summary>
    public int MaxParticles { get; set; } = 100;

    /// <summary>
    /// Particle emission rate (particles per second).
    /// </summary>
    public float EmissionRate { get; set; } = 10.0f;

    /// <summary>
    /// Lifetime of each particle in seconds.
    /// </summary>
    public float ParticleLifetime { get; set; } = 2.0f;

    /// <summary>
    /// Starting color of particles.
    /// </summary>
    public Vector4 StartColor { get; set; } = Vector4.One;

    /// <summary>
    /// Ending color of particles (for fade effects).
    /// </summary>
    public Vector4 EndColor { get; set; } = new Vector4(1, 1, 1, 0);

    /// <summary>
    /// Whether the emitter is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
```

**For Small Components** (use record struct):
```csharp
namespace Engine.Scene.Components;

/// <summary>
/// Simple velocity component for 2D movement.
/// </summary>
public record struct VelocityComponent(Vector2 Velocity);
```

### Step 2: Implement Serialization Support
**Location**: `Engine/Scene/Serializer/` (if custom converter needed)

**Standard Serialization** (automatic):
Most components work with default JSON serialization. The `SceneSerializer` handles standard properties automatically.

**Custom Serialization** (when needed):
- Complex types (e.g., `TileMapComponent`, `AnimationComponent`)
- Resource references (textures, audio clips)
- Specialized data structures

**Example Custom Converter**:
```csharp
namespace Engine.Scene.Serializer;

public class ParticleEmitterComponentConverter : JsonConverter<ParticleEmitterComponent>
{
    public override ParticleEmitterComponent Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // Custom deserialization logic
        var component = new ParticleEmitterComponent();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return component;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case "MaxParticles":
                        component.MaxParticles = reader.GetInt32();
                        break;
                    case "EmissionRate":
                        component.EmissionRate = reader.GetSingle();
                        break;
                    // ... other properties
                }
            }
        }

        return component;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ParticleEmitterComponent value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("MaxParticles", value.MaxParticles);
        writer.WriteNumber("EmissionRate", value.EmissionRate);
        writer.WriteNumber("ParticleLifetime", value.ParticleLifetime);
        // ... other properties
        writer.WriteEndObject();
    }
}
```

**Register Converter** (if custom):
Add to `SceneSerializer` or serialization configuration:
```csharp
options.Converters.Add(new ParticleEmitterComponentConverter());
```

### Step 3: Create Component Editor
**Location**: `Editor/ComponentEditors/` (or inline in Properties panel)

**Guidelines**:
- Use `EditorUIConstants` for all UI dimensions and spacing
- Follow ImGui patterns from existing editors
- Provide clear labels and tooltips
- Validate input ranges
- Use appropriate input controls (DragFloat, ColorEdit, Checkbox)

**Example Component Editor**:
```csharp
namespace Editor.ComponentEditors;

using Editor.UI;
using ImGuiNET;

public class ParticleEmitterComponentEditor
{
    public static void DrawEditor(ParticleEmitterComponent component)
    {
        ImGui.Separator();
        ImGui.Text("Particle Emitter");
        ImGui.Spacing();

        // Use EditorUIConstants for consistent sizing
        float labelWidth = ImGui.GetContentRegionAvail().X * EditorUIConstants.PropertyLabelRatio;

        // Max Particles
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Max Particles");
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(-1);
        int maxParticles = component.MaxParticles;
        if (ImGui.DragInt("##MaxParticles", ref maxParticles, 1, 1, 10000))
        {
            component.MaxParticles = Math.Max(1, maxParticles);
        }

        // Emission Rate
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Emission Rate");
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(-1);
        float emissionRate = component.EmissionRate;
        if (ImGui.DragFloat("##EmissionRate", ref emissionRate, 0.1f, 0.0f, 1000.0f))
        {
            component.EmissionRate = Math.Max(0.0f, emissionRate);
        }

        // Particle Lifetime
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Lifetime");
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(-1);
        float lifetime = component.ParticleLifetime;
        if (ImGui.DragFloat("##Lifetime", ref lifetime, 0.01f, 0.01f, 60.0f))
        {
            component.ParticleLifetime = Math.Max(0.01f, lifetime);
        }

        // Colors
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Start Color");
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(-1);
        Vector4 startColor = component.StartColor;
        if (ImGui.ColorEdit4("##StartColor", ref startColor))
        {
            component.StartColor = startColor;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("End Color");
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(-1);
        Vector4 endColor = component.EndColor;
        if (ImGui.ColorEdit4("##EndColor", ref endColor))
        {
            component.EndColor = endColor;
        }

        // Active Toggle
        ImGui.Spacing();
        bool isActive = component.IsActive;
        if (ImGui.Checkbox("Active", ref isActive))
        {
            component.IsActive = isActive;
        }

        ImGui.Separator();
    }
}
```

### Step 4: Register in Dependency Injection (if needed)
**Location**: `Editor/Program.cs` (for editor-specific components)

**When to Register**:
- Component has factory dependencies
- Component needs service injection
- Component editor requires dependencies

**Example Registration**:
```csharp
// In ConfigureServices method
container.Register<IComponentEditor<ParticleEmitterComponent>,
                   ParticleEmitterComponentEditor>(Reuse.Singleton);
```

**Note**: Most components don't require DI registration. Only register if the component or its editor needs injected dependencies.

### Step 5: Integrate with Properties Panel
**Location**: `Editor/Panels/PropertiesPanel.cs`

**Add Component Rendering**:
```csharp
if (entity.HasComponent<ParticleEmitterComponent>())
{
    var component = entity.GetComponent<ParticleEmitterComponent>();
    ParticleEmitterComponentEditor.DrawEditor(component);
}
```

**Add Component Menu**:
```csharp
if (ImGui.MenuItem("Particle Emitter"))
{
    selectedEntity.AddComponent<ParticleEmitterComponent>();
}
```

### Step 6: Create System (if needed)
**Location**: `Engine/Scene/Systems/`

**When to Create a System**:
- Component requires per-frame updates
- Component has logic that operates on entities
- Component needs to interact with rendering, physics, or other systems

**Example System**:
```csharp
namespace Engine.Scene.Systems;

public class ParticleSystem : ISystem
{
    public int Priority => 150; // Execute before rendering

    public void OnAttach(Scene scene) { }

    public void OnDetach(Scene scene) { }

    public void OnUpdate(Scene scene, TimeSpan deltaTime)
    {
        float dt = (float)deltaTime.TotalSeconds;

        foreach (var entity in scene.GetEntitiesWith<ParticleEmitterComponent>())
        {
            var emitter = entity.GetComponent<ParticleEmitterComponent>();

            if (!emitter.IsActive)
                continue;

            // Update particle logic here
            // Emit particles, update positions, handle lifetime, etc.
        }
    }

    public void OnEvent(Scene scene, Event e) { }
}
```

**Register System**:
```csharp
// In SceneSystemRegistry.cs
public static void RegisterDefaultSystems(SystemManager systemManager, IServiceProvider services)
{
    // ... existing systems
    systemManager.AddSystem(services.GetRequiredService<ParticleSystem>());
}

// In Program.cs (Engine or Editor)
container.Register<ParticleSystem>(Reuse.Singleton);
```

### Step 7: Update Documentation
**Location**: `docs/modules/ecs-gameobject.md`

**Add Component to List**:
Update the component list with:
- Component name and purpose
- Key properties
- Usage example
- Integration with systems

**Example Documentation Addition**:
```markdown
### ParticleEmitterComponent
Emits and manages particle effects for visual polish.

**Properties**:
- `MaxParticles`: Maximum simultaneous particles
- `EmissionRate`: Particles emitted per second
- `ParticleLifetime`: How long particles live
- `StartColor`, `EndColor`: Color gradient
- `IsActive`: Enable/disable emission

**Usage**:
```csharp
var entity = scene.CreateEntity("Explosion");
var emitter = entity.AddComponent<ParticleEmitterComponent>();
emitter.MaxParticles = 200;
emitter.EmissionRate = 50.0f;
emitter.StartColor = new Vector4(1, 0.5f, 0, 1); // Orange
emitter.EndColor = new Vector4(0.5f, 0, 0, 0);   // Fade to transparent red
```

**System**: `ParticleSystem` (Priority: 150)
```

## Checklist for Component Creation

- [ ] Component class created in `Engine/Scene/Components/`
- [ ] XML documentation comments added
- [ ] Serialization support implemented (custom converter if needed)
- [ ] Component editor created with `EditorUIConstants`
- [ ] Integrated into `PropertiesPanel` component list
- [ ] Add component menu item added
- [ ] System created (if component needs updates)
- [ ] System registered in `SceneSystemRegistry` (if applicable)
- [ ] DI registration added to `Program.cs` (if needed)
- [ ] Documentation updated in `docs/modules/ecs-gameobject.md`
- [ ] Component tested in editor (add, edit, save, load)
- [ ] Serialization tested (save scene, reload, verify values)

## Reference Documentation
- **Architecture**: `CLAUDE.md` - Component design patterns
- **Module Docs**: `docs/modules/ecs-gameobject.md` - All 18 existing components
- **Serialization**: `Engine/Scene/Serializer/` - Custom converters
- **Editor UI**: `Editor/UI/EditorUIConstants.cs` - UI constants
- **Properties Panel**: `Editor/Panels/PropertiesPanel.cs` - Integration point

## Integration with Agents
This skill works with both **game-engine-expert** (for component/system implementation) and **game-editor-architect** (for editor UI integration). Use this skill to plan the workflow, then delegate to appropriate agents for implementation.

## Common Patterns

### Component with Resource Reference
```csharp
public class MeshFilterComponent
{
    public string MeshPath { get; set; } = string.Empty;

    [JsonIgnore]
    public Mesh? LoadedMesh { get; set; }
}
```

### Component with Nested Data
```csharp
public class AnimationComponent
{
    public string AssetPath { get; set; } = string.Empty;
    public List<AnimationClip> Clips { get; set; } = new();
    public int CurrentClipIndex { get; set; } = 0;
    public bool IsPlaying { get; set; } = false;
}
```

### Component with Callbacks
```csharp
public class ButtonComponent
{
    public Vector2 Size { get; set; } = new(100, 50);

    [JsonIgnore]
    public Action? OnClick { get; set; }
}
```

## Performance Considerations

- **Avoid allocations**: Don't create objects in component properties
- **Keep components small**: Large components hurt cache coherency
- **Use value types**: When component is small (<= 16 bytes)
- **Minimize references**: Each reference is a pointer chase
- **Group related data**: Components accessed together should be similar size

## Tool Restrictions
None - this skill may create files, edit code, and update documentation as needed for complete component implementation.
