using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D subtextures (sprite atlas/sprite sheet regions).
/// </summary>
internal sealed class SubTextureRenderingSystem(IGraphics2D renderer, IContext context) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SubTextureRenderingSystem>();

    public int Priority => SystemPriorities.SubTextureRenderSystem;
    
    public void OnInit()
    {
        Logger.Debug("SubTextureRenderingSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Updates and renders all subtextures in the scene.
    /// Finds the primary camera and renders all entities with subtexture components.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        Camera? mainCamera = null;
        var cameraGroup = context.View<CameraComponent>();
        var cameraTransform = Matrix4x4.Identity;

        foreach (var (entity, cameraComponent) in cameraGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (cameraComponent.Primary)
            {
                mainCamera = cameraComponent.Camera;
                cameraTransform = transformComponent.GetTransform();
                break;
            }
        }
        
        if (mainCamera == null)
            return;
        
        renderer.BeginScene(mainCamera, cameraTransform);
        
        var subtextureGroup = context.View<SubTextureRendererComponent>();
        foreach (var (entity, subtextureComponent) in subtextureGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            
            if (subtextureComponent.Texture == null)
                continue;

            // Use the transform component's matrix directly (same as SpriteRenderingSystem)
            // The transform scale determines the world-space size of the rendered sprite
            var transform = transformComponent.GetTransform();

            // Use pre-calculated TexCoords if available (e.g., from animation system)
            // Otherwise calculate from grid coordinates
            Vector2[] texCoords;
            if (subtextureComponent.TexCoords != null)
            {
                // Direct UV coordinates (used by animation system)
                texCoords = subtextureComponent.TexCoords;
            }
            else
            {
                // Calculate from grid coordinates (traditional subtexture rendering)
                var subTexture = SubTexture2D.CreateFromCoords(
                    subtextureComponent.Texture,
                    subtextureComponent.Coords,
                    subtextureComponent.CellSize,
                    subtextureComponent.SpriteSize
                );
                texCoords = subTexture.TexCoords;
            }
            
            // Draw the subtexture quad with entity ID for picking
            renderer.DrawQuad(transform, subtextureComponent.Texture, texCoords, 1.0f, Vector4.One, entity.Id);
        }

        renderer.EndScene();
    }
    
    public void OnShutdown() {}
}