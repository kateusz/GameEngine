namespace Engine.Renderer;

/// <summary>
/// Per-frame 2D renderer statistics (batching, uploads, CPU and GPU timing).
/// </summary>
public record Graphics2DStats
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
    /// <summary>GPU quad pass ms from previous frame (DEBUG timer queries only).</summary>
    public double GpuQuadPassMs { get; set; }
    /// <summary>GPU line pass ms from previous frame (DEBUG timer queries only).</summary>
    public double GpuLinePassMs { get; set; }

    public uint GetTotalVertexCount() => QuadCount * RenderingConstants.QuadVertexCount;
    public uint GetTotalIndexCount() => QuadCount * RenderingConstants.QuadIndexCount;
    public uint GetTotalDrawCalls() => DrawCalls + LineDrawCalls;
}
