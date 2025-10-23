namespace Engine.Renderer.Textures;

public abstract class Texture : IDisposable
{
    public int Width { get; set; }
    public int Height { get; set; }

    public string Path { get; set; }

    public virtual void Bind(int slot = 0)
    {
    }

    public virtual void Unbind()
    {
    }

    public virtual void SetData(uint data, int size)
    {
    }

    public virtual uint GetRendererId()
    {
        return 0;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}