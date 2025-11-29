namespace Engine.Renderer.Buffers;

public struct BufferLayout
{
    public IList<BufferElement> Elements { get; }
    public int Stride { get; private set; }
    
    public BufferLayout(IList<BufferElement> elements)
    {
        Elements = elements;
        Stride = 0;
        CalculateOffsetsAndStride();
    }
    
    private void CalculateOffsetsAndStride()
    {
        var offset = 0;
        Stride = 0;
        
        // Use for loop instead of foreach to avoid readonly iteration variable issue with structs
        for (var i = 0; i < Elements.Count; i++)
        {
            var element = Elements[i];
            element.Offset = offset;
            offset += element.Size;
            Stride += element.Size;
            Elements[i] = element; // Update the element in the list
        }
    }
}