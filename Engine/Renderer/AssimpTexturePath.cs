namespace Engine.Renderer;

/// <summary>
/// Resolves Assimp external texture paths. FBX often stores Windows absolute paths from the
/// authoring machine; on macOS/Linux those must not be treated as relative to the model folder.
/// </summary>
internal static class AssimpTexturePath
{
    public static string? Resolve(string texturePath, string modelDirectory)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
            return null;

        var normalized = texturePath.Replace('\\', '/');
        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (IsAbsolute(normalized))
        {
            if (File.Exists(normalized))
                return Path.GetFullPath(normalized);
            if (OperatingSystem.IsWindows())
            {
                var winPath = texturePath.Replace('/', '\\');
                if (File.Exists(winPath))
                    return Path.GetFullPath(winPath);
            }
        }
        else if (!string.IsNullOrEmpty(modelDirectory))
        {
            var relative = Path.GetFullPath(Path.Combine(modelDirectory, normalized));
            if (File.Exists(relative))
                return relative;
        }

        if (string.IsNullOrEmpty(modelDirectory) || !Directory.Exists(modelDirectory))
            return null;

        var beside = Path.Combine(modelDirectory, fileName);
        if (File.Exists(beside))
            return Path.GetFullPath(beside);
        
        foreach (var hit in Directory.EnumerateFiles(modelDirectory, fileName, SearchOption.AllDirectories))
            return Path.GetFullPath(hit);

        return null;
    }
    
    public static bool IsAbsolute(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (Path.IsPathRooted(path))
            return true;

        // UNC
        if (path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith(@"\\", StringComparison.Ordinal))
            return true;

        // Windows drive: E:/… or E:\…
        return path.Length >= 3
               && char.IsAsciiLetter(path[0])
               && path[1] == ':'
               && (path[2] == '/' || path[2] == '\\');
    }
}
