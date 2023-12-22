using Engine.Renderer;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace Engine.Platform.OpenGL;

// TODO: add support for both RGB and RGBA
public class OpenGLTexture2D : Texture2D
{
    private readonly string _path;
    private readonly int _rendererId;
    private readonly PixelInternalFormat _internalFormat;
    private readonly PixelFormat _dataFormat;

    private OpenGLTexture2D(int rendererId, int width, int height, PixelInternalFormat internalFormat,
        PixelFormat dataFormat)
    {
        _rendererId = rendererId;
        _internalFormat = internalFormat;
        _dataFormat = dataFormat;

        Width = width;
        Height = height;
    }

    private OpenGLTexture2D(string path, int rendererId, int width, int height, PixelInternalFormat internalFormat,
        PixelFormat dataFormat)
    {
        _path = path;
        _rendererId = rendererId;
        _internalFormat = internalFormat;
        _dataFormat = dataFormat;

        Width = width;
        Height = height;
    }

    ~OpenGLTexture2D()
    {
    }

    public static async Task<OpenGLTexture2D> Create(string path)
    {
        // Generate handle
        var handle = GL.GenTexture();

        // Bind the handle
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);

        StbImage.stbi_set_flip_vertically_on_load(1);

        var width = 0;
        var height = 0;
        const PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8;
        const PixelFormat dataFormat = PixelFormat.Rgba;

        // Here we open a stream to the file and pass it to StbImageSharp to load.
        await using (var stream = File.OpenRead(path))
        {
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            width = image.Width;
            height = image.Height;

            GL.TexImage2D(
                TextureTarget.Texture2D,
                level: 0,
                internalFormat,
                image.Width,
                image.Height,
                border: 0,
                dataFormat,
                PixelType.UnsignedByte,
                image.Data);
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        return new OpenGLTexture2D(path, handle, width, height, internalFormat, dataFormat);
    }

    [Obsolete("Crash")]
    public static async Task<OpenGLTexture2D> Create(int width, int height)
    {
        // Generate handle
        var rendererId = GL.GenTexture();
        
        const PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8;
        const PixelFormat dataFormat = PixelFormat.Rgba;

        // Bind the handle
        // TODO: CreateTextures crashes the program
        GL.CreateTextures(TextureTarget.Texture2D, 1, new int[1] { rendererId });
        GL.TextureStorage2D(rendererId, 1, SizedInternalFormat.Rgba8, width, height);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        return new OpenGLTexture2D(rendererId, width, height, internalFormat, dataFormat);
    }

    // Activate texture
    // Multiple textures can be bound, if your shader needs more than just one.
    // If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.
    // The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
    // Original ver: public void Use(TextureUnit unit)
    public override void Bind(int slot)
    {
        // TODO: map slot to TextureUnit
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _rendererId);
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

        GL.TextureSubImage2D(_rendererId, 0, 0, 0, Width, Height, _dataFormat, PixelType.UnsignedByte,
            intPtrValue);
    }
}