namespace Engine.Renderer.Textures.Compression;

internal static class TextureBcSource
{
    public static bool IsRasterSourceExtension(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".tga";

    public static bool IsNormalLoad(string path, bool sRgb) =>
        !sRgb && NameContains(path, "normal", "nrm");

    public static bool IsOcclusionMap(string path) =>
        NameContains(path, "occlusion", "ao");

    public static (bool SRgb, bool IsNormal) InferEncodeRole(string path)
    {
        var normal = IsNormalLoad(path, sRgb: false);
        var linear = normal
                     || NameContains(path, "orm", "metallic", "roughness", "occlusion", "ao");
        return (!linear, normal);
    }

    private static bool NameContains(string path, params string[] tokens)
    {
        var stem = Path.GetFileNameWithoutExtension(path);
        foreach (var part in stem.Split(['_', '-', '.', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var token in tokens)
            {
                if (part.Equals(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
