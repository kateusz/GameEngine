using DryIoc;
using Engine.Renderer;

namespace Engine.Core.Modules;

public class RenderingModule : IModule
{
    public int Priority => 1;

    public void Register(IContainer container)
    {
        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
    }
}
