using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Textures;

namespace Ui.ImGui;

public static class ImGuiNativeTexture
{
    public static IntPtr From(Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        return (IntPtr)texture.GetRendererId();
    }

    public static IntPtr FromColorAttachment(IFrameBuffer frameBuffer)
    {
        ArgumentNullException.ThrowIfNull(frameBuffer);
        return (IntPtr)frameBuffer.GetColorAttachmentRendererId();
    }
}
