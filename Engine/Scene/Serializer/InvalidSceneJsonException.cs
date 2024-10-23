namespace Engine.Scene.Serializer;

[Serializable]
public class InvalidSceneJsonException : Exception
{
    // Default constructor
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
}