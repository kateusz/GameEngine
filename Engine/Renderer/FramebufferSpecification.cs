namespace Engine.Renderer;

public record FramebufferSpecification(uint Width, uint Height, uint Samples = 1, bool SwapChainTarget = false);