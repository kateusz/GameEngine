namespace Engine.Core;

public static class AssetsManager
{
    public static string AssetsPath { get; private set; } = Path.Combine(Environment.CurrentDirectory, "assets");
    
    public static void SetAssetsPath(string path) => AssetsPath = path;
}