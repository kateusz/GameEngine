namespace Engine.Core;

internal sealed class AssetsManager : IAssetsManager
{
    public string AssetsPath { get; private set; } = Path.Combine(Environment.CurrentDirectory, "assets");
    
    public void SetAssetsPath(string path) => AssetsPath = path;
}