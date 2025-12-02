using System.Text.Json.Serialization;

namespace Engine.Renderer.Textures;

public abstract class Texture : IDisposable
{
    [JsonIgnore]
    public virtual int Width { get; set; }

    [JsonIgnore]
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

    public virtual void Dispose()
    {
    }
}