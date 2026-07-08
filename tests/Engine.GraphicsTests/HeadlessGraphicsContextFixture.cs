using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

public sealed class HeadlessGraphicsContextFixture : IDisposable
{
    public IGraphicsContext GraphicsContext { get; }
    public IWindow Window { get; }

    public HeadlessGraphicsContextFixture()
    {
        Window = HeadlessWindow.Create("Engine.GraphicsTests");

        GraphicsContext = new SilkNetGraphicsContext();
        GraphicsContext.Create(Window);
    }

    public void Dispose()
    {
        GraphicsContext.Dispose();
        Window.Dispose();
    }
}
