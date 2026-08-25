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

        var stem = Path.GetFileNameWithoutExtension(fileName);
        return FindByStem(modelDirectory, CandidateStems(stem));
    }

    /// <summary>
    /// Stylized Unreal FBX often wires only the normal map. Pair T_Foo_N with T_Foo_D/BC/B/A beside the model.
    /// </summary>
    public static string? InferAlbedoFromNormal(string? normalPath, string modelDirectory)
    {
        if (string.IsNullOrEmpty(normalPath))
            return null;

        var stem = Path.GetFileNameWithoutExtension(normalPath);
        if (stem.Length < 3 || !stem.EndsWith("_N", StringComparison.OrdinalIgnoreCase))
            return null;

        var basename = stem[..^2];
        var albedoStems = new string[AlbedoSuffixes.Length];
        for (var i = 0; i < AlbedoSuffixes.Length; i++)
            albedoStems[i] = basename + AlbedoSuffixes[i];

        var hit = FindByStem(modelDirectory, albedoStems);
        if (hit != null)
            return hit;

        var normalDir = Path.GetDirectoryName(normalPath);
        if (string.IsNullOrEmpty(normalDir) || string.IsNullOrEmpty(modelDirectory))
            return null;

        if (string.Equals(
                Path.GetFullPath(normalDir),
                Path.GetFullPath(modelDirectory),
                StringComparison.OrdinalIgnoreCase))
            return null;

        return FindByStem(normalDir, albedoStems);
    }

    private static bool IsAbsolute(string path)
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
