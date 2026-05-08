using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene.Serializer;
using SceneComponents;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D sprites.
/// </summary>
internal sealed class SpriteRenderingSystem(
    IGraphics2D renderer,
    ITextureFactory? textureFactory,
    IContext context,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SpriteRenderingSystem>();
    private static readonly System.Numerics.Vector2[] DefaultTextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];

    public int Priority => SystemPriorities.SpriteRenderSystem;

    public void OnInit()
    {
        Logger.Debug("SpriteRenderingSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Updates and renders all sprites in the scene.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        if (cameraProvider.Camera == null)
            return;

        renderer.BeginScene(cameraProvider.Camera, cameraProvider.Transform);

        var spriteGroup = context.View<SpriteRendererComponent>();
        foreach (var (entity, spriteRendererComponent) in spriteGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            Texture2D? texture = null;
            if (textureFactory != null && !string.IsNullOrWhiteSpace(spriteRendererComponent.TexturePath))
                texture = textureFactory.Create(PathBuilder.Build(spriteRendererComponent.TexturePath));

            if (texture is not null)
                renderer.DrawQuad(transformComponent.GetTransform(), texture, DefaultTextureCoords,
                    spriteRendererComponent.TilingFactor, spriteRendererComponent.Color, entity.Id);
            else
                renderer.DrawQuad(transformComponent.GetTransform(), spriteRendererComponent.Color, entity.Id);
        }
        
        renderer.EndScene();
    }
    
    public void OnShutdown() {}
}
