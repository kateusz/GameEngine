using Engine.Renderer;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetFrameBuffer : FrameBuffer
{
    private uint _rendererId;
    private uint _colorAttachment;
    private uint _depthAttachment;
    private readonly FramebufferSpecification _specification;

    public SilkNetFrameBuffer(FramebufferSpecification spec)
    {
        _specification = spec;
        Invalidate();
    }

    ~SilkNetFrameBuffer()
    {
        SilkNetContext.GL.DeleteFramebuffers(1, _rendererId);
        SilkNetContext.GL.DeleteTexture(_colorAttachment);
        SilkNetContext.GL.DeleteRenderbuffer(_depthAttachment);
    }

    public override uint GetColorAttachmentRendererId() => _colorAttachment;
    public override FramebufferSpecification GetSpecification() => _specification;

    public override void Bind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);
    }

    public override void Unbind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void Invalidate()
    {
        unsafe
        {
            _rendererId = SilkNetContext.GL.GenFramebuffer();
            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);

            _colorAttachment = SilkNetContext.GL.GenTexture();
            SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
            SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _colorAttachment);

            // Create our texture and upload the image data.
            SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, _specification.Width,
                _specification.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);

            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);

            SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorAttachment, 0);

            // Generate handle
            _depthAttachment = SilkNetContext.GL.GenRenderbuffer();

            SilkNetContext.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthAttachment);
            SilkNetContext.GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8,
                _specification.Width, _specification.Height);
            SilkNetContext.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _depthAttachment);

            // Check framebuffer completeness
            var status = SilkNetContext.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Framebuffer is not complete: {status}");
            }

            // Unbind framebuffer
            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}