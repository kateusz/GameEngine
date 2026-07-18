using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers.FrameBuffer;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests.ImageRegression;

internal static class GlFramebufferCapture
{
    public static byte[] ReadColorRgba8(IFrameBuffer framebuffer, int attachmentIndex = 0)
    {
        var (width, height, _, _) = framebuffer.GetSpecification();
        framebuffer.Bind();

        try
        {
            return ReadBoundColorRgba8((int)width, (int)height, attachmentIndex);
        }
        finally
        {
            framebuffer.Unbind();
        }
    }

    public static byte[] ReadBoundColorRgba8(int width, int height, int attachmentIndex = 0)
    {
        var gl = SilkNetContext.GL;
        var pixels = new byte[width * height * 4];

        gl.ReadBuffer(GLEnum.ColorAttachment0 + attachmentIndex);
        unsafe
        {
            fixed (byte* ptr = pixels)
            {
                gl.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        FlipVertical(pixels, width, height);
        return pixels;
    }

    private static void FlipVertical(byte[] pixels, int width, int height)
    {
        var stride = width * 4;
        var row = new byte[stride];
        for (var y = 0; y < height / 2; y++)
        {
            var top = y * stride;
            var bottom = (height - 1 - y) * stride;
            System.Buffer.BlockCopy(pixels, top, row, 0, stride);
            System.Buffer.BlockCopy(pixels, bottom, pixels, top, stride);
            System.Buffer.BlockCopy(row, 0, pixels, bottom, stride);
        }
    }
}
