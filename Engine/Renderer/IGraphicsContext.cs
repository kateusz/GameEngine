namespace Engine.Renderer;

public interface IGraphicsContext : IDisposable
{
    bool IsCreated { get; }
    void Create();
}
