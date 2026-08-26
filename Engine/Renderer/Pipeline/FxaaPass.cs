using Engine.Core;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Serilog;

namespace Engine.Renderer.Pipeline;

public sealed class FxaaPass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory) : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<FxaaPass>();

    private IShader? _shader;
    private IVertexArray? _triangle;
    private bool _initAttempted;
    private bool _disposed;

    public bool Available { get; private set; }

    public void Init()
    {
        if (_initAttempted)
            return;

        _initAttempted = true;
        try
        {
            _shader = shaderFactory.Create(
                PathBuilder.Resolve("assets/shaders/OpenGL/fxaa.vert"),
                PathBuilder.Resolve("assets/shaders/OpenGL/fxaa.frag"));
            _triangle = vertexArrayFactory.Create();
            _shader.Bind();
            _shader.SetInt("u_Texture", 0);
            _shader.Unbind();
            Available = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "FXAA disabled: failed to create shader or triangle");
            Available = false;
            _shader?.Dispose();
            _shader = null;
            _triangle?.Dispose();
            _triangle = null;
        }
    }

    public void Apply(uint sourceTextureId, uint width, uint height, IFrameBuffer? dest)
    {
        Init();
        if (!Available || width == 0 || height == 0 || _shader == null || _triangle == null)
            return;

        if (dest != null)
            dest.Bind();
        else
            rendererApi.BindDefaultFramebuffer();

        rendererApi.SetViewport(0, 0, width, height);
        rendererApi.SetDepthTest(false);
        rendererApi.SetBlend(false);
        rendererApi.SetFaceCulling(false);
        try
        {
            _shader.Bind();
            rendererApi.BindTexture2D(sourceTextureId);
            rendererApi.SetBoundTexture2DFilterLinear();
            _shader.SetFloat("u_InverseWidth", 1f / width);
            _shader.SetFloat("u_InverseHeight", 1f / height);
            rendererApi.DrawArrays(_triangle, 3);
            _shader.Unbind();
        }
        finally
        {
            rendererApi.SetDepthTest(true);
            rendererApi.SetBlend(true);
            rendererApi.SetFaceCulling(true);
            dest?.Unbind();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _shader?.Dispose();
        _shader = null;
        _triangle?.Dispose();
        _triangle = null;
        Available = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
