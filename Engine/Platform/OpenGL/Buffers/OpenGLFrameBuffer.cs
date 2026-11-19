using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers.FrameBuffer;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Buffers;

internal sealed class OpenGLFrameBuffer : FrameBuffer
{
    private const uint MaxFramebufferSize = 8192;

    private uint _rendererId;
    private bool _disposed;
    private readonly int[] _previousViewport = new int[4];
    private int _previousDrawFbo;
    private int _previousReadFbo;
    private bool _bound;
    private readonly List<FrameBufferTextureSpecification> _colorAttachmentSpecs = [];
    private uint[] _colorAttachments = [];
    private uint _depthAttachment;
    private readonly FrameBufferTextureSpecification _depthAttachmentSpec;
    private readonly FrameBufferSpecification _specification;

    public OpenGLFrameBuffer(FrameBufferSpecification spec)
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

    public override uint GetColorAttachmentRendererId(int index = 0)
    {
        if (_colorAttachments == null || index < 0 || index >= _colorAttachments.Length)
        {
            Debug.WriteLine("Warning: Attempted to get color attachment from framebuffer with no color attachments");
            return 0;
        }
        return _colorAttachments[index];
    }

    public override uint GetDepthAttachmentRendererId() => _depthAttachment;

    public override FrameBufferSpecification GetSpecification() => _specification;

