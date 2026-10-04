namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshPaths
{
    public static string SiblingPath(string sourcePath) => sourcePath + ".mesh";

    public static string TextureFolderPath(string sourcePath) => sourcePath + ".tex";

    public static bool IsModelSourceExtension(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".glb", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".gltf", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".fbx", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ToRelativeAssetPath(string sourceDirectory, string? absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return null;

        var full = Path.GetFullPath(absolutePath);
        var rel = Path.GetRelativePath(Path.GetFullPath(sourceDirectory), full);
        if (rel.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
            return null;

        return rel.Replace('\\', '/');
    }

    public static string ResolveFromSourceDirectory(string sourceDirectory, string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return string.Empty;

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(sourceDirectory, normalized));
    }
}
