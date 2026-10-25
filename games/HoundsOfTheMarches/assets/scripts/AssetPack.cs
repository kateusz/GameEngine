using System.IO;
using System.Text.Json;

namespace Hounds.Marches;

public sealed class SpriteFrame
{
    public string Texture { get; init; } = "";
    public float U0 { get; init; }
    public float U1 { get; init; }
    public float VBottom { get; init; }
    public float VTop { get; init; }
}

public sealed class SpriteSheet
{
    public Dictionary<string, SpriteFrame[]> Clips { get; } = new(StringComparer.Ordinal);

    public SpriteFrame[] Get(string clip)
    {
        if (Clips.TryGetValue(clip, out var frames) && frames.Length > 0)
            return frames;
        if (Clips.TryGetValue("idle", out var idle))
            return idle;
        return [];
    }
}

/// <summary>
/// One art pack. Paths are relative to the project <c>assets/</c> directory.
/// Missing files fall back to the original pack when this pack is external, and never throw.
/// </summary>
public sealed class AssetPack
{
    public string Id { get; init; } = "original";
    public Dictionary<string, SpriteSheet> Sheets { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Images { get; } = new(StringComparer.Ordinal);
    private readonly string _root;
    private readonly AssetPack? _fallback;

    private AssetPack(string root, AssetPack? fallback)
    {
        _root = root;
        _fallback = fallback;
    }

    public string Image(string key)
    {
        if (!Images.TryGetValue(key, out var path) || path.Length == 0)
            return _fallback?.Image(key) ?? "";
        if (File.Exists(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar))))
            return path;
        var alt = _fallback?.Image(key) ?? "";
        return alt;
    }

    public static string Select(string assetsRoot, IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (args[i] == "--art" && i + 1 < args.Count)
                return args[i + 1].Equals("external", StringComparison.OrdinalIgnoreCase) ? "external" : "original";
            if (args[i].StartsWith("--art=", StringComparison.Ordinal))
            {
                var value = args[i]["--art=".Length..];
                return value.Equals("external", StringComparison.OrdinalIgnoreCase) ? "external" : "original";
            }
        }

        var file = Path.Combine(assetsRoot, "artpack.txt");
        if (File.Exists(file))
        {
            var text = File.ReadAllText(file).Trim();
            if (text.Equals("external", StringComparison.OrdinalIgnoreCase))
                return "external";
        }

        return "original";
    }

    public static AssetPack Load(string assetsRoot, string id)
    {
        AssetPack? fallback = null;
        if (!id.Equals("original", StringComparison.OrdinalIgnoreCase))
            fallback = TryLoad(assetsRoot, "original", null);
        return TryLoad(assetsRoot, id, fallback) ?? fallback ?? Empty(assetsRoot, id);
    }

    private static AssetPack Empty(string root, string id) => new(root, null) { Id = id };

    private static AssetPack? TryLoad(string assetsRoot, string id, AssetPack? fallback)
    {
        var manifest = Path.Combine(assetsRoot, id.Equals("external", StringComparison.OrdinalIgnoreCase) ? "External" : "Original", "pack.json");
        if (!File.Exists(manifest))
            return fallback;

        var pack = new AssetPack(assetsRoot, fallback) { Id = id };
        using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
        var root = doc.RootElement;
        if (root.TryGetProperty("images", out var images))
        {
            foreach (var prop in images.EnumerateObject())
                pack.Images[prop.Name] = prop.Value.GetString() ?? "";
        }

        if (root.TryGetProperty("sheets", out var sheets))
        {
            foreach (var prop in sheets.EnumerateObject())
            {
                var rel = prop.Value.GetString() ?? "";
                var sheet = LoadSheet(assetsRoot, rel, fallback?.Sheets.GetValueOrDefault(prop.Name));
                pack.Sheets[prop.Name] = sheet;
            }
        }

        return pack;
    }

    private static SpriteSheet LoadSheet(string assetsRoot, string relative, SpriteSheet? fallback)
    {
        var path = Path.Combine(assetsRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return fallback ?? new SpriteSheet();

        var sheet = new SpriteSheet();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("animations", out var anims))
            return fallback ?? sheet;

        foreach (var clip in anims.EnumerateObject())
        {
            var frames = new List<SpriteFrame>();
            foreach (var frameEl in clip.Value.EnumerateArray())
            {
                var texture = frameEl.TryGetProperty("texture", out var tex) ? tex.GetString() ?? "" : "";
                if (texture.Length == 0)
                    continue;
                var full = Path.Combine(assetsRoot, texture.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full))
                    continue;
                var (w, h) = PngSize(full);
                if (w <= 0 || h <= 0)
                    continue;
                var x = frameEl.TryGetProperty("x", out var xs) ? xs.GetInt32() : 0;
                var y = frameEl.TryGetProperty("y", out var ys) ? ys.GetInt32() : 0;
                var fw = frameEl.TryGetProperty("w", out var ws) ? ws.GetInt32() : w;
                var fh = frameEl.TryGetProperty("h", out var hs) ? hs.GetInt32() : h;
                var u0 = x / (float)w;
                var u1 = (x + fw) / (float)w;
                var vTop = 1f - y / (float)h;
                var vBottom = 1f - (y + fh) / (float)h;
                frames.Add(new SpriteFrame { Texture = texture, U0 = u0, U1 = u1, VTop = vTop, VBottom = vBottom });
            }

            if (frames.Count == 0 && fallback != null && fallback.Clips.TryGetValue(clip.Name, out var alt))
                frames.AddRange(alt);
            sheet.Clips[clip.Name] = frames.ToArray();
        }

        return sheet;
    }

    public static (int W, int H) PngSize(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var fs = File.OpenRead(path);
        if (fs.Read(header) < 24 || header[0] != 0x89)
            return (0, 0);
        var w = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        var h = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        return (w, h);
    }
}
