using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

public class UniformBufferFactory
{
    public static IUniformBuffer Create(uint size, uint binding)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetUniformBuffer(size, binding),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}