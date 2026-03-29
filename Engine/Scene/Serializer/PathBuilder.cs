using Engine.Core;
using Engine.Platform;

namespace Engine.Scene.Serializer;

public static class PathBuilder
{
    public static string Build(string path)
    {
        if (OSInfo.IsWindows)
        {
            path = path.Replace('/', '\\');
        }
        else if (OSInfo.IsMacOS)
        {
            path = path.Replace('\\', '/');
        }

        return Path.Combine(AssetsManager.AssetsPath, path);
    }
}