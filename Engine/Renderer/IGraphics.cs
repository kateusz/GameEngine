using System.Numerics;

namespace Engine.Renderer;

public interface IGraphics : IDisposable
{
    void SetClearColor(Vector4 color);
    void Clear();
}