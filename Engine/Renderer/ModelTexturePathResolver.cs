namespace Engine.Renderer;

internal static class ModelTexturePathResolver
{
    internal static string? ResolveSpecularSibling(string? albedoPath)
    {
        if (string.IsNullOrEmpty(albedoPath))
            return null;

        var specularPath = albedoPath.Replace("_BaseColor", "_Specular", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(specularPath, albedoPath, StringComparison.OrdinalIgnoreCase))
            return null;

        return System.IO.File.Exists(specularPath) ? specularPath : null;
    }
}
