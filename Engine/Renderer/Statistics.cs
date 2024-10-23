namespace Engine.Renderer;

public class Statistics
{
    public uint DrawCalls { get; set; }
    public uint QuadCount { get; set; }

    public Statistics(uint drawCalls, uint quadCount)
    {
        DrawCalls = drawCalls;
        QuadCount = quadCount;
    }

    public uint GetTotalVertexCount() { return QuadCount * 4; }
    public uint GetTotalIndexCount() { return QuadCount * 6; }
}