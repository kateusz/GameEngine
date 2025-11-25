namespace Engine;

/// <summary>
/// Interface for managing asset paths in the game engine.
/// </summary>
public interface IAssetsManager
{
    /// <summary>
    /// Gets the current assets directory path.
    /// </summary>
    string AssetsPath { get; }

    /// <summary>
    /// Sets the assets directory path.
    /// </summary>
    /// <param name="path">The new assets directory path.</param>
    void SetAssetsPath(string path);
}

/// <inheritdoc />
public class AssetsManager : IAssetsManager
{
    public string AssetsPath { get; private set; } = Path.Combine(Environment.CurrentDirectory, "assets");

    /// <inheritdoc />
    public void SetAssetsPath(string path)
    {
        AssetsPath = path;
    }
}