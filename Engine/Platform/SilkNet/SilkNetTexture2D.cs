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

namespace Engine.Platform.SilkNet;

public class SilkNetTexture2D : Texture2D
{
    private readonly string _path;
    private readonly uint _rendererId;
    private readonly InternalFormat _internalFormat;
    private readonly PixelFormat _dataFormat;
    
    private SilkNetTexture2D(uint rendererId, int width, int height, InternalFormat internalFormat,
        PixelFormat dataFormat)
    {
        _rendererId = rendererId;
        _internalFormat = internalFormat;
        _dataFormat = dataFormat;

        Width = width;
        Height = height;
    }

    private SilkNetTexture2D(string path, uint rendererId, int width, int height, InternalFormat internalFormat,
        PixelFormat dataFormat)
    {
        _path = path;
        _rendererId = rendererId;
        _internalFormat = internalFormat;
        _dataFormat = dataFormat;

        Width = width;
        Height = height;
    }
    
    public static Texture2D Create(string path)
    {
        // Generate handle
        var handle = SilkNetContext.GL.GenTexture();

        // Bind the handle
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, handle);

        StbImage.stbi_set_flip_vertically_on_load(1);

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
                    SilkNetContext.GL.TexImage2D(
                        TextureTarget.Texture2D,
                        level: 0,
                        internalFormat,
                        (uint)image.Width,
                        (uint)image.Height,
                        border: 0,
                        dataFormat,
                        GLEnum.UnsignedByte,
                        ptr);
                }
            }
        }

        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        SilkNetContext.GL.GenerateMipmap(TextureTarget.Texture2D);

        return new SilkNetTexture2D(path, handle, width, height, internalFormat, dataFormat);
    }
    
    // TODO: check whether it works
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

        SilkNetContext.GL.TextureSubImage2D(_rendererId, 0, 0, 0, (uint)Width, (uint)Height, _dataFormat, PixelType.UnsignedByte,
            intPtrValue);
    }
}