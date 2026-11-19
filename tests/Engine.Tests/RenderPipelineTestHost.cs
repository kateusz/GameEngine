using ECS;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;

namespace Engine.Tests;

/// <summary>A test owns all render hosts it creates and retires them at teardown.</summary>
internal sealed class RenderPipelineTestHost : IDisposable
{
    private readonly Dictionary<IGraphics3D, SceneRenderPipeline> _pipelines = new(ReferenceEqualityComparer.Instance);

    internal void RenderScene(Context context, IGraphics2D graphics2D, IGraphics3D graphics3D,
        ITextureFactory textureFactory, IModelFactory? modelFactory, in SceneView view,
        IVertexArrayFactory? vertexArrayFactory = null, IVertexBufferFactory? vertexBufferFactory = null,
        IIndexBufferFactory? indexBufferFactory = null)
    {
        if (!_pipelines.TryGetValue(graphics3D, out var pipeline))
        {
            pipeline = new(graphics2D, graphics3D, textureFactory, modelFactory,
                vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
            _pipelines.Add(graphics3D, pipeline);
        }
        pipeline.RenderScene(context, view);
    }

    public void Dispose()
    {
        foreach (var pipeline in _pipelines.Values) pipeline.Dispose();
        _pipelines.Clear();
    }
}
