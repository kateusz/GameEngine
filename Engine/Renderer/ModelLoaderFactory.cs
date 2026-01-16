using Engine.Platform.Assimp;
using Engine.Renderer.Buffers;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

internal sealed class ModelLoaderFactory(
    IRendererApiConfig apiConfig,
    ITextureFactory textureFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IModelLoaderFactory
{
    public IModelLoader Create()
    {
        return apiConfig.Type switch
        {
            ApiType.OpenGL => new AssimpModelLoader(
                textureFactory,
                vertexArrayFactory,
                vertexBufferFactory,
                indexBufferFactory),
            _ => throw new NotSupportedException($"No model loader for API type: {apiConfig.Type}")
        };
    }
}
