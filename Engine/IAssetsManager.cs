namespace Engine;

public interface IAssetsManager
{
    string AssetsPath { get; }
    void SetAssetsPath(string path);
}