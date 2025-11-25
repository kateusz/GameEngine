namespace Engine;

public interface IAssetsManager
{
    string AssetsPath { get; }
    void SetAssetsPath(string path);
}

public class AssetsManager : IAssetsManager
{
    public string AssetsPath { get; private set; } = Path.Combine(Environment.CurrentDirectory, "assets");
    
    public void SetAssetsPath(string path) => AssetsPath = path;
}