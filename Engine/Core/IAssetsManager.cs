namespace Engine.Core;

public interface IAssetsManager
{
    string AssetsPath { get; }
    void SetAssetsPath(string path);
}