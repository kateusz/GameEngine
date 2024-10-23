using System.Diagnostics;
using Engine.Renderer.Buffers.FrameBuffer;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetFrameBuffer : FrameBuffer
{
    private const uint MaxFramebufferSize = 8192;
    
    private uint _rendererId = 0;
    private uint[] _colorAttachments;
    private uint _depthAttachment;
    private readonly FrameBufferSpecification _specification;

    public SilkNetFrameBuffer(FrameBufferSpecification spec)
    {
        _specification = spec;
        Invalidate();
    }

    ~SilkNetFrameBuffer()
    {
        SilkNetContext.GL.DeleteFramebuffers(1, _rendererId);
        SilkNetContext.GL.DeleteTextures(_colorAttachments);
        SilkNetContext.GL.DeleteRenderbuffer(_depthAttachment);
    }

    public override uint GetColorAttachmentRendererId() => _colorAttachments[0];
    public override FrameBufferSpecification GetSpecification() => _specification;

    public override void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0 || width > MaxFramebufferSize || height > MaxFramebufferSize)
        {
            Debug.WriteLine("Attempted to resize framebuffer to {0}, {1}", width, height);
            return;
        }
        
        _specification.Width = width;
        _specification.Height = height;

        Invalidate();
    }

    public override void Bind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);

    public override void Unbind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void Invalidate()
    {
        unsafe
        {
            if (_rendererId != 0)
            {
                SilkNetContext.GL.DeleteFramebuffer(_rendererId);
                SilkNetContext.GL.DeleteTextures(_colorAttachments);
                SilkNetContext.GL.DeleteRenderbuffer(_depthAttachment);
            }

            _rendererId = SilkNetContext.GL.GenFramebuffer();
            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);

            _colorAttachments = new uint[3];
            SilkNetContext.GL.GenTextures(3, _colorAttachments);

            for (int i = 0; i < 3; i++)
            {
                SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _colorAttachments[i]);
                
                // Create our texture and upload the image data.
                SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, _specification.Width,
                    _specification.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.Linear);
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int)TextureMagFilter.Linear);
                SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, _colorAttachments[i], 0);
            }
            
            
            DrawBufferMode[] drawBuffers = new DrawBufferMode[3];
            for (int i = 0; i < 3; i++)
            {
                drawBuffers[i] = DrawBufferMode.ColorAttachment0 + i;
            }
            SilkNetContext.GL.DrawBuffers(3, drawBuffers);

            if (SilkNetContext.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer is not complete!");
            }

            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}