using System.IO;

namespace IslandDash;

/// <summary>One kit piece placement from a level file.</summary>
public sealed class PieceSpec(string kind, Vector3 position, Dictionary<string, string> args, int line)
{
    public string Kind { get; } = kind;
    public Vector3 Position { get; } = position;
    public int Line { get; } = line;

    public bool Has(string key) => args.ContainsKey(key);
    public string Text(string key, string fallback) => args.GetValueOrDefault(key) ?? fallback;
    public float Float(string key, float fallback) => TextData.ParseFloat(args.GetValueOrDefault(key), fallback);
    public Vector3 Vector(string key, Vector3 fallback) => TextData.ParseVector(args.GetValueOrDefault(key), fallback, uniformAllowed: true);
    public bool Flag(string key) => Float(key, 0f) != 0f;

    /// <summary>Waypoints relative to <see cref="Position"/>: <c>path=x,y,z;x,y,z;...</c>.</summary>
    public List<Vector3> Path(string key)
    {
        var points = new List<Vector3>();
        if (args.GetValueOrDefault(key) is not { } text)
            return points;
        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
            points.Add(TextData.ParseVector(part));
        return points;
    }
}

/// <summary>
/// Level text format, one piece per line (<c>#</c> starts a comment):
/// <code>
/// kind  x y z  [key=value ...]
/// repeat N dx dy dz  kind x y z [key=value ...]   # N copies, each offset by (dx,dy,dz)
/// </code>
/// Coordinates are world units, the track runs toward -Z. The piece kinds are listed in the game README ("Level kit").
/// </summary>
public static class LevelFile
{
    public static List<PieceSpec> Load(string relativePath)
    {
        var path = AssetPaths.Resolve(relativePath);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[IslandDash] Level file not found: {path}");
            return [];
        }

        return Parse(File.ReadAllLines(path));
    }

    public static List<PieceSpec> Parse(IReadOnlyList<string> lines)
    {
        var pieces = new List<PieceSpec>();
        for (var i = 0; i < lines.Count; i++)
        {
            var tokens = TextData.Tokenize(lines[i]);
            if (tokens.Count == 0)
                continue;

            var count = 1;
            var step = Vector3.Zero;
            if (tokens[0].Equals("repeat", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Count < 9)
                {
                    Console.WriteLine($"[IslandDash] level:{i + 1}: 'repeat N dx dy dz kind x y z' expected");
                    continue;
                }

                count = (int)TextData.ParseFloat(tokens[1], 1f);
                step = new Vector3(Num(tokens[2]), Num(tokens[3]), Num(tokens[4]));
                tokens = tokens.GetRange(5, tokens.Count - 5);
            }

            if (tokens.Count < 4)
            {
                Console.WriteLine($"[IslandDash] level:{i + 1}: 'kind x y z' expected, got '{lines[i].Trim()}'");
                continue;
            }

            var origin = new Vector3(Num(tokens[1]), Num(tokens[2]), Num(tokens[3]));
            var args = TextData.ParseArgs(tokens, 4);
            for (var n = 0; n < count; n++)
                pieces.Add(new PieceSpec(tokens[0].ToLowerInvariant(), origin + step * n, args, i + 1));
        }

        return pieces;
    }

    private static float Num(string text) => TextData.ParseFloat(text, 0f);
}
