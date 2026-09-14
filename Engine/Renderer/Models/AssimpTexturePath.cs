namespace Engine.Renderer.Models;

/// <summary>
/// Resolves Assimp external texture paths. FBX often stores Windows absolute paths from the
/// authoring machine; on macOS/Linux those must not be treated as relative to the model folder.
/// Marketplace FBX files also keep original .tga names while the files on disk are Unreal PNG
/// exports (T_ prefix, .png).
/// </summary>
internal static class AssimpTexturePath
{
    private static readonly string[] ImageExtensions = [".png", ".tga", ".jpg", ".jpeg"];
    private static readonly string[] AlbedoSuffixes = ["_D", "_BC", "_B", "_A"];

    public static string? Resolve(string texturePath, string modelDirectory)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
            return null;

        var normalized = Normalize(texturePath);
        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (TryExistingAbsolute(normalized, texturePath, out var existing))
            return existing;

        if (!IsAbsolute(normalized) && !string.IsNullOrEmpty(modelDirectory))
        {
            var relative = Path.GetFullPath(Path.Combine(modelDirectory, normalized));
            if (File.Exists(relative))
                return relative;
        }

        return FindByStem(modelDirectory, CandidateStems(Path.GetFileNameWithoutExtension(fileName)));
    }

    /// <summary>
    /// Stylized Unreal FBX often wires only the normal map. Pair T_Foo_N with T_Foo_D/BC/B/A beside the model.
    /// </summary>
    public static string? InferAlbedoFromNormal(string? normalPath, string modelDirectory)
    {
        if (string.IsNullOrEmpty(normalPath))
            return null;

        var normalized = Normalize(normalPath);
        var stem = Path.GetFileNameWithoutExtension(normalized);
        if (stem.Length < 3 || !stem.EndsWith("_N", StringComparison.OrdinalIgnoreCase))
            return null;

        var basename = stem[..^2];
        var albedoStems = new string[AlbedoSuffixes.Length];
        for (var i = 0; i < AlbedoSuffixes.Length; i++)
            albedoStems[i] = basename + AlbedoSuffixes[i];

        var hit = FindByStem(modelDirectory, albedoStems);
        if (hit != null)
            return hit;

        var normalDir = Path.GetDirectoryName(normalized);
        if (string.IsNullOrEmpty(normalDir) || string.IsNullOrEmpty(modelDirectory))
            return null;

        // Ghost Windows dirs (D:/Unreal) don't exist on macOS — skip GetFullPath.
        if (!Directory.Exists(normalDir))
            return null;

        if (string.Equals(
                Path.GetFullPath(normalDir),
                Path.GetFullPath(modelDirectory),
                StringComparison.OrdinalIgnoreCase))
            return null;

        return FindByStem(normalDir, albedoStems);
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static bool TryExistingAbsolute(string normalized, string original, out string? fullPath)
    {
        fullPath = null;
        if (!IsAbsolute(normalized))
            return false;

        // C:/… and //server/… are not local files on macOS/Linux.
        if (IsWindowsStyleAbsolute(normalized) && !OperatingSystem.IsWindows())
            return false;

        if (File.Exists(normalized))
        {
            fullPath = Path.GetFullPath(normalized);
            return true;
        }

        if (OperatingSystem.IsWindows())
        {
            var winPath = original.Replace('/', '\\');
            if (File.Exists(winPath))
            {
                fullPath = Path.GetFullPath(winPath);
                return true;
            }
        }

        return false;
    }

    private static bool IsAbsolute(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (Path.IsPathRooted(path))
            return true;

        return IsWindowsStyleAbsolute(path);
    }

    private static bool IsWindowsStyleAbsolute(string path)
    {
        if (path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith(@"\\", StringComparison.Ordinal))
            return true;

        return path.Length >= 3
               && char.IsAsciiLetter(path[0])
               && path[1] == ':'
               && (path[2] == '/' || path[2] == '\\');
    }

    private static List<string> CandidateStems(string stem)
    {
        var stems = new List<string> { stem };
        if (!stem.StartsWith("T_", StringComparison.OrdinalIgnoreCase))
            stems.Add("T_" + stem);
        if (stem.Equals("FlatNormal", StringComparison.OrdinalIgnoreCase))
            stems.Add("T_Default_N");
        return stems;
    }

    private static string? FindByStem(string directory, IReadOnlyList<string> stemsInOrder)
    {
        if (string.IsNullOrEmpty(directory) || stemsInOrder.Count == 0 || !Directory.Exists(directory))
            return null;

        foreach (var stem in stemsInOrder)
        {
            foreach (var ext in ImageExtensions)
            {
                var beside = Path.Combine(directory, stem + ext);
                if (File.Exists(beside))
                    return Path.GetFullPath(beside);
            }
        }

        var wanted = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < stemsInOrder.Count; i++)
            wanted.TryAdd(stemsInOrder[i], i);

        string? best = null;
        var bestRank = int.MaxValue;
        foreach (var hit in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
        {
            if (!IsImageExtension(Path.GetExtension(hit)))
                continue;

            var hitStem = Path.GetFileNameWithoutExtension(hit);
            if (!wanted.TryGetValue(hitStem, out var rank) || rank >= bestRank)
                continue;

            bestRank = rank;
            best = hit;
            if (rank == 0)
                break;
        }

        return best == null ? null : Path.GetFullPath(best);
    }

    private static bool IsImageExtension(string ext)
    {
        foreach (var image in ImageExtensions)
        {
            if (ext.Equals(image, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
