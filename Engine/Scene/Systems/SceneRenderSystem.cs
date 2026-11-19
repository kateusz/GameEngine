using ECS;
using ECS.Systems;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;

namespace Engine.Scene.Systems;

internal sealed class SceneRenderSystem(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    Context context,
    IModelFactory modelFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory,
    LodViewportHeight lodViewportHeight,
    IFrameBufferFactory frameBufferFactory) : ISystem
{
    private readonly SceneRenderPipeline _pipeline = new(graphics2D, graphics3D, textureFactory, modelFactory,
        vertexArrayFactory, vertexBufferFactory, indexBufferFactory, frameBufferFactory);

    public void OnShutdown() => _pipeline.Dispose();

    public int Priority => 150;

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!CameraQueries.TryGetPrimaryView(context, lodViewportHeight.Resolve(), out var view))
            return;

        _pipeline.RenderScene(context, view);
    }
}
