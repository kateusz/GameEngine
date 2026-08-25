using Engine.Renderer.Buffers.VertexArray;
using System.Numerics;

namespace Engine.Renderer;

public interface IRendererAPI
{
    void SetClearColor(Vector4 color);
    void Clear();
    void BindTexture2D(uint textureId, int slot = 0);
    void BindTextureCube(uint textureId, int slot = 0);
    void DrawIndexed(IVertexArray vertexArray, uint count);
    void DrawArrays(IVertexArray vertexArray, uint vertexCount);
    void DrawLines(IVertexArray vertexArray, uint vertexCount);
    void SetLineWidth(float width);
    void SetDepthTest(bool enabled);
    void SetBlend(bool enabled);
    /// <summary>When false, depth buffer is not written (transparent pass).</summary>
    void SetDepthWrite(bool enabled);
    /// <summary>When enabled, culls back faces by default, or front faces when <paramref name="cullFrontFaces"/> is true.</summary>
    void SetFaceCulling(bool enabled, bool cullFrontFaces = false);
    void SetPolygonMode(PolygonMode mode);
    void SetViewport(int x, int y, uint width, uint height);
    void Init();
    int GetError();
}