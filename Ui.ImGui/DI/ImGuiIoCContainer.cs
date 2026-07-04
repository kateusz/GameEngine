using DryIoc;

namespace Ui.ImGui.DI;

public static class ImGuiIoCContainer
{
    public static void Register(Container container)
    {
        container.Register<IImGuiLayerFactory, ImGuiLayerFactory>(Reuse.Singleton);
        container.Register<IImGuiLayer>(
            made: Made.Of(
                r => ServiceInfo.Of<IImGuiLayerFactory>(),
                f => f.Create()
            ),
            reuse: Reuse.Singleton);
        container.RegisterDelegate<Engine.Core.IFrameCompositor>(
            r => r.Resolve<IImGuiLayer>(),
            Reuse.Singleton);
    }
}
