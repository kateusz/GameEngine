namespace Engine.Renderer.Buffers;

public class FrameBufferSpecification(uint width, uint height, uint samples = 1, bool swapChainTarget = false)
{
    public uint Width { get;  set; } = width;
    public uint Height { get;  set; } = height;
    public uint Samples { get; init; } = samples;
    public bool SwapChainTarget { get; init; } = swapChainTarget;

    public void Deconstruct(out uint width, out uint height, out uint samples, out bool swapChainTarget)
    {
        width = this.Width;
        height = this.Height;
        samples = this.Samples;
        swapChainTarget = this.SwapChainTarget;
    }
}