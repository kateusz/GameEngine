using Hounds.Marches;

namespace Hounds.Tools;

public static class BakeProgram
{
    public static int Main(string[] args)
    {
        var root = ProjectRoot();
        var assets = Path.Combine(root, "assets");
        var check = args.Length == 0 || args[0] == "--check";
        var bake = args.Contains("--bake") || check;
        if (!check && !bake)
        {
            Console.WriteLine("Hounds of the Marches asset tool.");
            Console.WriteLine("  --bake    write original PNGs, titles, and placeholder WAVs");
            Console.WriteLine("  --check   bake if needed, then verify rules, level, and both packs");
            return 1;
        }

        if (bake)
            OriginalAssetBaker.Bake(assets);

        if (!check)
        {
            Console.WriteLine("Baked original pack under " + assets);
            return 0;
        }

        SliceRules.Verify();
        Melee.Verify();
        var level = LevelData.Load(Path.Combine(assets, "levels", "marches.json"));
        level.Verify();
        foreach (var id in new[] { "original", "external" })
        {
            var pack = AssetPack.Load(assets, id);
            if (pack.Image("mud").Length == 0 || pack.Image("stone").Length == 0)
                throw new InvalidOperationException($"Pack '{id}' is missing ground textures.");
            if (!pack.Sheets.TryGetValue("player", out var player) || player.Get("idle").Length == 0)
                throw new InvalidOperationException($"Pack '{id}' has no player idle frame.");
            foreach (var clip in new[] { "run", "lightWindup", "heavyWindup", "dodge", "block", "windup" })
                if (player.Get(clip).Length == 0)
                    throw new InvalidOperationException($"Pack '{id}' player is missing clip '{clip}'.");
            AssertPng(assets, pack.Image("parallax_far"));
            AssertPng(assets, pack.Image("heart"));
        }

        var selected = AssetPack.Select(assets, args);
        Console.WriteLine($"Hounds check ok. --art selects '{selected}' (default original).");
        return 0;
    }

    private static void AssertPng(string assets, string relative)
    {
        var path = Path.Combine(assets, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            throw new FileNotFoundException("Missing png", path);
        using var fs = File.OpenRead(path);
        Span<byte> sig = stackalloc byte[8];
        if (fs.Read(sig) < 8 || sig[0] != 0x89 || sig[1] != (byte)'P')
            throw new InvalidDataException("Not a png: " + relative);
    }

    private static string ProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "game.config.json")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("game.config.json not found from " + AppContext.BaseDirectory);
    }
}
