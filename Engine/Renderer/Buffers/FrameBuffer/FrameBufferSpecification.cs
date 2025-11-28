namespace Engine.Renderer.Buffers.FrameBuffer;

public sealed class FrameBufferSpecification(uint width, uint height, uint samples = 1, bool swapChainTarget = false)
{
    public uint Width { get; internal set; } = width;
    public uint Height { get; internal set; } = height;
    public uint Samples { get; } = samples;
    public bool SwapChainTarget { get; } = swapChainTarget;
    public FramebufferAttachmentSpecification AttachmentsSpec { get; init; } = null!;

    public void Deconstruct(out uint width, out uint height, out uint samples, out bool swapChainTarget)
    {
        width = Width;
        height = Height;
        samples = Samples;
        swapChainTarget = SwapChainTarget;
    }
}