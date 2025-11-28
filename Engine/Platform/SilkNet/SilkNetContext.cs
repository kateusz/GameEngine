using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.Platform.SilkNet;

internal static class SilkNetContext
{
    public static GL GL { get; set; }
    public static IWindow Window { get; set; }
}