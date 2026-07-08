using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

internal static class HeadlessWindow
{
    public static IWindow Create(string title, Vector2D<int>? size = null)
    {
        var options = WindowOptions.Default;
        options.IsVisible = false;
        options.Title = title;
        options.Size = size ?? new Vector2D<int>(64, 64);
        options.ShouldSwapAutomatically = false;

        var window = Silk.NET.Windowing.Window.Create(options);
        window.Initialize();
        return window;
    }
}
