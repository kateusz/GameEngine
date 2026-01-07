using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Textures;
using Silk.NET.OpenGL;
using StbImageSharp;
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

    public static Texture2D Create(string path)
    {
        var handle = SilkNetContext.GL.GenTexture();
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenTexture");

        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "ActiveTexture(Texture0)");
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, handle);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture(Texture2D)");

        // Flip texture vertically to match OpenGL's coordinate system (bottom-left origin)
        StbImage.stbi_set_flip_vertically_on_load(StbiFlipVerticallyEnabled);

        var width = 0;
        var height = 0;
        const InternalFormat internalFormat = InternalFormat.Rgba8;
        const PixelFormat dataFormat = PixelFormat.Rgba;

        // Here we open a stream to the file and pass it to StbImageSharp to load.
        using (var stream = File.OpenRead(path))
        {
            unsafe
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                width = image.Width;
                height = image.Height;

                fixed (byte* ptr = image.Data)
                {
                    // Create our texture and upload the image data.
                    SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width,
                        (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
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
        }

        return new OpenGLTexture2D(path, handle, width, height, internalFormat, dataFormat);
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