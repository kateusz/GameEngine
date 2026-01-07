using Engine.Platform.SilkNet.Input;
using Engine.Renderer;
using Silk.NET.Input;

namespace Engine.Core.Input;

internal sealed class InputSystemFactory(IRendererApiConfig apiConfig) : IInputSystemFactory
{
    public IInputSystem Create(IInputContext inputContext)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetInputSystem(inputContext),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
