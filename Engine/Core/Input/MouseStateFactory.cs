using Engine.Platform.SilkNet.Input;
using Engine.Renderer;

namespace Engine.Core.Input;

public class MouseStateFactory
{
    public static IMouseState Create()
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SIlkNetMouseState(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}