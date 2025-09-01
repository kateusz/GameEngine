using System.Numerics;

namespace Engine.Renderer;

public interface IGraphics
{
    void SetClearColor(Vector4 color);
    void Clear();
}