    public override void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0 || width > MaxFramebufferSize || height > MaxFramebufferSize)
        {
            Debug.WriteLine("Attempted to resize framebuffer to {0}, {1}", width, height);
            return;
        }

        var previousWidth = _specification.Width;
        var previousHeight = _specification.Height;
        _specification.Width = width;
        _specification.Height = height;
        try { Invalidate(); }
        catch
        {
            _specification.Width = previousWidth;
            _specification.Height = previousHeight;
            throw;
        }
        if (_bound)
            SilkNetContext.GL.Viewport(0, 0, width, height);
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
            OpenGLDebug.CheckError(SilkNetContext.GL, "ReadBuffer");
            
            var redValue = 0;
            SilkNetContext.GL.ReadPixels(x, y, 1, 1, GLEnum.RedInteger, PixelType.Int, &redValue);
            OpenGLDebug.CheckError(SilkNetContext.GL, "ReadPixels");
            
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
        OpenGLDebug.CheckError(SilkNetContext.GL, "ClearBuffer");
    }

    public override void BindDepthCubemapFace(int face)
    {
        if (face is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(face), face, "Cubemap face must be 0..5");
        if (_depthAttachmentSpec.TextureFormat != FrameBufferTextureFormat.DepthCubemap)
            throw new InvalidOperationException("Framebuffer has no depth cubemap");

        SilkNetContext.GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.TextureCubeMapPositiveX + face,
            _depthAttachment,
            0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindDepthCubemapFace");
    }
    
    public override void Bind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_bound)
            throw new InvalidOperationException("Framebuffer is already bound; nested binding of the same target is unsupported.");
        SilkNetContext.GL.GetInteger(GLEnum.Viewport, _previousViewport);
        _previousDrawFbo = SilkNetContext.GL.GetInteger(GLEnum.DrawFramebufferBinding);
        _previousReadFbo = SilkNetContext.GL.GetInteger(GLEnum.ReadFramebufferBinding);
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);
        SilkNetContext.GL.Viewport(0, 0, _specification.Width, _specification.Height);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer");
        _bound = true;
    }

    public override void Unbind()
    {
        if (!_bound)
            throw new InvalidOperationException("Framebuffer is not bound.");
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, (uint)_previousDrawFbo);
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)_previousReadFbo);
        SilkNetContext.GL.Viewport(_previousViewport[0], _previousViewport[1], (uint)_previousViewport[2], (uint)_previousViewport[3]);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer (unbind)");
        _bound = false;
    }

    private void Invalidate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var gl = SilkNetContext.GL;
        var draw = (uint)gl.GetInteger(GLEnum.DrawFramebufferBinding);
        var read = (uint)gl.GetInteger(GLEnum.ReadFramebufferBinding);
        var texture2D = (uint)gl.GetInteger(GLEnum.TextureBinding2D);
        var textureCube = (uint)gl.GetInteger(GLEnum.TextureBindingCubeMap);
        var oldId = _rendererId;
        var oldColors = _colorAttachments;
        var oldDepth = _depthAttachment;
        _rendererId = 0;
        _colorAttachments = [];
        _depthAttachment = 0;
        try
        {
            Allocate();
        }
        catch
        {
            DeleteResources(_rendererId, _colorAttachments, _depthAttachment);
            _rendererId = oldId;
            _colorAttachments = oldColors;
            _depthAttachment = oldDepth;
            throw;
        }
        finally
        {
            // A bound target may itself have been resized; restore its new incarnation.
            gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, draw != 0 && draw == oldId ? _rendererId : draw);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, read != 0 && read == oldId ? _rendererId : read);
            gl.BindTexture(TextureTarget.Texture2D, Replacement(texture2D));
            gl.BindTexture(TextureTarget.TextureCubeMap, Replacement(textureCube));
        }
        DeleteResources(oldId, oldColors, oldDepth);

        uint Replacement(uint texture)
        {
            if (texture == 0) return 0;
            if (texture == oldDepth) return _depthAttachment;
            var index = Array.IndexOf(oldColors, texture);
            return index >= 0 ? _colorAttachments[index] : texture;
        }
    }

    private static void DeleteResources(uint framebuffer, uint[] colors, uint depth)
    {
        var gl = SilkNetContext.GL;
        foreach (var color in colors)
            if (color != 0) gl.DeleteTexture(color);
        if (depth != 0) gl.DeleteTexture(depth);
        if (framebuffer != 0) gl.DeleteFramebuffer(framebuffer);
    }

    private void Allocate()
    {
        _rendererId = SilkNetContext.GL.GenFramebuffer();
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenFramebuffer");
        
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer");

        _colorAttachments = new uint[_colorAttachmentSpecs.Count];

        SilkNetContext.GL.GenTextures(_colorAttachments);
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenTextures");

        for (var i = 0; i < _colorAttachments.Length; i++)
        {
            AttachColorTexture(i);
        }

        if (_depthAttachmentSpec.TextureFormat != FrameBufferTextureFormat.None)
        {
            _depthAttachment = SilkNetContext.GL.GenTexture();
            OpenGLDebug.CheckError(SilkNetContext.GL, "GenTexture (depth)");

            switch (_depthAttachmentSpec.TextureFormat)
            {
                case FrameBufferTextureFormat.DEPTH24STENCIL8:
                    SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _depthAttachment);
                    AttachDepthTexture(_depthAttachment, _specification.Samples, GLEnum.Depth24Stencil8, FramebufferAttachment.DepthStencilAttachment, _specification.Width, _specification.Height);
                    break;
                case FrameBufferTextureFormat.DepthComponent:
                    SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _depthAttachment);
                    AttachShadowDepthTexture(_depthAttachment, _specification.Width, _specification.Height, _depthAttachmentSpec);
                    break;
                case FrameBufferTextureFormat.DepthCubemap:
                    AttachShadowDepthCubemap(_depthAttachment, _specification.Width, _specification.Height);
                    break;
            }
        }
        else
        {
            // Explicitly set to no attachment
            _depthAttachment = 0;
        }

        DrawBuffers();

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
                OpenGLDebug.CheckError(SilkNetContext.GL, "DrawBuffers");
                break;
            }
            default:
                // Only depth-pass (when 0 attachments)
                SilkNetContext.GL.DrawBuffer(GLEnum.None);
                OpenGLDebug.CheckError(SilkNetContext.GL, "DrawBuffer (None)");
                SilkNetContext.GL.ReadBuffer(GLEnum.None);
                OpenGLDebug.CheckError(SilkNetContext.GL, "ReadBuffer (None)");
                break;
        }

        var status = SilkNetContext.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        OpenGLDebug.CheckError(SilkNetContext.GL, "CheckFramebufferStatus");

        if (status != GLEnum.FramebufferComplete)
        {
            throw new InvalidOperationException($"Framebuffer is not complete! Status: {status} (0x{(int)status:X})");
        }
    }

    private unsafe void AttachColorTexture(int attachmentIndex)
    {
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _colorAttachments[attachmentIndex]);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"BindTexture (color attachment {attachmentIndex})");

        InternalFormat internalFormat;
        PixelFormat format;
        PixelType pixelType;
        switch (_colorAttachmentSpecs[attachmentIndex].TextureFormat)
        {
            case FrameBufferTextureFormat.RGBA8:
                internalFormat = InternalFormat.Rgba8;
                format = PixelFormat.Rgba;
                pixelType = PixelType.UnsignedByte;
                break;
            case FrameBufferTextureFormat.RGBA16F:
                internalFormat = InternalFormat.Rgba16f;
                format = PixelFormat.Rgba;
                pixelType = PixelType.Float;
                break;
            case FrameBufferTextureFormat.RED_INTEGER:
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
        OpenGLDebug.CheckError(SilkNetContext.GL, $"TexImage2D (color attachment {attachmentIndex})");
        
        var spec = _colorAttachmentSpecs[attachmentIndex];
        var minFilter = spec.Filter == FrameBufferTextureFilter.Linear
            ? TextureMinFilter.Linear
            : TextureMinFilter.Nearest;
        var magFilter = spec.Filter == FrameBufferTextureFilter.Linear
            ? TextureMagFilter.Linear
            : TextureMagFilter.Nearest;
        var wrap = spec.Wrap == FrameBufferTextureWrap.ClampToEdge
            ? (int)GLEnum.ClampToEdge
            : (int)GLEnum.Repeat;

        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)minFilter);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"TexParameter MinFilter (color attachment {attachmentIndex})");
        
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)magFilter);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"TexParameter MagFilter (color attachment {attachmentIndex})");

        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, wrap);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"TexParameter WrapS (color attachment {attachmentIndex})");
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, wrap);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"TexParameter WrapT (color attachment {attachmentIndex})");
        
        SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0 + attachmentIndex, TextureTarget.Texture2D, _colorAttachments[attachmentIndex], 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"FramebufferTexture2D (color attachment {attachmentIndex})");
    }

    private static bool IsDepthFormat(FrameBufferTextureFormat format)
    {
        return format switch
        {
            FrameBufferTextureFormat.DEPTH24STENCIL8 => true,
            FrameBufferTextureFormat.DepthComponent => true,
            FrameBufferTextureFormat.DepthCubemap => true,
            _ => false
        };
    }

    private static unsafe void AttachShadowDepthCubemap(uint id, uint width, uint height)
    {
        var gl = SilkNetContext.GL;
        gl.BindTexture(TextureTarget.TextureCubeMap, id);
        for (var face = 0; face < 6; face++)
        {
            gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, 0, InternalFormat.DepthComponent,
                width, height, 0, PixelFormat.DepthComponent, PixelType.Float, (void*)0);
        }

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureCompareFunc, (int)GLEnum.Lequal);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.TextureCubeMapPositiveX,
            id,
            0);
        OpenGLDebug.CheckError(gl, "FramebufferTexture2D (point shadow cubemap +X)");
    }

    private static unsafe void AttachShadowDepthTexture(
        uint id, uint width, uint height, FrameBufferTextureSpecification spec)
    {
        SilkNetContext.GL.TexImage2D(
            TextureTarget.Texture2D, 0, InternalFormat.DepthComponent,
            width, height, 0, PixelFormat.DepthComponent, PixelType.Float, (void*)0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "TexImage2D (shadow depth)");

        var filter = spec.Filter == FrameBufferTextureFilter.Linear
            ? (int)GLEnum.Linear
            : (int)GLEnum.Nearest;
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, filter);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, filter);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)GLEnum.Lequal);

        var wrap = spec.Wrap switch
        {
            FrameBufferTextureWrap.ClampToBorder => (int)GLEnum.ClampToBorder,
            FrameBufferTextureWrap.ClampToEdge => (int)GLEnum.ClampToEdge,
            _ => (int)GLEnum.Repeat
        };
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, wrap);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, wrap);
        if (spec.Wrap == FrameBufferTextureWrap.ClampToBorder)
        {
            Span<float> border = [1f, 1f, 1f, 1f];
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, border);
        }

        SilkNetContext.GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, id, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "FramebufferTexture2D (shadow depth)");
    }
    
    private static GLEnum TextureFormatToGL(FrameBufferTextureFormat format)
    {
        switch (format)
        {
            case FrameBufferTextureFormat.RGBA8:       return GLEnum.Rgba8;
            case FrameBufferTextureFormat.RGBA16F:     return GLEnum.Rgba16f;
            case FrameBufferTextureFormat.RED_INTEGER: return GLEnum.RedInteger;
            default:
                throw new NotSupportedException($"Unsupported texture format: {format}");
        }
    }
    private static void AttachDepthTexture(uint id, uint samples, GLEnum format, FramebufferAttachment attachmentType, uint width, uint height)
    {
        var multisampled = samples > 1;

        if (multisampled)
        {
            // Multisampled texture
            SilkNetContext.GL.TexImage2DMultisample(TextureTarget.Texture2DMultisample, samples, format, width, height, false);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexImage2DMultisample (depth)");
        }
        else
        {
            // Regular 2D texture
            SilkNetContext.GL.TexStorage2D(TextureTarget.Texture2D, 1, format, width, height);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexStorage2D (depth)");

            // Set texture parameters
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter MinFilter (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter MagFilter (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter WrapR (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter WrapS (depth)");
            
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter WrapT (depth)");
        }

        // Attach the texture to the framebuffer
        SilkNetContext.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType, multisampled ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D, id, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "FramebufferTexture2D (depth)");
    }
    
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;
        _colorAttachmentSpecs?.Clear();

        if (!disposing)
            return;

        try
        {
            if (_colorAttachments != null && _colorAttachments.Length > 0)
            {
                foreach (var attachment in _colorAttachments)
                {
                    if (attachment != 0)
                        SilkNetContext.GL.DeleteTexture(attachment);
                }
                Array.Clear(_colorAttachments, 0, _colorAttachments.Length);
            }

            if (_depthAttachment != 0)
            {
                SilkNetContext.GL.DeleteTexture(_depthAttachment);
                _depthAttachment = 0;
            }

            if (_rendererId != 0)
            {
                SilkNetContext.GL.DeleteFramebuffer(_rendererId);
                _rendererId = 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Error disposing FrameBuffer {_rendererId}: {ex.Message}"
            );
        }
    }

#if DEBUG
    ~OpenGLFrameBuffer()
    {
        if (_rendererId != 0)
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
