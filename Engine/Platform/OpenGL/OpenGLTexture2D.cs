using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Textures;
using Silk.NET.OpenGL;
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

    public static Texture2D Create(string path, bool sRgb = false)
    {
        var decoded = TextureFileDecoder.Decode(path, sRgb);
        return UploadTexture(path, decoded.Data, decoded.Width, decoded.Height, decoded.InternalFormat,
            decoded.DataFormat);
    }

    public static Texture2D CreateFromRgba(byte[] rgba, int width, int height)
    {
        return UploadTexture(string.Empty, rgba, width, height, InternalFormat.Rgba8, PixelFormat.Rgba,
            generateMipmaps: false);
    }

    private static Texture2D UploadTexture(string path, byte[] data, int width, int height,
        InternalFormat internalFormat, PixelFormat dataFormat, bool generateMipmaps = true)
    {
        var handle = SilkNetContext.GL.GenTexture();
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenTexture");

        try
        {
            SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
            SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, handle);
            OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture(Texture2D)");

            unsafe
            {
                // ponytail: RGBA8 plus mipmaps is what kept multi-GB models resident. BC3 is 1 byte/pixel.
                // Ceiling: desktop S3TC only. Fallback is the uncompressed upload below.
                var compressed = TryUploadBc3(data, width, height, internalFormat, dataFormat, generateMipmaps);
                if (!compressed)
                {
                    fixed (byte* ptr = data)
                    {
                        SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width,
                            (uint)height, 0, dataFormat, PixelType.UnsignedByte, ptr);
                        OpenGLDebug.CheckError(SilkNetContext.GL, "TexImage2D");
                    }
                }

                var minFilter = generateMipmaps
                    ? TextureMinFilter.LinearMipmapLinear
                    : TextureMinFilter.Linear;
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int)minFilter);
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int)TextureMagFilter.Linear);
                OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(filters)");

                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                    (int)TextureWrapMode.Repeat);
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                    (int)TextureWrapMode.Repeat);
                OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(wrap modes)");

                if (generateMipmaps && !compressed)
                {
                    SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
                    SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 10);
                    OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(mipmap levels)");

                    SilkNetContext.GL.GenerateMipmap(TextureTarget.Texture2D);
                    OpenGLDebug.CheckError(SilkNetContext.GL, "GenerateMipmap");
                }

                // Anisotropic filtering for sharp textures at oblique angles
                SilkNetContext.GL.TexParameter(TextureTarget.Texture2D,
                    (TextureParameterName)0x84FE, 16.0f); // GL_TEXTURE_MAX_ANISOTROPY_EXT
            }

            return new OpenGLTexture2D(path, handle, width, height, internalFormat,
                dataFormat == PixelFormat.Bgra ? PixelFormat.Rgba : dataFormat);
        }
        catch
        {
            SilkNetContext.GL.DeleteTexture(handle);
            throw;
        }
    }

    public override void Bind(int slot = 0)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0 + slot);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"ActiveTexture({slot})");
        // A leftover cubemap on the same unit makes sampler2D draws INVALID_OPERATION (macOS).
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, 0);
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
            throw new ArgumentException("Data must be entire texture!", nameof(data));
        }

        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "ActiveTexture(Texture0) in SetData");
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture in SetData");
        SilkNetContext.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)_internalFormat, (uint)Width, (uint)Height, 0,
            _dataFormat, PixelType.UnsignedByte, intPtrValue);
        OpenGLDebug.CheckError(SilkNetContext.GL, "TexImage2D in SetData");
    }

    private static unsafe bool TryUploadBc3(byte[] data, int width, int height, InternalFormat internalFormat,
        PixelFormat dataFormat, bool generateMipmaps)
    {
        if (dataFormat != PixelFormat.Rgba || data.Length != width * height * 4 || !HasS3tc())
            return false;

        var format = internalFormat == InternalFormat.Srgb8Alpha8
            ? (InternalFormat)0x8C4E // GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT
            : (InternalFormat)0x83F3; // GL_COMPRESSED_RGBA_S3TC_DXT5_EXT

        var mip = data;
        var mipWidth = width;
        var mipHeight = height;
        var level = 0;
        while (true)
        {
            var blocks = Bc3Encoder.Encode(mip, mipWidth, mipHeight);
            fixed (byte* ptr = blocks)
            {
                SilkNetContext.GL.CompressedTexImage2D(TextureTarget.Texture2D, level, format, (uint)mipWidth,
                    (uint)mipHeight, 0, (uint)blocks.Length, ptr);
                OpenGLDebug.CheckError(SilkNetContext.GL, "CompressedTexImage2D");
            }

            if (!generateMipmaps || (mipWidth == 1 && mipHeight == 1))
                break;

            mip = Bc3Encoder.Half(mip, mipWidth, mipHeight, out mipWidth, out mipHeight);
            level++;
        }

        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, level);
        return true;
    }

    private static bool? _hasS3tc;

    private static bool HasS3tc()
    {
        if (_hasS3tc.HasValue)
            return _hasS3tc.Value;

        // glGetString(GL_EXTENSIONS) is INVALID_ENUM in a core profile and the next CheckError throws it.
        _hasS3tc = false;
        var count = SilkNetContext.GL.GetInteger(GLEnum.NumExtensions);
        if (SilkNetContext.GL.GetError() != GLEnum.NoError)
            return false;

        for (uint i = 0; i < (uint)count; i++)
        {
            var name = SilkNetContext.GL.GetStringS(StringName.Extensions, i);
            if (SilkNetContext.GL.GetError() != GLEnum.NoError)
                return false;
            if (string.Equals(name, "GL_EXT_texture_compression_s3tc", StringComparison.Ordinal))
            {
                _hasS3tc = true;
                return true;
            }
        }

        return false;
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
        SilkNetContext.GL.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
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
        return _hashCode;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;


        if (disposing)
        {
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
                Debug.WriteLine($"Failed to delete OpenGL texture {_rendererId}: {e.Message}");
            }
        }

        _disposed = true;
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
        Dispose(false);
    }
#endif
}
