using Engine.Renderer.Buffers.FrameBuffer;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetFrameBuffer : FrameBuffer
{
    private static readonly ILogger Logger = Log.ForContext<SilkNetFrameBuffer>();

    private static readonly Lazy<uint> MaxFramebufferSize = new(() =>
    {
        // Ensure OpenGL context is initialized before querying capabilities
        if (SilkNetContext.GL == null)
        {
            Logger.Warning("Attempting to query MaxFramebufferSize before OpenGL context initialization. Using fallback value of 8192.");
            return 8192;
        }

        int maxWidth = SilkNetContext.GL.GetInteger(GetPName.MaxFramebufferWidth);
        int maxHeight = SilkNetContext.GL.GetInteger(GetPName.MaxFramebufferHeight);

        // Validate OpenGL query results
        if (maxWidth <= 0 || maxHeight <= 0)
        {
            Logger.Warning("Invalid OpenGL framebuffer size query results (Width: {MaxWidth}, Height: {MaxHeight}). Using fallback value of 8192.",
                maxWidth, maxHeight);
            return 8192;
        }

        uint maxSize = (uint)System.Math.Min(maxWidth, maxHeight);
        Logger.Information("OpenGL Max Framebuffer Size: {MaxSize}x{MaxSize} (Width: {MaxWidth}, Height: {MaxHeight})",
            maxSize, maxSize, maxWidth, maxHeight);
        return maxSize;
    });

    private uint _rendererId = 0;
    private readonly List<FramebufferTextureSpecification> _colorAttachmentSpecs = [];
    private uint[] _colorAttachments;
    private uint _depthAttachment = 0;
    private readonly FramebufferTextureSpecification _depthAttachmentSpec;
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

        if (_depthAttachment != 0)
        {
            SilkNetContext.GL.DeleteTextures(1, _depthAttachment);
        }

        Array.Clear(_colorAttachments, 0, _colorAttachments.Length);
        _depthAttachment = 0;
    }

    public override uint GetColorAttachmentRendererId() => _colorAttachments[0];
    public override FrameBufferSpecification GetSpecification() => _specification;

    public override void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0 ||
            width > MaxFramebufferSize.Value ||
            height > MaxFramebufferSize.Value)
        {
            Logger.Warning("Attempted to resize framebuffer to {Width}x{Height}. Max supported size: {MaxSize}x{MaxSize}",
                width, height, MaxFramebufferSize.Value, MaxFramebufferSize.Value);
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
            Logger.Warning($"Warning: Invalid attachment index {attachmentIndex}, " +
                             $"valid range is 0-{_colorAttachmentSpecs.Count - 1}");
            return -1;
        }

        // Validate coordinates
        if (x < 0 || x >= _specification.Width || y < 0 || y >= _specification.Height)
        {
            Logger.Warning($"Warning: Pixel coordinates ({x}, {y}) out of bounds " +
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
            
            int redValue = 0;
            SilkNetContext.GL.ReadPixels(x, y, 1, 1, GLEnum.RedInteger, PixelType.Int, &redValue);

#if DEBUG
            var error = SilkNetContext.GL.GetError();
            if (error != GLEnum.NoError)
            {
                Logger.Warning("Warning: ReadPixels failed with OpenGL error: {error}", error);
            }
#endif

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
        bool attachmentCountChanged = _colorAttachments == null ||
                                       _colorAttachments.Length != _colorAttachmentSpecs.Count;

        if (_rendererId != 0)
        {
            SilkNetContext.GL.DeleteFramebuffer(_rendererId);
            GLDebug.CheckError(SilkNetContext.GL, "DeleteFramebuffer");
            
            SilkNetContext.GL.DeleteTextures(_colorAttachments);

            // Only delete if we actually have a depth attachment
            if (_depthAttachment != 0)
            {
                SilkNetContext.GL.DeleteTextures(1, _depthAttachment);
                _depthAttachment = 0;
            }
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
                DrawBufferMode[] drawBuffers = new DrawBufferMode[4];
                for (int i = 0; i < 4; i++)
                {
                    drawBuffers[i] = DrawBufferMode.ColorAttachment0 + i;
                }

                SilkNetContext.GL.DrawBuffers((uint)_colorAttachments.Length, drawBuffers);
                GLDebug.CheckError(SilkNetContext.GL, "DrawBuffers");
                break;
            }
            case 0:
                // Only depth-pass
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

        InternalFormat internalFormat = InternalFormat.Rgba8;
        PixelFormat format = PixelFormat.Rgba;
        switch (_colorAttachmentSpecs[attachmentIndex].TextureFormat)
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
    
    private GLEnum TextureFormatToGL(FramebufferTextureFormat format)
    {
        switch (format)
        {
            case FramebufferTextureFormat.RGBA8:       return GLEnum.Rgba8;
            case FramebufferTextureFormat.RED_INTEGER: return GLEnum.RedInteger;
        }

        return 0;
    }
    
    private void AttachDepthTexture(uint id, uint samples, GLEnum format, FramebufferAttachment attachmentType, uint width, uint height)
    {
        bool multisampled = samples > 1;

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
}