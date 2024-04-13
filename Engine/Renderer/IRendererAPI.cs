using Engine.Renderer.VertexArray;
using System.Numerics;

namespace Engine.Renderer;

public interface IRendererAPI
{
    ApiType ApiType { get; }
    void SetClearColor(Vector4 color);
    void Clear();
    void DrawIndexed(IVertexArray vertexArray);
    void Init();
}