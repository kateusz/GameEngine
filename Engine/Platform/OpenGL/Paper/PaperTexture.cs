using Engine.Platform.SilkNet;
using Prowl.Vector;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Paper;

internal sealed class PaperTexture(uint handle)
{
    public uint Width;
    public uint Height;

    public static PaperTexture CreateNew(uint width, uint height)
    {
        SilkNetContext.EnsureCurrent();
        var gl = SilkNetContext.GL
            ?? throw new InvalidOperationException("OpenGL context is not ready for Paper texture creation.");

        var handle = gl.GenTexture();

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, handle);
        gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
        unsafe
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, null);
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        return new PaperTexture(handle) { Width = width, Height = height };
    }

    public void SetData(IntRect bounds, byte[] data)
    {
        var gl = SilkNetContext.GL;
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, handle);

        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.TexSubImage2D(TextureTarget.Texture2D, 0, bounds.Min.X, bounds.Min.Y, (uint)bounds.Size.X,
                    (uint)bounds.Size.Y, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }
    }

    public void Use(TextureUnit unit)
    {
        var gl = SilkNetContext.GL;
        gl.ActiveTexture(unit);
        gl.BindTexture(TextureTarget.Texture2D, handle);
    }

    public void Dispose()
    {
        SilkNetContext.GL.DeleteTexture(handle);
    }
}
