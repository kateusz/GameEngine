namespace Engine.Renderer.Buffers;

public interface IIndexBuffer : IBindable, IDisposable
{
    int Count { get; }
}