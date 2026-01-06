using System.Diagnostics;
using Engine.Renderer.Buffers.FrameBuffer;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

internal sealed class SilkNetFrameBuffer : FrameBuffer
{
    private const uint MaxFramebufferSize = 8192;

    private uint _rendererId;
    private readonly List<FramebufferTextureSpecification> _colorAttachmentSpecs = [];
    private uint[] _colorAttachments;
    private uint _depthAttachment;
    private readonly FramebufferTextureSpecification _depthAttachmentSpec;
    private readonly FrameBufferSpecification _specification;
    private bool _disposed;

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

    /// <summary>
    /// Gets the renderer ID of the first color attachment.
    /// </summary>
    /// <returns>The OpenGL texture ID of the first color attachment, or 0 if there are no color attachments (e.g., depth-only framebuffers).</returns>
    public override uint GetColorAttachmentRendererId()
    {
        if (_colorAttachments == null || _colorAttachments.Length == 0)
        {
            Debug.WriteLine("Warning: Attempted to get color attachment from framebuffer with no color attachments");
            return 0;
        }
        return _colorAttachments[0];
    }

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
        // Validate attachment index
        if (attachmentIndex < 0 || attachmentIndex >= _colorAttachmentSpecs.Count)
        {
            Debug.WriteLine($"Warning: Invalid attachment index {attachmentIndex}, " +
                           $"valid range is 0-{_colorAttachmentSpecs.Count - 1}");
            return -1;
        }

        // Validate coordinates
        if (x < 0 || x >= _specification.Width || y < 0 || y >= _specification.Height)
        {
            Debug.WriteLine($"Warning: Pixel coordinates ({x}, {y}) out of bounds " +
                           $"for framebuffer size ({_specification.Width}, {_specification.Height})");
            return -1;
        }

