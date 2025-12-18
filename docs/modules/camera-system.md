# Camera System

## Core Concepts

### Projection Types

- **Orthographic Projection**: Maintains parallel lines and consistent object sizes regardless of depth. Ideal for 2D games, UI, and technical visualizations.
- **Perspective Projection**: Simulates human vision with foreshortening, where distant objects appear smaller. Essential for 3D games.

### Primary Camera Pattern

In a scene with multiple cameras, only one camera is designated as "primary" and actively used for rendering.

### Lazy Projection Calculation

The camera uses a dirty flag pattern to optimize projection matrix calculation. The matrix is only recalculated when properties change AND the projection is accessed, avoiding unnecessary computation during batched property updates.

## Architecture

Cameras exist as ECS components attached to entities, allowing them to:
- Inherit position, rotation, and transformations from the entity's transform
- Save/load automatically with scenes via serialization
- Support scripted camera movement through standard script components

## Runtime Behavior

### Frame Rendering

1. Scene discovers the primary camera by querying camera components
2. Camera entity's transform provides the view matrix
3. Camera component provides the projection matrix
4. Renderer combines matrices and applies to all objects

### Viewport Resize Handling

When the viewport resizes, cameras without a fixed aspect ratio automatically update their aspect ratio and mark their projection for recalculation.

### Camera Switching

To switch cameras at runtime, set the current primary camera's flag to false and the desired camera's flag to true. The next frame renders from the new perspective.

## Integration Points

- **Scene**: Queries for cameras, determines primary, passes to renderers, notifies of viewport changes
- **Renderer**: Both 2D and 3D systems accept a camera for rendering, using its matrices for GPU uniforms
- **Editor**: Uses its own orthographic camera for the scene viewport, taking precedence over scene cameras in edit mode

## Configuration Defaults

| Setting | Perspective | Orthographic |
|---------|-------------|--------------|
| Size/FOV | 45° | 10 units |
| Near Clip | 0.01 | Platform-dependent |
| Far Clip | 1000 | Platform-dependent |
| Aspect Ratio | 16:9 (default) | 16:9 (default) |
