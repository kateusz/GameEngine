using Silk.NET.Windowing;

namespace Engine.Renderer;

public interface IGraphicsContext : IDisposable
{
    bool IsCreated { get; }
    void Create(IWindow window);
}
