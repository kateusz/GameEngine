using System.Text.Json;

namespace Hounds.Marches;

public sealed class LevelData
{
    public float KillY { get; init; }
    public float SpawnX { get; init; }
    public float SpawnY { get; init; }
    public string[] Rows { get; init; } = [];
    public Dictionary<char, string> Legend { get; init; } = new();
    public RoomInfo[] Rooms { get; init; } = [];
    public Spawn[] Actors { get; init; } = [];
    public Spawn[] Pickups { get; init; } = [];
    public Spawn[] Inscriptions { get; init; } = [];
    public Spawn[] Checkpoints { get; init; } = [];
    public Spawn[] Ladders { get; init; } = [];
    public Spawn[] Puddles { get; init; } = [];
    public Spawn[] Props { get; init; } = [];

    public readonly record struct Spawn(string Kind, float X, float Y, float W, float H, int Amount, string Text);

    public static LevelData Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var legend = new Dictionary<char, string>();
        if (root.TryGetProperty("legend", out var legendEl))
        {
            foreach (var prop in legendEl.EnumerateObject())
                if (prop.Name.Length > 0)
                    legend[prop.Name[0]] = prop.Value.GetString() ?? "";
        }

        return new LevelData
        {
            KillY = Num(root, "killY", -3f),
            SpawnX = root.GetProperty("spawn").GetProperty("x").GetSingle(),
            SpawnY = root.GetProperty("spawn").GetProperty("y").GetSingle(),
            Rows = Strings(root, "rows"),
            Legend = legend,
            Rooms = RoomsOf(root),
            Actors = Spawns(root, "actors"),
            Pickups = Spawns(root, "pickups"),
            Inscriptions = Spawns(root, "inscriptions"),
            Checkpoints = Spawns(root, "checkpoints"),
            Ladders = Spawns(root, "ladders"),
            Puddles = Spawns(root, "puddles"),
            Props = Spawns(root, "props")
        };
    }

    public void Verify()
    {
        if (Rooms.Length < 3)
            throw new InvalidOperationException("Level needs village, bridge, and courtyard.");
        if (Rooms.Length - 1 < 2)
            throw new InvalidOperationException("Level needs two room transitions.");
        var kinds = Actors.Select(a => a.Kind).ToHashSet(StringComparer.Ordinal);
        foreach (var need in new[] { "swordsman", "halberdier", "boss" })
            if (!kinds.Contains(need))
                throw new InvalidOperationException($"Level is missing actor '{need}'.");
        if (Checkpoints.Length < 2)
            throw new InvalidOperationException("Level needs two shrines.");
        if (Rows.Length == 0 || Rows[0].Length < 64)
            throw new InvalidOperationException("Level row is too short for a multi-screen march.");
    }

    private static RoomInfo[] RoomsOf(JsonElement root)
    {
        if (!root.TryGetProperty("rooms", out var arr))
            return [];
        var list = new List<RoomInfo>();
        foreach (var el in arr.EnumerateArray())
            list.Add(new RoomInfo
            {
                Id = el.GetProperty("id").GetString() ?? "",
                MinX = el.GetProperty("minX").GetSingle(),
                MaxX = el.GetProperty("maxX").GetSingle(),
                CamMin = el.GetProperty("camMin").GetSingle(),
                CamMax = el.GetProperty("camMax").GetSingle(),
                CamY = el.GetProperty("camY").GetSingle(),
                Title = el.GetProperty("title").GetString() ?? ""
            });
        return list.ToArray();
    }

    private static Spawn[] Spawns(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr))
            return [];
        var list = new List<Spawn>();
        foreach (var el in arr.EnumerateArray())
            list.Add(new Spawn(
                el.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "",
                el.TryGetProperty("x", out var x) ? x.GetSingle() : 0,
                el.TryGetProperty("y", out var y) ? y.GetSingle() : 0,
                el.TryGetProperty("w", out var w) ? w.GetSingle() : 1,
                el.TryGetProperty("h", out var h) ? h.GetSingle() : 1,
                el.TryGetProperty("amount", out var a) ? a.GetInt32() : 0,
                el.TryGetProperty("text", out var t) ? t.GetString() ?? "" : ""));
        return list.ToArray();
    }

    private static string[] Strings(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr))
            return [];
        return arr.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
    }

    private static float Num(JsonElement root, string name, float fallback) =>
        root.TryGetProperty(name, out var el) ? el.GetSingle() : fallback;
}
