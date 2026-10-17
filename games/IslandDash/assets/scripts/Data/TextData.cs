namespace IslandDash;

/// <summary>Tiny parsing helpers shared by the manifest and level text formats (no System.Text.Json in game scripts).</summary>
public static class TextData
{
    public static List<string> Tokenize(string line)
    {
        var comment = line.IndexOf('#');
        if (comment >= 0)
            line = line[..comment];
        return line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public static Dictionary<string, string> ParseArgs(IReadOnlyList<string> tokens, int start)
    {
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = start; i < tokens.Count; i++)
        {
            var eq = tokens[i].IndexOf('=');
            if (eq > 0)
                args[tokens[i][..eq]] = tokens[i][(eq + 1)..];
            else
                args[tokens[i]] = "1";
        }

        return args;
    }

    public static float ParseFloat(string? text, float fallback) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    public static float[] ParseFloats(string? text, float[] fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        var parts = text.Split(',');
        var values = new float[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            values[i] = ParseFloat(parts[i], i < fallback.Length ? fallback[i] : 0f);
        return values;
    }

    /// <summary>"x,y,z" or (when <paramref name="uniformAllowed"/>) a single number used for all three axes.</summary>
    public static Vector3 ParseVector(string? text, Vector3 fallback = default, bool uniformAllowed = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        var v = ParseFloats(text, [fallback.X, fallback.Y, fallback.Z]);
        if (v.Length == 1)
            return uniformAllowed ? new Vector3(v[0]) : fallback;
        return new Vector3(v[0], v.Length > 1 ? v[1] : fallback.Y, v.Length > 2 ? v[2] : fallback.Z);
    }

    public static Vector4 ParseColor(string? text, Vector4 fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        var v = ParseFloats(text, [fallback.X, fallback.Y, fallback.Z, fallback.W]);
        return new Vector4(v[0], v.Length > 1 ? v[1] : fallback.Y, v.Length > 2 ? v[2] : fallback.Z, v.Length > 3 ? v[3] : 1f);
    }
}
