namespace Engine.Renderer.Textures;

public abstract class Texture
{
    public int Width { get; set; }
    public int Height { get; set; }
    // todo: load texture from path during deserialization
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
}