namespace Engine.Renderer.Textures;

public abstract class Texture : IDisposable
{
    protected bool _disposed = false;

    public virtual int Width { get; set; }
    public virtual int Height { get; set; }

    public string? Path { get; set; }

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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources if needed by derived classes
            }

            // Derived classes should override this method to dispose unmanaged OpenGL resources
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Texture()
    {
        // Finalizer for safety - warns if Dispose not called
        Dispose(false);
    }
}