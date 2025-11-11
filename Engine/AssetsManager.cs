namespace Engine;

public static class AssetsManager
{
    private static string _assetsPath = Path.Combine(Environment.CurrentDirectory, "assets");
    public static string AssetsPath => _assetsPath;

    public static void SetAssetsPath(string path)
    {
        _assetsPath = path;
    }
}