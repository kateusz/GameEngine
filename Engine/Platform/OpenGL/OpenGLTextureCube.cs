using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Textures;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLTextureCube : TextureCube
{
    private uint _rendererId;
    private bool _disposed;

    public OpenGLTextureCube(uint rendererId, int size)
    {
        _rendererId = rendererId;
        Width = size;
        Height = size;
        Path = string.Empty;
    }

    public override uint GetRendererId() => _rendererId;

    public override void Bind(int slot = 0)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0 + slot);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"ActiveTexture({slot})");
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, 0);
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture(TextureCubeMap)");
    }

    public override void Unbind()
    {
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UnbindTextureCubeMap");
    }

    public static unsafe OpenGLTextureCube CreateBlack()
    {
        var gl = SilkNetContext.GL;
        var handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.TextureCubeMap, handle);

        Span<byte> black = stackalloc byte[4];
        for (var face = 0; face < 6; face++)
        {
            fixed (byte* ptr = black)
            {
                gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, 0, InternalFormat.Rgba8,
                    1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        OpenGLDebug.CheckError(gl, "CreateBlack cubemap");

        return new OpenGLTextureCube(handle, 1);
    }

    protected override void Dispose(bool disposing)
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
            Debug.WriteLine($"Failed to delete OpenGL cubemap {_rendererId}: {e.Message}");
        }

        _disposed = true;
    }
}
