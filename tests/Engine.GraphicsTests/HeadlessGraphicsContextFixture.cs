using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

public sealed class HeadlessGraphicsContextFixture : IDisposable
{
    public IGraphicsContext GraphicsContext { get; }
    public IWindow Window { get; }

    public HeadlessGraphicsContextFixture()
    {
        var options = WindowOptions.Default;
        options.IsVisible = false;
        options.Title = "Engine.GraphicsTests";
        options.Size = new Vector2D<int>(64, 64);

        Window = Silk.NET.Windowing.Window.Create(options);
        Window.Initialize();

        GraphicsContext = new SilkNetGraphicsContext();
        GraphicsContext.Create(Window);
    }

    public void Dispose()
    {
        GraphicsContext.Dispose();
        Window.Dispose();
    }
}
