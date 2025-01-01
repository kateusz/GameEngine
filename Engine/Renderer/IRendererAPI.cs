using Engine.Renderer.VertexArray;
using System.Numerics;

namespace Engine.Renderer;

public interface IRendererAPI
{
    void SetClearColor(Vector4 color);
    void Clear();
    void DrawIndexed(IVertexArray vertexArray, uint count);
    void DrawLines(IVertexArray vertexArray, uint vertexCount);
    void SetLineWidth(float width);
    void Init();
    int GetError();
}