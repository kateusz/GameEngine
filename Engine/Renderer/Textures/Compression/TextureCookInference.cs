namespace Engine.Renderer.Textures.Compression;

internal static class TextureCookInference
{
    public static bool IsCookableExtension(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".tga";

    public static bool IsNormalLoad(string path, bool sRgb) =>
        !sRgb && NameContains(path, "normal", "nrm");

    public static (bool SRgb, bool IsNormal) InferCookRole(string path)
    {
        var normal = NameContains(path, "normal", "nrm");
        var linear = normal || NameContains(path, "orm", "metallic", "roughness", "occlusion", "ao");
        return (!linear, normal);
    }

    private static bool NameContains(string path, params string[] tokens)
    {
        var stem = Path.GetFileNameWithoutExtension(path);
        foreach (var token in tokens)
        {
            if (stem.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
