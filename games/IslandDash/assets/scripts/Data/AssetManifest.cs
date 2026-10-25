using System.IO;
using SceneComponents.Rendering;

namespace IslandDash;

/// <summary>One logical model: file, normalization (scale + pivot) and material overrides.</summary>
public sealed record ModelEntry(
    string Name,
    string? Path,
    Vector3 Scale,
    Vector3 Pivot,
    Vector4 Tint,
    Vector3 Emissive,
    float EmissiveStrength,
    float Roughness)
{
    /// <summary>Primitive fallback when a name is missing from the manifest: bottom-anchored magenta unit cube.</summary>
    public static ModelEntry Placeholder(string name) =>
        new(name, null, Vector3.One, new Vector3(0f, 0.5f, 0f), new Vector4(1f, 0f, 1f, 1f), Vector3.Zero, 0f, 0.6f);

    public ModelRendererComponent CreateRenderer()
    {
        var renderer = new ModelRendererComponent();
        Apply(renderer);
        return renderer;
    }

    public void Apply(ModelRendererComponent renderer)
    {
        renderer.ModelPath = Path;
        renderer.Color = Tint;
        renderer.Emissive = Emissive;
        renderer.EmissiveStrength = EmissiveStrength;
        renderer.Roughness = Roughness;
        renderer.Metallic = 0f;
    }
}

/// <summary>
/// Logical model name → file path, scale and pivot offset, loaded from <c>assets/config/assets.manifest</c>.
/// Swapping an art pack means editing that file only; gameplay code never references model files directly.
/// </summary>
public sealed class AssetManifest
{
    public const string PrimitiveCube = "cube";

    private readonly Dictionary<string, ModelEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warned = new(StringComparer.OrdinalIgnoreCase);

    public static AssetManifest Load(string relativePath)
    {
        var path = AssetPaths.Resolve(relativePath);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[IslandDash] Asset manifest not found at {path}; every model falls back to a placeholder cube.");
            return new AssetManifest();
        }

        return Parse(File.ReadAllLines(path));
    }

    /// <summary>
    /// Line format: <c>name  path|cube  scale  pivot  [tint=r,g,b[,a]] [emissive=r,g,b,strength] [roughness=x]</c>.
    /// <c>scale</c> is one number or x,y,z; <c>pivot</c> is x,y,z in model units (added before scaling).
    /// </summary>
    public static AssetManifest Parse(IEnumerable<string> lines)
    {
        var manifest = new AssetManifest();
        var lineNumber = 0;
        foreach (var raw in lines)
        {
            lineNumber++;
            var tokens = TextData.Tokenize(raw);
            if (tokens.Count == 0)
                continue;
            if (tokens.Count < 4)
            {
                Console.WriteLine($"[IslandDash] assets.manifest:{lineNumber}: expected 'name path scale pivot', got '{raw.Trim()}'");
                continue;
            }

            var args = TextData.ParseArgs(tokens, 4);
            var path = tokens[1].Equals(PrimitiveCube, StringComparison.OrdinalIgnoreCase) ? null : tokens[1];
            var emissive = TextData.ParseFloats(args.GetValueOrDefault("emissive"), [0f, 0f, 0f, 0f]);
            manifest._entries[tokens[0]] = new ModelEntry(
                tokens[0],
                path,
                TextData.ParseVector(tokens[2], uniformAllowed: true),
                TextData.ParseVector(tokens[3]),
                TextData.ParseColor(args.GetValueOrDefault("tint"), Vector4.One),
                new Vector3(emissive[0], emissive[1], emissive[2]),
                emissive.Length > 3 ? emissive[3] : 1f,
                TextData.ParseFloat(args.GetValueOrDefault("roughness"), 0.75f));
        }

        return manifest;
    }

    public ModelEntry Get(string name)
    {
        if (_entries.TryGetValue(name, out var entry))
            return entry;

        if (_warned.Add(name))
            Console.WriteLine($"[IslandDash] Model '{name}' missing from assets.manifest; using a placeholder cube.");
        return ModelEntry.Placeholder(name);
    }
}
