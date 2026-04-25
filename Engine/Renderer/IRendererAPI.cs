using Engine.Renderer.VertexArray;
using System.Numerics;

namespace Engine.Renderer;

public interface IRendererAPI
{
    void SetClearColor(Vector4 color);
    void Clear();
    void ClearDepth();
    void BindTexture2D(uint textureId, int slot = 0);
    void DrawIndexed(IVertexArray vertexArray, uint count);
    void DrawLines(IVertexArray vertexArray, uint vertexCount);
    void SetLineWidth(float width);
    void SetDepthTest(bool enabled);
    void SetCullFace(CullMode mode);
    void Init();
    int GetError();
}