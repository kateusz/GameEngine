using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;
using System.Numerics;

namespace Engine.Renderer;

public interface IRendererAPI
{
    void SetClearColor(Vector4 color);
    void Clear();
    void BindTexture2D(uint textureId, int slot = 0);
    void BindTextureCube(uint textureId, int slot = 0);
    bool TryCreateSkyCapture(string absolutePath, out uint cubemapId, out ISkyCapture capture);
    void DeleteTexture(uint textureId);
    void BindDefaultFramebuffer();
    void SetBoundTexture2DFilterLinear();
    void DrawIndexed(IVertexArray vertexArray, uint count);

    /// <summary>
    /// Draws <paramref name="vertexArray"/> once for each element of <paramref name="instances"/>.
    /// The vertex array must already be bound. Instance attributes are written onto that binding.
    /// </summary>
    void DrawIndexedInstanced(IVertexArray vertexArray, uint indexCount, ReadOnlySpan<MeshInstanceData> instances);
    void DrawArrays(IVertexArray vertexArray, uint vertexCount);
    void DrawLines(IVertexArray vertexArray, uint vertexCount);
    void SetLineWidth(float width);
    void SetDepthTest(bool enabled);
    void SetBlend(bool enabled);
    /// <summary>When false, depth buffer is not written (transparent pass).</summary>
    void SetDepthWrite(bool enabled);
    /// <summary>When enabled, back faces are culled. Disable for double-sided materials.</summary>
    void SetFaceCulling(bool enabled);
    void SetCullFrontFaces(bool cullFront);
    void SetPolygonMode(PolygonMode mode);
    void SetViewport(int x, int y, uint width, uint height);
    void Init();
    int GetError();
}