using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.Platform.SilkNet;

public class SilkNetContext
{
    public static GL GL { get; set; }
    public static IWindow Window { get; set; }
    public static IInputContext InputContext { get; set; }
}