using System.Diagnostics;
using Engine.Renderer.Buffers.FrameBuffer;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetFrameBuffer : FrameBuffer
{
    private const uint MaxFramebufferSize = 8192;

    private uint _rendererId = 0;
    private List<FramebufferTextureSpecification> _colorAttachmentSpecs = new List<FramebufferTextureSpecification>();
    private uint[] _colorAttachments;
    private uint _depthAttachment;
    private FramebufferTextureSpecification _depthAttachmentSpec;
    private readonly FrameBufferSpecification _specification;

    public SilkNetFrameBuffer(FrameBufferSpecification spec)
    {
        _specification = spec;

        foreach (var specificationAttachment in _specification.AttachmentsSpec.Attachments)
        {
            if (!IsDepthFormat(specificationAttachment.TextureFormat))
                _colorAttachmentSpecs.Add(specificationAttachment);
            else
                _depthAttachmentSpec = specificationAttachment;
        }

        Invalidate();
    }

    ~SilkNetFrameBuffer()
    {
        SilkNetContext.GL.DeleteFramebuffers(1, _rendererId);
        SilkNetContext.GL.DeleteTextures(_colorAttachments);
        SilkNetContext.GL.DeleteTextures(1, _depthAttachment);

        Array.Clear(_colorAttachments, 0, _colorAttachments.Length);
        _depthAttachment = 0;
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

    public override int ReadPixel(int attachmentIndex, int x, int y)
    {
        unsafe
        {
            SilkNetContext.GL.ReadBuffer(GLEnum.ColorAttachment0 + attachmentIndex);
            int redValue = 0;
            SilkNetContext.GL.ReadPixels(x, y, 1, 1, GLEnum.RedInteger, PixelType.Int, &redValue);
            return redValue;
        }
    }

    public override void ClearAttachment(int attachmentIndex, int value)
    {
        unsafe
        {
            var spec = _colorAttachmentSpecs[attachmentIndex];
            SilkNetContext.GL.ClearBuffer (BufferKind.Color,attachmentIndex, value);
        }
    }
    
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
            if (_rendererId != 0)
            {
                SilkNetContext.GL.DeleteFramebuffer(_rendererId);
                SilkNetContext.GL.DeleteTextures(_colorAttachments);
                SilkNetContext.GL.DeleteTextures(1, _depthAttachment);
            }

            _rendererId = SilkNetContext.GL.GenFramebuffer();
            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);

            _colorAttachments = new uint[_colorAttachmentSpecs.Count];
            SilkNetContext.GL.GenTextures(_colorAttachments);

            for (int i = 0; i < _colorAttachments.Length; i++)
            {
                SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _colorAttachments[i]);

                InternalFormat internalFormat = InternalFormat.Rgba8;
                PixelFormat format = PixelFormat.Rgba;
                switch (_colorAttachmentSpecs[i].TextureFormat)
                {
                    case FramebufferTextureFormat.RGBA8:
                        internalFormat = InternalFormat.Rgba8;
                        format = PixelFormat.Rgba;
                        break;
                    case FramebufferTextureFormat.RED_INTEGER:
                        internalFormat = InternalFormat.R32i;
                        format = PixelFormat.RedInteger;
                        break;
                }
                
                // Create our texture and upload the image data.
                SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, _specification.Width,
                    _specification.Height, 0, format, PixelType.Int, (void*)0);
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.Nearest);
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int)TextureMagFilter.Nearest);
                SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, _colorAttachments[i], 0);
            }
            
            if (_depthAttachmentSpec.TextureFormat != FramebufferTextureFormat.None)
            {
                _depthAttachment = SilkNetContext.GL.GenTexture();
                SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _depthAttachment);
                
                switch (_depthAttachmentSpec.TextureFormat)
                {
                    case FramebufferTextureFormat.DEPTH24STENCIL8:
                        AttachDepthTexture(_depthAttachment, _specification.Samples, GLEnum.Depth24Stencil8, FramebufferAttachment.DepthStencilAttachment, _specification.Width, _specification.Height);
                        break;
                }
            }

            // Handle draw buffers
            if (_colorAttachments.Length >= 1)
            {
                if (_colorAttachments.Length > 4)
                {
                    throw new Exception("Too many color attachments!");
                }
                
                DrawBufferMode[] drawBuffers = new DrawBufferMode[4];
                for (int i = 0; i < 4; i++)
                {
                    drawBuffers[i] = DrawBufferMode.ColorAttachment0 + i;
                }

                SilkNetContext.GL.DrawBuffers((uint)_colorAttachments.Length, drawBuffers);
            }
            else if (_colorAttachments.Length == 0)
            {
                // Only depth-pass
                SilkNetContext.GL.DrawBuffer(GLEnum.None);
            }

            if (SilkNetContext.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer is not complete!");
            }

            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }

    public bool IsDepthFormat(FramebufferTextureFormat format)
    {
        switch (format)
        {
            case FramebufferTextureFormat.DEPTH24STENCIL8:
                return true;
            default:
                return false;
        }
    }
    
    public GLEnum HazelFBTextureFormatToGL(FramebufferTextureFormat format)
    {
        switch (format)
        {
            case FramebufferTextureFormat.RGBA8:       return GLEnum.Rgba8;
            case FramebufferTextureFormat.RED_INTEGER: return GLEnum.RedInteger;
        }

        return 0;
    }
    
    public void AttachDepthTexture(uint id, uint samples, GLEnum format, FramebufferAttachment attachmentType, uint width, uint height)
    {
        bool multisampled = samples > 1;

        if (multisampled)
        {
            // Multisampled texture
            SilkNetContext.GL.TexImage2DMultisample(TextureTarget.Texture2DMultisample, samples, format, width, height, false);
        }
        else
        {
            // Regular 2D texture
            SilkNetContext.GL.TexStorage2D(TextureTarget.Texture2D, 1, format, width, height);

            // Set texture parameters
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        }

        // Attach the texture to the framebuffer
        SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType, multisampled ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D, id, 0);
    }
}