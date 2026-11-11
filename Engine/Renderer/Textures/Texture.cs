using System.Numerics;
using System.Text.Json.Serialization;

namespace Engine.Renderer.Textures;

public abstract class Texture : IDisposable
{
    [JsonIgnore]
    public virtual int Width { get; set; }

    [JsonIgnore]
    public virtual int Height { get; set; }

    /// <summary>Maps 0–1 UV to the logical image when GPU storage is padded to a BC block grid.</summary>
    [JsonIgnore]
    public virtual Vector2 UvScale { get; init; } = Vector2.One;

    /// <summary>BC5 normal maps omit Z; reconstruct in the shader when true.</summary>
    [JsonIgnore]
    public virtual bool NormalTangentZReconstruct { get; init; }

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}