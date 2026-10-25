using System.IO;
using System.Text.Json;

namespace Hounds.Marches;

public sealed class LevelData
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

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
        var raw = JsonSerializer.Deserialize<LevelFile>(File.ReadAllText(path), JsonOptions)
                  ?? throw new InvalidOperationException("Empty level: " + path);
        var legend = new Dictionary<char, string>();
        foreach (var (key, value) in raw.Legend)
            if (key.Length > 0)
                legend[key[0]] = value;

        return new LevelData
        {
            KillY = raw.KillY,
            SpawnX = raw.Spawn.X,
            SpawnY = raw.Spawn.Y,
            Rows = raw.Rows,
            Legend = legend,
            Rooms = raw.Rooms,
            Actors = Map(raw.Actors),
            Pickups = Map(raw.Pickups),
            Inscriptions = Map(raw.Inscriptions),
            Checkpoints = Map(raw.Checkpoints),
            Ladders = Map(raw.Ladders),
            Puddles = Map(raw.Puddles),
            Props = Map(raw.Props)
        };
    }

    public void Verify()
    {
        if (Rooms.Length < 3)
            throw new InvalidOperationException("Level needs village, bridge, and courtyard.");
        var kinds = Actors.Select(a => a.Kind).ToHashSet(StringComparer.Ordinal);
        foreach (var need in new[] { "swordsman", "halberdier", "boss" })
            if (!kinds.Contains(need))
                throw new InvalidOperationException($"Level is missing actor '{need}'.");
        if (Checkpoints.Length < 2)
            throw new InvalidOperationException("Level needs two shrines.");
        if (Rows.Length == 0 || Rows[0].Length < 64)
            throw new InvalidOperationException("Level row is too short for a multi-screen march.");
    }

    private static Spawn[] Map(SpawnFile[] items) =>
        items.Select(s => new Spawn(s.Kind, s.X, s.Y, s.W, s.H, s.Amount, s.Text)).ToArray();

    private sealed class LevelFile
    {
        public float KillY { get; set; } = -3f;
        public SpawnPoint Spawn { get; set; } = new();
        public Dictionary<string, string> Legend { get; set; } = new();
        public string[] Rows { get; set; } = [];
        public RoomInfo[] Rooms { get; set; } = [];
        public SpawnFile[] Actors { get; set; } = [];
        public SpawnFile[] Pickups { get; set; } = [];
        public SpawnFile[] Inscriptions { get; set; } = [];
        public SpawnFile[] Checkpoints { get; set; } = [];
        public SpawnFile[] Ladders { get; set; } = [];
        public SpawnFile[] Puddles { get; set; } = [];
        public SpawnFile[] Props { get; set; } = [];
    }

    private sealed class SpawnPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    private sealed class SpawnFile
    {
        public string Kind { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float W { get; set; } = 1f;
        public float H { get; set; } = 1f;
        public int Amount { get; set; }
        public string Text { get; set; } = "";
    }
}
