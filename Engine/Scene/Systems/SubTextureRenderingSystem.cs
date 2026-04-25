using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D subtextures (sprite atlas/sprite sheet regions).
/// </summary>
internal sealed class SubTextureRenderingSystem(IGraphics2D renderer, IContext context, IPrimaryCameraProvider cameraProvider) : ISystem
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
        if (cameraProvider.Camera == null)
            return;

        renderer.BeginScene(cameraProvider.Camera, cameraProvider.Transform);
        
        var subtextureGroup = context.View<SubTextureRendererComponent>();
        foreach (var (entity, subtextureComponent) in subtextureGroup)
        {
            if (subtextureComponent.Texture == null)
                continue;

            var transform = entity.GetWorldTransform(context);

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