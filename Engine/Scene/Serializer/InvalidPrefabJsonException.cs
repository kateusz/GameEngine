namespace Engine.Scene.Serializer;

[Serializable]
public class InvalidPrefabJsonException : Exception
{
    public InvalidPrefabJsonException()
        : base("The prefab JSON is invalid.")
    {
    }

    public InvalidPrefabJsonException(string message)
        : base(message)
    {
    }

    public InvalidPrefabJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
