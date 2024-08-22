using Engine.Renderer.Buffers.FrameBuffer;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class Utils
{
    private static GLEnum TextureTarget(bool multisampled)
    {
        return multisampled ? GLEnum.Texture2DMultisample : GLEnum.Texture2D;
    }

    public static void CreateTextures(GL gl, bool multisampled, uint[] outID, uint count)
    {
        gl.GenTextures((uint)count, outID);
    }

    public static void BindTexture(GL gl, bool multisampled, uint id)
    {
        gl.BindTexture(TextureTarget(multisampled), id);
    }

    public static void AttachColorTexture(GL gl, uint id, uint samples, GLEnum format, uint width, uint height,
        int index)
    {
        bool multisampled = samples > 1;
        if (multisampled)
        {
            gl.TexImage2DMultisample(GLEnum.Texture2DMultisample, samples, format, width, height, false);
        }
        else
        {
            unsafe
            {
                gl.BindTexture(GLEnum.Texture2D, id);
                gl.TexImage2D(GLEnum.Texture2D, 0, (int)format, width, height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)0);

                // Set texture parameters for filtering and wrapping
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
            }
        }

        gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0 + index, TextureTarget(multisampled), id,
            0);
    }

    public static void AttachDepthTexture(GL gl, uint id, uint samples, GLEnum format, GLEnum attachmentType, uint width,
        uint height)
    {
        bool multisampled = samples > 1;
        if (multisampled)
        {
            gl.TexImage2DMultisample(GLEnum.Texture2DMultisample, samples, format, width, height, false);
        }
        else
        {
            unsafe
            {
                gl.BindTexture(GLEnum.Texture2D, id);
                gl.TexImage2D(GLEnum.Texture2D, 0, (int)format, width, height, 0, GLEnum.DepthComponent,
                    GLEnum.UnsignedByte, (void*)0);

                // Set texture parameters for filtering and wrapping
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
            }
        }

        gl.FramebufferTexture2D(GLEnum.Framebuffer, attachmentType, TextureTarget(multisampled), id, 0);
    }

    public static bool IsDepthFormat(FramebufferTextureFormat format)
    {
        switch (format)
        {
            case FramebufferTextureFormat.DEPTH24STENCIL8:
                return true;
            default:
                return false;
        }
    }
}