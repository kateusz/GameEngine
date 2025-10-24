namespace Engine.Renderer;

public interface IBindable : IDisposable
{
    void Bind();
    void Unbind();
}