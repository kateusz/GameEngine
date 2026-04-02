using System.Runtime.Serialization;

namespace Engine.Scene.Serializer;

[Serializable]
public class InvalidSceneJsonException : Exception
{
    public InvalidSceneJsonException()
        : base("The scene JSON is invalid.")
    {
    }

    public InvalidSceneJsonException(string message)
        : base(message)
    {
    }

    public InvalidSceneJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected InvalidSceneJsonException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}