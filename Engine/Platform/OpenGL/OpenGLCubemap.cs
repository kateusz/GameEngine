using Engine.Platform.SilkNet;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLCubemap : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLCubemap>();

    private uint _textureId;
    private bool _disposed;

    public uint TextureId => _textureId;
    public uint Size { get; }

    public OpenGLCubemap(uint size, InternalFormat internalFormat, bool generateMipmaps = false)
    {
        Size = size;
        var gl = SilkNetContext.GL;

        _textureId = gl.GenTexture();
        gl.BindTexture(TextureTarget.TextureCubeMap, _textureId);

        // Allocate storage for all 6 faces
        for (var i = 0; i < 6; i++)
        {
            unsafe
            {
                gl.TexImage2D(
                    TextureTarget.TextureCubeMapPositiveX + i, 0, internalFormat,
                    size, size, 0, PixelFormat.Rgb, PixelType.Float, null);
            }
        }

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);

        if (generateMipmaps)
        {
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear);
        }
        else
        {
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
        }

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        if (generateMipmaps)
        {
            gl.GenerateMipmap(TextureTarget.TextureCubeMap);
        }

        gl.BindTexture(TextureTarget.TextureCubeMap, 0);

        Logger.Debug("Created cubemap {Size}x{Size}, format={Format}, mipmaps={Mipmaps}",
            size, size, internalFormat, generateMipmaps);
    }

    public void Bind(int slot)
    {
        var gl = SilkNetContext.GL;
        gl.ActiveTexture(TextureUnit.Texture0 + slot);
        gl.BindTexture(TextureTarget.TextureCubeMap, _textureId);
    }

    public void GenerateMipmaps()
    {
        var gl = SilkNetContext.GL;
        gl.BindTexture(TextureTarget.TextureCubeMap, _textureId);
        gl.GenerateMipmap(TextureTarget.TextureCubeMap);
        gl.BindTexture(TextureTarget.TextureCubeMap, 0);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_textureId != 0)
        {
            SilkNetContext.GL.DeleteTexture(_textureId);
            _textureId = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
