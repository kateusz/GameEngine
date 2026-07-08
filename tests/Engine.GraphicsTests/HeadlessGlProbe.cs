using Engine.Platform.SilkNet;
using Silk.NET.Maths;

namespace Engine.GraphicsTests;

internal static class HeadlessGlProbe
{
    private static readonly Lazy<bool> LazyAvailable = new(Probe);

    public static bool IsAvailable => LazyAvailable.Value;

    private static bool Probe()
    {
        try
        {
            var window = HeadlessWindow.Create("GL probe", new Vector2D<int>(1, 1));
            var context = new SilkNetGraphicsContext();
            context.Create(window);
            context.Dispose();
            window.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
