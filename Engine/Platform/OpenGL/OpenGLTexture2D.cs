using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Textures;
using Pfim;
using Silk.NET.OpenGL;
using StbImageSharp;
using Buffer = System.Buffer;
using InternalFormat = Silk.NET.OpenGL.InternalFormat;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;
using PixelType = Silk.NET.OpenGL.PixelType;
using TextureMagFilter = Silk.NET.OpenGL.TextureMagFilter;
using TextureMinFilter = Silk.NET.OpenGL.TextureMinFilter;
using TextureParameterName = Silk.NET.OpenGL.TextureParameterName;
using TextureTarget = Silk.NET.OpenGL.TextureTarget;
using TextureUnit = Silk.NET.OpenGL.TextureUnit;
using TextureWrapMode = Silk.NET.OpenGL.TextureWrapMode;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLTexture2D : Texture2D
{
    // StbImageSharp flag to flip texture vertically during loading
    // OpenGL expects texture coordinates with origin at bottom-left, but most image formats have origin at top-left
    private const int StbiFlipVerticallyEnabled = 1;

    private uint _rendererId;
    private readonly int _hashCode;
    private readonly InternalFormat _internalFormat;
    private readonly PixelFormat _dataFormat;
    private bool _disposed;

    private OpenGLTexture2D(uint rendererId, int width, int height, InternalFormat internalFormat,
        PixelFormat dataFormat)
    {
        _rendererId = rendererId;
        _hashCode = rendererId.GetHashCode();
        _internalFormat = internalFormat;
        _dataFormat = dataFormat;

        Width = width;
        Height = height;

        Path = string.Empty;
    }

    private OpenGLTexture2D(string path, uint rendererId, int width, int height, InternalFormat internalFormat,
        PixelFormat dataFormat) : this(rendererId, width, height, internalFormat, dataFormat)
    {
        Path = path;
    }

    public override uint GetRendererId()
    {
        return _rendererId;
    }

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".hdr", ".psd", ".pic", ".pnm", ".pgm", ".ppm",
        ".dds"
    };

    private static readonly HashSet<string> PfimExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dds", ".tga"
    };

    public static bool IsSupportedFormat(string path)
    {
        var ext = System.IO.Path.GetExtension(path);
        return SupportedExtensions.Contains(ext);
    }

    public static Texture2D Create(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}", path);

        var ext = System.IO.Path.GetExtension(path);
        if (PfimExtensions.Contains(ext))
            return CreateFromPfim(path);

        return CreateFromStb(path);
    }

    private static Texture2D CreateFromStb(string path)
    {
        StbImage.stbi_set_flip_vertically_on_load(StbiFlipVerticallyEnabled);

        ImageResult image;
        using (var stream = File.OpenRead(path))
        {
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }

        return UploadTexture(path, image.Data, image.Width, image.Height, InternalFormat.Rgba8, PixelFormat.Rgba);
    }

    private static Texture2D CreateFromPfim(string path)
    {
        using var pfimImage = Pfimage.FromFile(path);
        if (pfimImage.Compressed)
            pfimImage.Decompress();

        var (internalFormat, dataFormat) = pfimImage.Format switch
        {
            Pfim.ImageFormat.Rgba32 => (InternalFormat.Rgba8, PixelFormat.Bgra),
            Pfim.ImageFormat.Rgb24 => (InternalFormat.Rgb8, PixelFormat.Bgr),
            Pfim.ImageFormat.R5g5b5 => (InternalFormat.Rgb5, PixelFormat.Bgr),
            Pfim.ImageFormat.R5g6b5 => (InternalFormat.Rgb565, PixelFormat.Bgr),
            Pfim.ImageFormat.R5g5b5a1 => (InternalFormat.Rgb5A1, PixelFormat.Bgra),
            Pfim.ImageFormat.Rgba16 => (InternalFormat.Rgba4, PixelFormat.Bgra),
            _ => throw new NotSupportedException($"Unsupported Pfim format '{pfimImage.Format}' for texture: {path}")
        };

        // Pfim data may have stride padding - copy tightly packed rows if needed
        var bytesPerPixel = pfimImage.BitsPerPixel / 8;
        var tightStride = pfimImage.Width * bytesPerPixel;
        byte[] data;

        if (pfimImage.Stride != tightStride)
        {
            data = new byte[tightStride * pfimImage.Height];
            for (var row = 0; row < pfimImage.Height; row++)
            {
                Buffer.BlockCopy(pfimImage.Data, row * pfimImage.Stride, data, row * tightStride, tightStride);
            }
        }
        else
        {
            data = pfimImage.Data;
        }

        // Flip vertically to match OpenGL's bottom-left origin
        FlipVertically(data, pfimImage.Width, pfimImage.Height, tightStride);

        return UploadTexture(path, data, pfimImage.Width, pfimImage.Height, internalFormat, dataFormat);
    }

    private static void FlipVertically(byte[] data, int width, int height, int stride)
    {
        var tempRow = new byte[stride];
        for (var y = 0; y < height / 2; y++)
        {
            var topOffset = y * stride;
            var bottomOffset = (height - 1 - y) * stride;
            Buffer.BlockCopy(data, topOffset, tempRow, 0, stride);
            Buffer.BlockCopy(data, bottomOffset, data, topOffset, stride);
            Buffer.BlockCopy(tempRow, 0, data, bottomOffset, stride);
        }
    }

    private static Texture2D UploadTexture(string path, byte[] data, int width, int height,
        InternalFormat internalFormat, PixelFormat dataFormat)
    {
        var handle = SilkNetContext.GL.GenTexture();
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenTexture");

        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "ActiveTexture(Texture0)");
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, handle);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture(Texture2D)");

        unsafe
        {
            fixed (byte* ptr = data)
            {
                SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width,
                    (uint)height, 0, dataFormat, PixelType.UnsignedByte, ptr);
                OpenGLDebug.CheckError(SilkNetContext.GL, "TexImage2D");
            }

            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(filters)");

            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.Repeat);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.Repeat);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(wrap modes)");

            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
            OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(mipmap levels)");

            SilkNetContext.GL.GenerateMipmap(TextureTarget.Texture2D);
            OpenGLDebug.CheckError(SilkNetContext.GL, "GenerateMipmap");
        }

        return new OpenGLTexture2D(path, handle, width, height, internalFormat,
            dataFormat == PixelFormat.Bgra ? PixelFormat.Rgba : dataFormat);
    }

    // Activate texture
    // Multiple textures can be bound, if your shader needs more than just one.
    // If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.
    // The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    // Original ver: public void Use(TextureUnit unit)
    public override void Bind(int slot = 0)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0 + slot);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"ActiveTexture({slot})");
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture");
    }

    public override void Unbind()
    {
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UnbindTexture");
    }

    public override void SetData(uint data, int size)
    {
        var intPtrValue = IntPtr.Size switch
        {
            4 => new IntPtr((int)data),
            8 => new IntPtr((long)data),
            _ => throw new NotSupportedException("Unsupported platform.")
        };

        var bpp = _dataFormat == PixelFormat.Rgba ? 4 : 3;

        if (size != Width * Height * bpp)
        {
            throw new Exception("Data must be entire texture!");
        }

        // todo: texture1?
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "ActiveTexture(Texture0) in SetData");
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture in SetData");
        SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)_internalFormat, (uint)Width, (uint)Height, 0,
            _dataFormat, PixelType.UnsignedByte, intPtrValue);
        OpenGLDebug.CheckError(SilkNetContext.GL, "TexImage2D in SetData");
    }

    public static Texture2D Create(int width, int height)
    {
        var internalFormat = InternalFormat.Rgba8;
        var dataFormat = PixelFormat.Rgba;

        var textures = new uint[1];
        SilkNetContext.GL.GenTextures(1, textures);
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenTextures");
        var rendererId = textures[0];

        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "ActiveTexture(Texture0) in Create");
        SilkNetContext.GL.BindTexture(GLEnum.Texture2D, rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture in Create");

        SilkNetContext.GL.TexImage2D(GLEnum.Texture2D, 0, internalFormat, (uint)width, (uint)height, 0, dataFormat,
            GLEnum.UnsignedByte, IntPtr.Zero);
        OpenGLDebug.CheckError(SilkNetContext.GL, "TexImage2D in Create");

        SilkNetContext.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
        SilkNetContext.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(filters) in Create");
        SilkNetContext.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
        SilkNetContext.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
        OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(wrap modes) in Create");

        return new OpenGLTexture2D(rendererId, width, height, internalFormat, dataFormat);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not OpenGLTexture2D other)
            return false;
        
        return _rendererId == other.GetRendererId();
    }

    public override int GetHashCode()
    {
        // Use a hash code derived from the unique renderer ID, which represents this texture in OpenGL.
        // Cached at construction to avoid issues with mutability when _rendererId is set to 0 during disposal.
        return _hashCode;
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (_rendererId != 0)
            {
                SilkNetContext.GL.DeleteTexture(_rendererId);
                _rendererId = 0;
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete OpenGL texture {_rendererId}: {e.Message}");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLTexture2D()
    {
        if (!_disposed && _rendererId != 0)
        {
            Debug.WriteLine(
                $"GPU LEAK: Texture {_rendererId} (path: '{Path}') not disposed!"
            );
        }
    }
#endif
}