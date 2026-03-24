using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Profiling;

namespace Engine.Renderer.Buffers;

internal sealed class VertexBufferFactory(IRendererApiConfig apiConfig, IPerformanceProfiler profiler) : IVertexBufferFactory
{
    public IVertexBuffer Create(uint size)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new OpenGLVertexBuffer(size, profiler),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
