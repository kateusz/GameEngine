using Engine.Renderer;

namespace Editor.Features.Import;

/// <summary>
/// One Assimp mesh-bearing node: geometry in node-local space, transform relative to import root.
/// </summary>
internal readonly record struct AssimpModelPart(
    string Name,
    System.Numerics.Matrix4x4 LocalToRoot,
    IReadOnlyList<ModelSubmesh> Submeshes);

internal static class AssimpPartNaming
{
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "part";

        Span<char> buf = stackalloc char[raw.Length];
        var n = 0;
        foreach (var c in raw.Trim())
        {
            buf[n++] = c is '/' or '\\'
                       || Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0
                ? '_'
                : c;
        }

        var s = new string(buf[..n]).Trim('_', ' ', '.');
        return string.IsNullOrWhiteSpace(s) || s.Contains("..", StringComparison.Ordinal) ? "part" : s;
    }

    /// <summary>First use keeps base name; later duplicates get _1, _2, …</summary>
    public static string UniqueSanitize(string raw, Dictionary<string, int> counts)
    {
        var baseName = Sanitize(raw);
        if (!counts.TryGetValue(baseName, out var n))
        {
            counts[baseName] = 1;
            return baseName;
        }

        counts[baseName] = n + 1;
        return $"{baseName}_{n}";
    }
}
