namespace Engine.Renderer;

/// <summary>
/// Per-frame 2D renderer statistics (batching, uploads, CPU and GPU timing).
/// </summary>
public class Graphics2DStats
{
    public uint DrawCalls { get; set; }
    public uint QuadCount { get; set; }
    public uint LineDrawCalls { get; set; }
    public uint LineVertexCount { get; set; }
    public uint BatchCount { get; set; }
    public uint TextureBinds { get; set; }
    public uint ProgramSwitches { get; set; }
    public long UploadBytes { get; set; }
    public double BatchFillMs { get; set; }
    public double FlushMs { get; set; }
    /// <summary>GPU time for quad draw pass from the previous frame (timer query lag).</summary>
    public double GpuQuadPassMs { get; set; }
    /// <summary>GPU time for line draw pass from the previous frame (timer query lag).</summary>
    public double GpuLinePassMs { get; set; }

    public uint GetTotalVertexCount() => QuadCount * RenderingConstants.QuadVertexCount;
    public uint GetTotalIndexCount() => QuadCount * RenderingConstants.QuadIndexCount;
    public uint GetTotalDrawCalls() => DrawCalls + LineDrawCalls;
}
