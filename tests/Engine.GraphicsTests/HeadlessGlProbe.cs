using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

internal static class HeadlessGlProbe
{
    private static readonly Lazy<bool> LazyAvailable = new(Probe);

    public static bool IsAvailable => LazyAvailable.Value;

    private static bool Probe()
    {
        try
        {
            var options = WindowOptions.Default;
            options.IsVisible = false;
            options.Title = "GL probe";
            options.Size = new Vector2D<int>(1, 1);

            var window = Window.Create(options);
            window.Initialize();
            var gl = window.CreateOpenGL();
            gl.Dispose();
            window.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
