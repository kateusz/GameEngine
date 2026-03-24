namespace Benchmark;

public enum BenchmarkTestType
{
    None,
    Renderer2DStress,
    TextureSwitching,
    DrawCallOptimization,
    BatchSaturation,
    TextureAtlasPressure,
    ShaderSwitchCost,
    SystemIsolation,
    FramebufferStress,
    AllocationHunt,
}