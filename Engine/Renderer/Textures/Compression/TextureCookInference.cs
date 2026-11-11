namespace Engine.Renderer.Textures.Compression;

internal static class TextureCookInference
{
    private static readonly string[] NormalTokens = ["normal", "nrm"];
    private static readonly string[] OcclusionTokens = ["occlusion", "ao"];
    private static readonly string[] LinearDataTokens =
        ["normal", "nrm", "orm", "metallic", "roughness", "occlusion", "ao"];

    public static bool IsCookableExtension(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".tga", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLinearDataMapName(string fileNameWithoutExtension)
    {
        foreach (var token in LinearDataTokens)
        {
            if (ContainsToken(fileNameWithoutExtension, token))
                return true;
        }

        return false;
    }

    public static bool IsNormalMapPath(string path) =>
        ContainsAnyToken(Path.GetFileNameWithoutExtension(path), NormalTokens);

    public static bool IsOcclusionMapPath(string path) =>
        ContainsAnyToken(Path.GetFileNameWithoutExtension(path), OcclusionTokens);

    public static bool IsNormalLoad(string path, bool sRgb) => !sRgb && IsNormalMapPath(path);

    public static (bool SRgb, bool IsNormal) InferCookRole(string sourcePath)
    {
        var stem = Path.GetFileNameWithoutExtension(sourcePath);
        var linear = IsLinearDataMapName(stem);
        return (SRgb: !linear, IsNormal: IsNormalMapPath(sourcePath));
    }

    private static bool ContainsAnyToken(string stem, string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (ContainsToken(stem, token))
                return true;
        }

        return false;
    }

    private static bool ContainsToken(string stem, string token)
    {
        if (stem.Equals(token, StringComparison.OrdinalIgnoreCase))
            return true;
        return stem.Contains('_' + token, StringComparison.OrdinalIgnoreCase)
               || stem.Contains('-' + token, StringComparison.OrdinalIgnoreCase)
               || stem.StartsWith(token + '_', StringComparison.OrdinalIgnoreCase)
               || stem.StartsWith(token + '-', StringComparison.OrdinalIgnoreCase)
               || stem.EndsWith('_' + token, StringComparison.OrdinalIgnoreCase)
               || stem.EndsWith('-' + token, StringComparison.OrdinalIgnoreCase);
    }
}
