using System.Numerics;

namespace Engine.Renderer.Pipeline;

public interface IGraphics : IDisposable
{
    void Init();
    void SetClearColor(Vector4 color);
    void Clear();
}