        // Must bind framebuffer before reading
        var previousFBO = SilkNetContext.GL.GetInteger(GLEnum.ReadFramebufferBinding);
        if (previousFBO != (int)_rendererId)
        {
            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _rendererId);
        }

        unsafe
        {
            SilkNetContext.GL.ReadBuffer(GLEnum.ColorAttachment0 + attachmentIndex);
            GLDebug.CheckError(SilkNetContext.GL, "ReadBuffer");
            
            var redValue = 0;
            SilkNetContext.GL.ReadPixels(x, y, 1, 1, GLEnum.RedInteger, PixelType.Int, &redValue);
            GLDebug.CheckError(SilkNetContext.GL, "ReadPixels");
            
            // Restore previous binding
            if (previousFBO != (int)_rendererId)
            {
                SilkNetContext.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)previousFBO);
            }

            return redValue;
        }
    }

    public override void ClearAttachment(int attachmentIndex, int value)
    {
        SilkNetContext.GL.ClearBuffer(BufferKind.Color, attachmentIndex, value);
        GLDebug.CheckError(SilkNetContext.GL, "ClearBuffer");
    }
    
    public override void Bind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);
        GLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer");
    }

    public override void Unbind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer (unbind)");
    }

    private void Invalidate()
    {
        var attachmentCountChanged = _colorAttachments == null ||
                                     _colorAttachments.Length != _colorAttachmentSpecs.Count;

        // Properly dispose existing resources before creating new ones
        if (_rendererId != 0)
        {
            SilkNetContext.GL.DeleteFramebuffer(_rendererId);
            GLDebug.CheckError(SilkNetContext.GL, "DeleteFramebuffer");
            _rendererId = 0;
        }

        if (_colorAttachments != null && _colorAttachments.Length > 0)
        {
            foreach (var attachment in _colorAttachments)
            {
                if (attachment != 0)
                {
                    SilkNetContext.GL.DeleteTexture(attachment);
                }
            }
            GLDebug.CheckError(SilkNetContext.GL, "DeleteTextures (color attachments)");
        }

        if (_depthAttachment != 0)
        {
            SilkNetContext.GL.DeleteTexture(_depthAttachment);
            GLDebug.CheckError(SilkNetContext.GL, "DeleteTexture (depth attachment)");
            _depthAttachment = 0;
        }

        _rendererId = SilkNetContext.GL.GenFramebuffer();
        GLDebug.CheckError(SilkNetContext.GL, "GenFramebuffer");
        
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);
        GLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer");

        // Only allocate if attachment count changed
        if (attachmentCountChanged)
            _colorAttachments = new uint[_colorAttachmentSpecs.Count];

        SilkNetContext.GL.GenTextures(_colorAttachments);
        GLDebug.CheckError(SilkNetContext.GL, "GenTextures");

        for (var i = 0; i < _colorAttachments.Length; i++)
        {
            AttachColorTexture(i);
        }

        if (_depthAttachmentSpec.TextureFormat != FramebufferTextureFormat.None)
        {
            _depthAttachment = SilkNetContext.GL.GenTexture();
            GLDebug.CheckError(SilkNetContext.GL, "GenTexture (depth)");
            
            SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _depthAttachment);

            switch (_depthAttachmentSpec.TextureFormat)
            {
                case FramebufferTextureFormat.DEPTH24STENCIL8:
                    AttachDepthTexture(_depthAttachment, _specification.Samples, GLEnum.Depth24Stencil8, FramebufferAttachment.DepthStencilAttachment, _specification.Width, _specification.Height);
                    break;
            }
        }
        else
        {
            // Explicitly set to no attachment
            _depthAttachment = 0;
        }

        DrawBuffers();

        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer (unbind after invalidate)");
    }

    private void DrawBuffers()
    {
        switch (_colorAttachments.Length)
        {
            // Handle draw buffers
            case > 4:
                throw new InvalidOperationException($"Too many color attachments! Maximum is 4, but {_colorAttachments.Length} were specified.");
            case >= 1:
            {
                var drawBuffers = new DrawBufferMode[4];
                for (var i = 0; i < 4; i++)
                {
                    drawBuffers[i] = DrawBufferMode.ColorAttachment0 + i;
                }

                SilkNetContext.GL.DrawBuffers((uint)_colorAttachments.Length, drawBuffers);
                GLDebug.CheckError(SilkNetContext.GL, "DrawBuffers");
                break;
            }
            default:
                // Only depth-pass (when 0 attachments)
                SilkNetContext.GL.DrawBuffer(GLEnum.None);
                GLDebug.CheckError(SilkNetContext.GL, "DrawBuffer (None)");
                break;
        }

        var status = SilkNetContext.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GLDebug.CheckError(SilkNetContext.GL, "CheckFramebufferStatus");

        if (status != GLEnum.FramebufferComplete)
        {
            throw new InvalidOperationException($"Framebuffer is not complete! Status: {status} (0x{(int)status:X})");
        }
    }

    private unsafe void AttachColorTexture(int attachmentIndex)
    {
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _colorAttachments[attachmentIndex]);
        GLDebug.CheckError(SilkNetContext.GL, $"BindTexture (color attachment {attachmentIndex})");

        var internalFormat = InternalFormat.Rgba8;
        var format = PixelFormat.Rgba;
        PixelType pixelType;
        switch (_colorAttachmentSpecs[attachmentIndex].TextureFormat)
        {
            case FramebufferTextureFormat.RGBA8:
                internalFormat = InternalFormat.Rgba8;
                format = PixelFormat.Rgba;
                pixelType = PixelType.UnsignedByte;
                break;
            case FramebufferTextureFormat.RED_INTEGER:
                internalFormat = InternalFormat.R32i;
                format = PixelFormat.RedInteger;
                pixelType = PixelType.Int;
                break;
            default:
                throw new NotSupportedException(
                    $"Unsupported texture format: {_colorAttachmentSpecs[attachmentIndex].TextureFormat}");
        }

        // Create our texture and upload the image data.
        SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, _specification.Width,
            _specification.Height, 0, format, pixelType, (void*)0);
        GLDebug.CheckError(SilkNetContext.GL, $"TexImage2D (color attachment {attachmentIndex})");
        
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest);
        GLDebug.CheckError(SilkNetContext.GL, $"TexParameter MinFilter (color attachment {attachmentIndex})");
        
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest);
        GLDebug.CheckError(SilkNetContext.GL, $"TexParameter MagFilter (color attachment {attachmentIndex})");
        
        SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0 + attachmentIndex, TextureTarget.Texture2D, _colorAttachments[attachmentIndex], 0);
        GLDebug.CheckError(SilkNetContext.GL, $"FramebufferTexture2D (color attachment {attachmentIndex})");
    }

    private static bool IsDepthFormat(FramebufferTextureFormat format)
    {
        return format switch
        {
            FramebufferTextureFormat.DEPTH24STENCIL8 => true,
            _ => false
        };
    }
    
    private static GLEnum TextureFormatToGL(FramebufferTextureFormat format)
    {
        switch (format)
        {
            case FramebufferTextureFormat.RGBA8:       return GLEnum.Rgba8;
            case FramebufferTextureFormat.RED_INTEGER: return GLEnum.RedInteger;
        }

        return 0;
    }

    private static void AttachDepthTexture(uint id, uint samples, GLEnum format, FramebufferAttachment attachmentType, uint width, uint height)
    {
        var multisampled = samples > 1;

        if (multisampled)
        {
            // Multisampled texture
            SilkNetContext.GL.TexImage2DMultisample(TextureTarget.Texture2DMultisample, samples, format, width, height, false);
            GLDebug.CheckError(SilkNetContext.GL, "TexImage2DMultisample (depth)");
        }
        else
        {
            // Regular 2D texture
            SilkNetContext.GL.TexStorage2D(TextureTarget.Texture2D, 1, format, width, height);
            GLDebug.CheckError(SilkNetContext.GL, "TexStorage2D (depth)");

            // Set texture parameters
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GLDebug.CheckError(SilkNetContext.GL, "TexParameter MinFilter (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GLDebug.CheckError(SilkNetContext.GL, "TexParameter MagFilter (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
            GLDebug.CheckError(SilkNetContext.GL, "TexParameter WrapR (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GLDebug.CheckError(SilkNetContext.GL, "TexParameter WrapS (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            GLDebug.CheckError(SilkNetContext.GL, "TexParameter WrapT (depth)");
        }

        // Attach the texture to the framebuffer
        SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType, multisampled ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D, id, 0);
        GLDebug.CheckError(SilkNetContext.GL, "FramebufferTexture2D (depth)");
    }
    
    public override void Dispose()
    {
        if (_disposed)
            return;

        _colorAttachmentSpecs?.Clear();

        try
        {
            if (_rendererId != 0)
            {
                SilkNetContext.GL.DeleteFramebuffer(_rendererId);
                _rendererId = 0;
            }

            if (_colorAttachments != null && _colorAttachments.Length > 0)
            {
                foreach (var attachment in _colorAttachments)
                {
                    if (attachment != 0)
                    {
                        SilkNetContext.GL.DeleteTexture(attachment);
                    }
                }
                Array.Clear(_colorAttachments, 0, _colorAttachments.Length);
            }

            if (_depthAttachment != 0)
            {
                SilkNetContext.GL.DeleteTexture(_depthAttachment);
                _depthAttachment = 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Error disposing FrameBuffer {_rendererId}: {ex.Message}"
            );
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~SilkNetFrameBuffer()
    {
        if (!_disposed && _rendererId != 0)
        {
            Debug.WriteLine(
                $"GPU LEAK: FrameBuffer {_rendererId} not disposed! " +
                $"Size: {_specification.Width}x{_specification.Height}, " +
                $"Attachments: {_colorAttachments?.Length ?? 0}"
            );
        }
    }
#endif
}