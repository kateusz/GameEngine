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
    /// <summary>When false, depth buffer is not written (transparent pass).</summary>
    void SetDepthWrite(bool enabled);
    /// <summary>When enabled, back faces are culled. Disable for double-sided materials.</summary>
    void SetFaceCulling(bool enabled);
    void SetPolygonMode(PolygonMode mode);
    void Init();
    int GetError();
}