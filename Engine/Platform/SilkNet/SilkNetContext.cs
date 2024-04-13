using Engine.Renderer;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetContext : IGraphicsContext
{
    public static GL GL { get; set; }
    
    public void Init(Action swapBuffer)
    {
        throw new NotImplementedException();
    }

    public void SwapBuffers()
    {
        throw new NotImplementedException();
    }
}