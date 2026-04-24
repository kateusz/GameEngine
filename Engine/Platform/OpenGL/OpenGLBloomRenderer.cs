using System.Numerics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Shaders;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLBloomRenderer(
    IFrameBufferFactory frameBufferFactory,
    IShaderFactory shaderFactory,
    IRendererAPI rendererApi) : IBloomRenderer
{
    private const uint MinFramebufferSize = 2;
    private const int FullscreenQuadVertexCount = 6;

    private readonly (uint Vao, uint Vbo) _fullscreenQuad = CreateFullscreenQuad();
    private readonly IShader _extractShader = shaderFactory.Create(
        "assets/shaders/OpenGL/fullscreenQuad.vert",
        "assets/shaders/OpenGL/bloomExtract.frag");
    private readonly IShader _blurShader = shaderFactory.Create(
        "assets/shaders/OpenGL/fullscreenQuad.vert",
        "assets/shaders/OpenGL/bloomBlur.frag");
    private readonly IShader _compositeShader = shaderFactory.Create(
        "assets/shaders/OpenGL/fullscreenQuad.vert",
        "assets/shaders/OpenGL/bloomComposite.frag");

    private IFrameBuffer? _extractFrameBuffer;
    private IFrameBuffer? _pingFrameBuffer;
    private IFrameBuffer? _pongFrameBuffer;
    private IFrameBuffer? _compositeFrameBuffer;
    private uint _width;
    private uint _height;
    private int _downsampleFactor = BloomSettings.Default.DownsampleFactor;
    private bool _disposed;

    public void Resize(uint width, uint height)
    {
        var clampedWidth = Math.Max(width, MinFramebufferSize);
        var clampedHeight = Math.Max(height, MinFramebufferSize);
        if (_width == clampedWidth && _height == clampedHeight)
            return;

        _width = clampedWidth;
        _height = clampedHeight;
        RecreateFramebuffers();
    }

    public uint Apply(uint sourceColorTextureId, in BloomSettings settings)
    {
        if (sourceColorTextureId == 0)
            return 0;
        if (!settings.Enabled)
            return sourceColorTextureId;

        var nextDownsampleFactor = Math.Clamp(settings.DownsampleFactor, 1, 4);
        if (nextDownsampleFactor != _downsampleFactor)
        {
            _downsampleFactor = nextDownsampleFactor;
            RecreateFramebuffers();
        }

        EnsureInitializedFramebuffers();
        rendererApi.SetDepthTest(false);
        try
        {
            RenderExtractPass(sourceColorTextureId, settings);
            var blurredTexture = RenderBlurPasses(settings.BlurPasses);
            RenderCompositePass(sourceColorTextureId, blurredTexture, settings);
            return _compositeFrameBuffer!.GetColorAttachmentRendererId();
        }
        finally
        {
            rendererApi.SetDepthTest(true);
        }
    }

    private void EnsureInitializedFramebuffers()
    {
        if (_extractFrameBuffer is not null &&
            _pingFrameBuffer is not null &&
            _pongFrameBuffer is not null &&
            _compositeFrameBuffer is not null)
        {
            return;
        }

        if (_width == 0 || _height == 0)
        {
            _width = MinFramebufferSize;
            _height = MinFramebufferSize;
        }

        RecreateFramebuffers();
    }

    private void RecreateFramebuffers()
    {
        _extractFrameBuffer?.Dispose();
        _pingFrameBuffer?.Dispose();
        _pongFrameBuffer?.Dispose();
        _compositeFrameBuffer?.Dispose();

        var fullResolutionSpec = BuildColorSpec(_width, _height, FramebufferTextureFormat.RGBA16F);
        var blurWidth = Math.Max(_width / (uint)_downsampleFactor, MinFramebufferSize);
        var blurHeight = Math.Max(_height / (uint)_downsampleFactor, MinFramebufferSize);
        var blurSpec = BuildColorSpec(blurWidth, blurHeight, FramebufferTextureFormat.RGBA16F);

        _extractFrameBuffer = frameBufferFactory.Create(blurSpec);
        _pingFrameBuffer = frameBufferFactory.Create(blurSpec);
        _pongFrameBuffer = frameBufferFactory.Create(blurSpec);
        _compositeFrameBuffer = frameBufferFactory.Create(fullResolutionSpec);
    }

    private static FrameBufferSpecification BuildColorSpec(
        uint width,
        uint height,
        FramebufferTextureFormat colorTextureFormat)
    {
        return new FrameBufferSpecification(width, height)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(colorTextureFormat)
            ])
        };
    }

    private void RenderExtractPass(uint sourceColorTextureId, in BloomSettings settings)
    {
        _extractFrameBuffer!.Bind();
        rendererApi.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        rendererApi.Clear();

        _extractShader.Bind();
        _extractShader.SetInt("u_SceneTexture", 0);
        _extractShader.SetFloat("u_Threshold", settings.Threshold);
        _extractShader.SetFloat("u_SoftKnee", settings.SoftKnee);
        BindTextureToSlot(sourceColorTextureId, 0);
        DrawFullscreenQuad();

        _extractFrameBuffer.Unbind();
    }

    private uint RenderBlurPasses(int blurPasses)
    {
        var iterations = Math.Max(1, blurPasses);
        uint inputTexture = _extractFrameBuffer!.GetColorAttachmentRendererId();

        for (var i = 0; i < iterations; i++)
        {
            _pingFrameBuffer!.Bind();
            rendererApi.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
            rendererApi.Clear();
            _blurShader.Bind();
            _blurShader.SetInt("u_Source", 0);
            _blurShader.SetInt("u_Horizontal", 1);
            BindTextureToSlot(inputTexture, 0);
            DrawFullscreenQuad();
            _pingFrameBuffer.Unbind();

            _pongFrameBuffer!.Bind();
            rendererApi.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
            rendererApi.Clear();
            _blurShader.Bind();
            _blurShader.SetInt("u_Source", 0);
            _blurShader.SetInt("u_Horizontal", 0);
            BindTextureToSlot(_pingFrameBuffer.GetColorAttachmentRendererId(), 0);
            DrawFullscreenQuad();
            _pongFrameBuffer.Unbind();

            inputTexture = _pongFrameBuffer.GetColorAttachmentRendererId();
        }

        return inputTexture;
    }

    private void RenderCompositePass(uint sourceColorTextureId, uint bloomTextureId, in BloomSettings settings)
    {
        _compositeFrameBuffer!.Bind();
        rendererApi.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        rendererApi.Clear();

        _compositeShader.Bind();
        _compositeShader.SetInt("u_SceneTexture", 0);
        _compositeShader.SetInt("u_BloomTexture", 1);
        _compositeShader.SetFloat("u_BloomIntensity", settings.Intensity);
        _compositeShader.SetFloat("u_Exposure", settings.Exposure);
        _compositeShader.SetFloat("u_Gamma", settings.Gamma);
        BindTextureToSlot(sourceColorTextureId, 0);
        BindTextureToSlot(bloomTextureId, 1);
        DrawFullscreenQuad();

        _compositeFrameBuffer.Unbind();
    }

    private void DrawFullscreenQuad()
    {
        SilkNetContext.GL.BindVertexArray(_fullscreenQuad.Vao);
        SilkNetContext.GL.DrawArrays(PrimitiveType.Triangles, 0, FullscreenQuadVertexCount);
        SilkNetContext.GL.BindVertexArray(0);
    }

    private static void BindTextureToSlot(uint textureId, int slot)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0 + slot);
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, textureId);
    }

    private static unsafe (uint Vao, uint Vbo) CreateFullscreenQuad()
    {
        var vertices = new float[]
        {
            -1f, -1f, 0f, 0f,
             1f, -1f, 1f, 0f,
             1f,  1f, 1f, 1f,
            -1f, -1f, 0f, 0f,
             1f,  1f, 1f, 1f,
            -1f,  1f, 0f, 1f
        };

        var vao = SilkNetContext.GL.GenVertexArray();
        var vbo = SilkNetContext.GL.GenBuffer();

        SilkNetContext.GL.BindVertexArray(vao);
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        fixed (float* ptr = vertices)
        {
            SilkNetContext.GL.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Length * sizeof(float)),
                ptr,
                BufferUsageARB.StaticDraw);
        }

        SilkNetContext.GL.EnableVertexAttribArray(0);
        SilkNetContext.GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        SilkNetContext.GL.EnableVertexAttribArray(1);
        SilkNetContext.GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        SilkNetContext.GL.BindVertexArray(0);

        return (vao, vbo);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _extractFrameBuffer?.Dispose();
        _pingFrameBuffer?.Dispose();
        _pongFrameBuffer?.Dispose();
        _compositeFrameBuffer?.Dispose();
        _extractShader.Dispose();
        _blurShader.Dispose();
        _compositeShader.Dispose();
        SilkNetContext.GL.DeleteBuffer(_fullscreenQuad.Vbo);
        SilkNetContext.GL.DeleteVertexArray(_fullscreenQuad.Vao);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
