namespace Engine.Renderer.Buffers;

public record FrameBufferSpecification(uint Width, uint Height, uint Samples = 1, bool SwapChainTarget = false);