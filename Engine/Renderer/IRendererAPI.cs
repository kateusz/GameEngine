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
    void SetViewport(int x, int y, uint width, uint height);
    void EnableFaceCulling(bool enable);
    void SetCullFace(bool cullBack);
    void BindTextureUnit(uint unit, uint textureId);
    void Init();
    int GetError();
}