using System.IO;
using System.Text.Json;

namespace Hounds.Marches;

public sealed class SpriteFrame
{
    public string Texture { get; init; } = "";
    public float U0 { get; init; }
    public float U1 { get; init; } = 1f;
    public float VBottom { get; init; }
    public float VTop { get; init; } = 1f;
}

public sealed class SpriteSheet
{
    public Dictionary<string, SpriteFrame[]> Clips { get; } = new(StringComparer.Ordinal);

    public SpriteFrame[] Get(string clip)
    {
        if (Clips.TryGetValue(clip, out var frames) && frames.Length > 0)
            return frames;
        return Clips.TryGetValue("idle", out var idle) ? idle : [];
    }
}

/// <summary>
/// One art pack. Paths are relative to the project <c>assets/</c> directory.
/// </summary>
public sealed class AssetPack
{
    public string Id { get; init; } = "original";
    public Dictionary<string, SpriteSheet> Sheets { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Images { get; } = new(StringComparer.Ordinal);
    private readonly string _root;

    private AssetPack(string root) => _root = root;

    public string Image(string key)
    {
        if (!Images.TryGetValue(key, out var path) || path.Length == 0)
            return "";
        return File.Exists(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar))) ? path : "";
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
        if (File.Exists(file) && File.ReadAllText(file).Trim().Equals("external", StringComparison.OrdinalIgnoreCase))
            return "external";
        return "original";
    }

    public static AssetPack Load(string assetsRoot, string id)
    {
        var folder = id.Equals("external", StringComparison.OrdinalIgnoreCase) ? "External" : "Original";
        var manifest = Path.Combine(assetsRoot, folder, "pack.json");
        if (!File.Exists(manifest))
            return new AssetPack(assetsRoot) { Id = id };

        var pack = new AssetPack(assetsRoot) { Id = id };
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
                pack.Sheets[prop.Name] = LoadSheet(assetsRoot, prop.Value.GetString() ?? "");
        }

        return pack;
    }

    private static SpriteSheet LoadSheet(string assetsRoot, string relative)
    {
        var sheet = new SpriteSheet();
        if (relative.Length == 0)
            return sheet;
        var path = Path.Combine(assetsRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return sheet;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("animations", out var anims))
            return sheet;

        foreach (var clip in anims.EnumerateObject())
        {
            var frames = new List<SpriteFrame>();
            foreach (var frameEl in clip.Value.EnumerateArray())
            {
                var texture = frameEl.TryGetProperty("texture", out var tex) ? tex.GetString() ?? "" : "";
                if (texture.Length == 0)
                    continue;
                // ponytail: UV baked into JSON; missing fields = full texture (external pack).
                frames.Add(new SpriteFrame
                {
                    Texture = texture,
                    U0 = frameEl.TryGetProperty("u0", out var u0) ? u0.GetSingle() : 0f,
                    U1 = frameEl.TryGetProperty("u1", out var u1) ? u1.GetSingle() : 1f,
                    VBottom = frameEl.TryGetProperty("vBottom", out var vb) ? vb.GetSingle() : 0f,
                    VTop = frameEl.TryGetProperty("vTop", out var vt) ? vt.GetSingle() : 1f
                });
            }

            sheet.Clips[clip.Name] = frames.ToArray();
        }

        return sheet;
    }
}
