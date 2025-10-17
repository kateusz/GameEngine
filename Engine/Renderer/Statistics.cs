namespace Engine.Renderer;

public class Statistics
{
    public uint DrawCalls { get; set; } = 0;
    public uint QuadCount { get; set; } = 0;
    public float EditorCameraX { get; set; }
    public float EditorCameraY { get; set; }
    public float EditorCameraZ { get; set; }

    public uint GetTotalVertexCount() => QuadCount * RenderingConstants.QuadVertexCount;
    public uint GetTotalIndexCount() => QuadCount * RenderingConstants.QuadIndexCount;
}