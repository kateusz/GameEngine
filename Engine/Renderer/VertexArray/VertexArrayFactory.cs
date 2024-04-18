using Engine.Platform.OpenTK;
using Engine.Platform.SilkNet;

namespace Engine.Renderer.VertexArray;

public static class VertexArrayFactory
{
    public static IVertexArray Create()
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenTK:
                return new OpenTKVertexArray();
            case ApiType.SilkNet:
                return new SilkNetVertexArray();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}