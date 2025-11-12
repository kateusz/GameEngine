using System.Runtime.Serialization;

namespace Engine.Renderer.Exceptions;

/// <summary>
/// Exception thrown when the renderer fails to initialize.
/// </summary>
[Serializable]
public class RendererInitializationException : Exception
{
    public RendererInitializationException()
        : base("Failed to initialize renderer.")
    {
    }

    public RendererInitializationException(string message)
        : base(message)
    {
    }

    public RendererInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected RendererInitializationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
