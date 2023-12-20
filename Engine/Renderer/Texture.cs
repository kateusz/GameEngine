namespace Engine.Renderer;

public abstract class Texture
{
    public int Width { get; set; }
    public int Height { get; set; }

    public virtual void Bind(int slot = 0)
    {
        
    }

    public virtual void SetData(byte[] data, int size)
    {
        
    }
}

public class Texture2D : Texture
{
    
}