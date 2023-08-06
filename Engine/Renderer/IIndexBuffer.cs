namespace Engine.Renderer;

public interface IIndexBuffer
{
    void Bind();
    void Unbind();
    int GetCount();
}