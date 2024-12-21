namespace Engine.Renderer;

public record Statistics
{
    public uint DrawCalls { get; set; } = 0;
    public uint QuadCount { get; set; } = 0;
    public float EditorCameraX { get; set; }
    public float EditorCameraY { get; set; }
    public float EditorCameraZ { get; set; }

    public uint GetTotalVertexCount() { return QuadCount * 4; }
    public uint GetTotalIndexCount() { return QuadCount * 6; }
}