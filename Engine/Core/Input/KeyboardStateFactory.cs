using Engine.Platform.SilkNet.Input;
using Engine.Renderer;

namespace Engine.Core.Input;

public class KeyboardStateFactory
{
    public static IKeyboardState Create()
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetKeyboardState(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}