namespace Engine.Renderer.Buffers;

public class BufferLayout
{
    public BufferLayout(IList<BufferElement> elements)
    {
        Elements = elements;
        CalculateOffsetsAndStride();
    }
    
    public IList<BufferElement> Elements { get; set; }
    public int Stride { get; set; }
    
    private void CalculateOffsetsAndStride()
    {
        var offset = 0;
        Stride = 0;
        
        foreach (var element in Elements)
        {
            element.Offset = offset;
            offset += element.Size;
            Stride += element.Size;
        }
    }
}