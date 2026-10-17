using System.Numerics;
using ECS;
using IslandDash;
using SceneComponents;

// One runnable check for the collision sweep and for level 1. No test framework:
//   dotnet run --project games/IslandDash/tests/SelfCheck
var failed = 0;

var root = FindRoot();
var pieces = LevelFile.Parse(File.ReadAllLines(Path.Combine(root, "assets", "levels", "level1.lvl")));
Check(pieces.Count > 40, $"level parsed only {pieces.Count} pieces");

foreach (var piece in pieces)
    Check(LevelFile.Kinds.Contains(piece.Kind), $"level:{piece.Line} unknown kind '{piece.Kind}'");

foreach (var kind in new[]
         {
             "start", "goal", "kill_plane", "mover", "vanish", "faller", "crusher",
             "spikes", "enemy", "crate_life", "crate_checkpoint"
         })
    Check(pieces.Exists(p => p.Kind == kind), $"level is missing a '{kind}' piece");

var crystals = pieces.Count(p => p.Kind == "crystal");
foreach (var crate in pieces.Where(p => p.Kind == "crate"))
    crystals += (int)crate.Float("crystals", 2f);
Check(crystals >= 100, $"level only contains {crystals} crystals; 100 should be reachable");
Console.WriteLine($"level: {pieces.Count} pieces, {crystals} crystals");

if (pieces.FirstOrDefault(p => p.Kind == "start") is { } start)
    Check(pieces.Where(p => p.Kind == "ground").Any(g => Covers(g, start.Position)), "start is not standing on a ground piece");
if (pieces.FirstOrDefault(p => p.Kind == "goal") is { } goal)
    Check(Landings().Any(l => Covers(l, goal.Position)), "goal is not standing on a landing");

var path = Landings().Where(p => MathF.Abs(p.Position.X) <= 2.2f).OrderByDescending(p => p.Position.Z).ToList();
for (var i = 1; i < path.Count; i++)
{
    var gap = Leading(path[i - 1]) - Trailing(path[i]);
    Check(gap <= 5f,
        $"gap of {gap:0.00} m between {path[i - 1].Kind} z {path[i - 1].Position.Z:0.0} and {path[i].Kind} z {path[i].Position.Z:0.0}");
}

CheckSweep();
CheckManifest();

if (failed > 0)
{
    Console.Error.WriteLine($"{failed} check(s) failed");
    return 1;
}

Console.WriteLine("SelfCheck ok");
return 0;

void Check(bool condition, string message)
{
    if (condition)
        return;
    failed++;
    Console.Error.WriteLine("FAIL " + message);
}

string FindRoot()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "game.config.json")))
            return dir.FullName;
    }

    throw new InvalidOperationException("game.config.json not found above the test output");
}

IEnumerable<PieceSpec> Landings() =>
    pieces.Where(p => p.Kind is "ground" or "block" or "platform" or "mover" or "vanish" or "faller");

static Vector3 Half(PieceSpec piece)
{
    var size = piece.Vector("size", Vector3.One);
    var amp = piece.Vector("amp", Vector3.Zero);
    return new Vector3(size.X * 0.5f + MathF.Abs(amp.X), size.Y * 0.5f, size.Z * 0.5f + MathF.Abs(amp.Z));
}

static bool Covers(PieceSpec landing, Vector3 point)
{
    var half = Half(landing);
    return MathF.Abs(point.X - landing.Position.X) <= half.X + 0.4f
           && MathF.Abs(point.Z - landing.Position.Z) <= half.Z + 0.4f;
}

static float Leading(PieceSpec piece) => piece.Position.Z - Half(piece).Z;
static float Trailing(PieceSpec piece) => piece.Position.Z + Half(piece).Z;

void CheckSweep()
{
    var context = new Context();
    var wall = new Entity(1, "Wall");
    wall.AddComponent(new TransformComponent(new Vector3(2f, 1f, 0f), Vector3.Zero, Vector3.One));
    wall.AddComponent(new ColliderComponent { HalfExtents = new Vector3(0.5f, 1f, 2f) });
    context.Register(wall);

    var box = Box.FromCenter(new Vector3(0f, 1f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
    var allowed = Collision.SweepAxis(context, box, axis: 0, delta: 10f, ignoreEntityId: -1, out var hit);
    Check(MathF.Abs(allowed - 1f) < 0.001f, $"horizontal sweep moved {allowed}, expected 1 (no tunneling)");
    Check(hit != null, "horizontal sweep reported no hit");

    var platform = new Entity(2, "Platform");
    platform.AddComponent(new TransformComponent(new Vector3(0f, -0.1f, 0f), Vector3.Zero, Vector3.One));
    platform.AddComponent(new ColliderComponent { HalfExtents = new Vector3(2f, 0.1f, 2f) });
    context.Register(platform);

    var falling = Box.FromCenter(new Vector3(0f, 3f, 0f), new Vector3(0.4f));
    var dropped = Collision.SweepAxis(context, falling, axis: 1, delta: -20f, ignoreEntityId: -1, out var groundHit);
    Check(groundHit != null && MathF.Abs(falling.Min.Y + dropped) < 0.001f,
        $"a 20 m drop passed a thin platform (moved {dropped})");
}

void CheckManifest()
{
    var manifest = AssetManifest.Parse(
    [
        "# comment",
        "hero  cube  2  0,0.5,0  tint=1,0,0  emissive=0,1,0,3  roughness=0.2",
        "bad line"
    ]);
    var hero = manifest.Get("hero");
    Check(hero.Path == null, "a 'cube' manifest entry should draw a primitive");
    Check(hero.Scale == new Vector3(2f), $"scale parsed as {hero.Scale}");
    Check(hero.Pivot.Y == 0.5f, "pivot y");
    Check(MathF.Abs(hero.EmissiveStrength - 3f) < 0.001f, "emissive strength");
    var missing = manifest.Get("nope");
    Check(missing.Tint is { X: 1f, Y: 0f, Z: 1f }, "missing names fall back to a magenta placeholder");
}
