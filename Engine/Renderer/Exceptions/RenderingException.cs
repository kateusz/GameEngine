namespace Engine.Renderer.Exceptions;

/// <summary>
/// Exception thrown when a rendering operation fails.
/// </summary>
[Serializable]
public class RenderingException : Exception
{
    public RenderingException()
        : base("A rendering operation failed.")
    {
    }

    public RenderingException(string message)
        : base(message)
    {
    }

    public RenderingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
