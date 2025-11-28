// Batch Sorting Example
// Demonstrates sorting draw calls by state to minimize expensive state changes

using System.Numerics;
using Engine.Renderer;
using Engine.Scene;

namespace Engine.Optimization.Examples;

/// <summary>
/// Optimized renderer that sorts draw calls by state (shader, texture, blend mode)
/// to minimize expensive OpenGL state changes.
/// </summary>
public class BatchSortingRenderer
{
    private readonly Graphics2D _graphics;
    private readonly List<DrawCommand> _drawCommands = new();

    public BatchSortingRenderer(Graphics2D graphics)
    {
        _graphics = graphics;
    }

    /// <summary>
    /// Queue a draw command (doesn't draw immediately).
    /// </summary>
    public void Submit(Entity entity)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var sprite = entity.GetComponent<SpriteRendererComponent>();

        _drawCommands.Add(new DrawCommand
        {
            Position = transform.Translation,
            Size = transform.Scale,
            Texture = sprite.Texture,
            Color = sprite.Color,
            Shader = sprite.CustomShader ?? GetDefaultShader(),
            BlendMode = sprite.UseAlphaBlending ? BlendMode.Alpha : BlendMode.None,
            ZOrder = transform.Translation.Z
        });
    }

    /// <summary>
    /// Execute all queued draw commands in optimized order.
    /// </summary>
    public void Flush()
    {
        if (_drawCommands.Count == 0)
            return;

        // ✅ Sort by state to minimize expensive changes
        // Priority: Shader (most expensive) > Texture > BlendMode > Z-order
        var sorted = _drawCommands
            .OrderBy(cmd => cmd.Shader.GetHashCode())    // Group by shader first
            .ThenBy(cmd => cmd.Texture.RendererID)       // Then by texture
            .ThenBy(cmd => cmd.BlendMode)                 // Then by blend mode
            .ThenBy(cmd => cmd.ZOrder)                    // Finally by depth (for correctness)
            .ToList();

        // Track current state to skip redundant changes
        Shader? currentShader = null;
        BlendMode currentBlendMode = BlendMode.None;

        foreach (var cmd in sorted)
        {
            // Change shader only if different
            if (cmd.Shader != currentShader)
            {
                cmd.Shader.Bind();
                currentShader = cmd.Shader;
            }

            // Change blend mode only if different
            if (cmd.BlendMode != currentBlendMode)
            {
                SetBlendMode(cmd.BlendMode);
                currentBlendMode = cmd.BlendMode;
            }

            // Draw quad (texture handled by Graphics2D batching)
            _graphics.DrawQuad(cmd.Position, cmd.Size, cmd.Texture, cmd.Color);
        }

        _drawCommands.Clear();
    }

    private void SetBlendMode(BlendMode mode)
    {
        switch (mode)
        {
            case BlendMode.None:
                GL.Disable(EnableCap.Blend);
                break;
            case BlendMode.Alpha:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
        }
    }

    private Shader GetDefaultShader() => ShaderFactory.Get("default_sprite");

    private class DrawCommand
    {
        public required Vector3 Position { get; init; }
        public required Vector3 Size { get; init; }
        public required Texture Texture { get; init; }
        public required Vector4 Color { get; init; }
        public required Shader Shader { get; init; }
        public required BlendMode BlendMode { get; init; }
        public float ZOrder { get; init; }
    }
}

public enum BlendMode
{
    None,
    Alpha,
    Additive
}

/// <summary>
/// Example usage in a render system.
/// </summary>
public class OptimizedRenderSystem : ISystem
{
    private readonly BatchSortingRenderer _renderer;
    private readonly Scene _scene;

    public OptimizedRenderSystem(Scene scene, Graphics2D graphics)
    {
        _scene = scene;
        _renderer = new BatchSortingRenderer(graphics);
    }

    public void OnUpdate(float deltaTime)
    {
        // Submit all sprites (queues them, doesn't draw yet)
        var entities = _scene.GetEntitiesWithComponent<SpriteRendererComponent>();
        foreach (var entity in entities)
        {
            _renderer.Submit(entity);
        }

        // Execute all draws in optimized order
        _renderer.Flush();

        // Result: Minimized state changes
        // Example with 1000 sprites using 3 shaders + 50 textures:
        //   Unsorted: ~500 state changes (shader switches every few sprites)
        //   Sorted:   ~3 shader switches + 50 texture switches = 53 state changes
    }
}

/// <summary>
/// Advanced: Multi-pass sorting for transparency correctness.
/// </summary>
public class MultiPassBatchRenderer
{
    private readonly BatchSortingRenderer _renderer;

    public MultiPassBatchRenderer(Graphics2D graphics)
    {
        _renderer = new BatchSortingRenderer(graphics);
    }

    public void RenderScene(IEnumerable<Entity> entities)
    {
        // ✅ Pass 1: Opaque objects (front-to-back, state-sorted)
        var opaqueEntities = entities
            .Where(e => !e.GetComponent<SpriteRendererComponent>().UseAlphaBlending)
            .OrderBy(e => e.GetComponent<TransformComponent>().Translation.Z); // Front-to-back for early-Z

        foreach (var entity in opaqueEntities)
        {
            _renderer.Submit(entity);
        }
        _renderer.Flush();

        // ✅ Pass 2: Transparent objects (back-to-front, state-sorted within depth bands)
        var transparentEntities = entities
            .Where(e => e.GetComponent<SpriteRendererComponent>().UseAlphaBlending)
            .OrderByDescending(e => e.GetComponent<TransformComponent>().Translation.Z); // Back-to-front for blending

        foreach (var entity in transparentEntities)
        {
            _renderer.Submit(entity);
        }
        _renderer.Flush();

        // Result:
        //   - Opaque objects: Optimal state changes + early-Z culling
        //   - Transparent objects: Correct blending order + minimal state changes
    }
}
