using Engine.Platform.SilkNet;
using Engine.Renderer;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLShadowMap : IShadowMap
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLShadowMap>();

    private uint _fbo;
    private uint _depthTexture;
    private bool _disposed;

    private uint _previousFbo;

    public uint Width { get; }
    public uint Height { get; }
    public uint DepthTextureId => _depthTexture;

    public OpenGLShadowMap(uint width, uint height)
    {
        Width = width;
        Height = height;

        var gl = SilkNetContext.GL;

        _fbo = gl.GenFramebuffer();
        _depthTexture = gl.GenTexture();

        gl.BindTexture(TextureTarget.Texture2D, _depthTexture);
        unsafe
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent,
                width, height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

        // Set border color to 1.0 so areas outside the shadow map are lit
        float[] borderColor = [1.0f, 1.0f, 1.0f, 1.0f];
        unsafe
        {
            fixed (float* ptr = borderColor)
            {
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, ptr);
            }
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, _depthTexture, 0);
        gl.DrawBuffer(DrawBufferMode.None);
        gl.ReadBuffer(ReadBufferMode.None);

        var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            Logger.Error("Shadow map framebuffer incomplete: {Status}", status);
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        Logger.Debug("Created shadow map {Width}x{Height}", width, height);
    }

    public void Bind()
    {
        var gl = SilkNetContext.GL;

        // Save the currently bound framebuffer so we can restore it
        gl.GetInteger(GetPName.FramebufferBinding, out int currentFbo);
        _previousFbo = (uint)currentFbo;

        gl.Viewport(0, 0, Width, Height);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        gl.Clear(ClearBufferMask.DepthBufferBit);
    }

    public void Unbind()
    {
        // Restore the previously bound framebuffer (e.g. editor's FBO)
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _previousFbo);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var gl = SilkNetContext.GL;
        if (_fbo != 0)
        {
            gl.DeleteFramebuffer(_fbo);
            _fbo = 0;
        }
        if (_depthTexture != 0)
        {
            gl.DeleteTexture(_depthTexture);
            _depthTexture = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
