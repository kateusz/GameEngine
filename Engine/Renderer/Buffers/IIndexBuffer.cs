namespace Engine.Renderer.Buffers;

public interface IIndexBuffer : IBindable
{
    int Count { get; }
}