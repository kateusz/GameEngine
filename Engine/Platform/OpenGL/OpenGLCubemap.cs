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

    public OpenGLCubemap(uint size, InternalFormat internalFormat, int mipLevels = 1)
    {
        Size = size;
        var gl = SilkNetContext.GL;

        _textureId = gl.GenTexture();
        gl.BindTexture(TextureTarget.TextureCubeMap, _textureId);

        // Allocate storage for all 6 faces at each mip level explicitly
        for (var mip = 0; mip < mipLevels; mip++)
        {
            var mipSize = System.Math.Max(1u, size >> mip);
            for (var face = 0; face < 6; face++)
            {
                unsafe
                {
                    gl.TexImage2D(
                        TextureTarget.TextureCubeMapPositiveX + face, mip, internalFormat,
                        mipSize, mipSize, 0, PixelFormat.Rgba, PixelType.Float, null);
                }
            }
        }

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            mipLevels > 1 ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        // Clamp max mip level to prevent sampling beyond allocated levels
        if (mipLevels > 1)
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, mipLevels - 1);

        gl.BindTexture(TextureTarget.TextureCubeMap, 0);

        Logger.Debug("Created cubemap {Size}x{Size}, format={Format}, mipLevels={MipLevels}",
            size, size, internalFormat, mipLevels);
    }

    public void Bind(int slot)
    {
        var gl = SilkNetContext.GL;
        gl.ActiveTexture(TextureUnit.Texture0 + slot);
        gl.BindTexture(TextureTarget.TextureCubeMap, _textureId);
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
