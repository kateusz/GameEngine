using Engine.Core;
using Engine.Renderer;

namespace Runtime;

public class RuntimeApplication : Application
{
    public RuntimeApplication(IGraphics2D graphics2D, IGraphics3D graphics3D) : base(null!, graphics2D, graphics3D,
        true)
    {
        PushLayer(new Runtime2DLayer());
    }
}