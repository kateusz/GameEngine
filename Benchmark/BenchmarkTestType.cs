namespace Benchmark;

public enum BenchmarkTestType
{
    None,
    Renderer2DStress,
    TextureSwitching,
    DrawCallOptimization,

    // Physics tests
    PhysicsBouncingBall,
    PhysicsFallingBodies,
    PhysicsStacking,
}