using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Project;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Scripting;

namespace Hounds.Marches;

[Register(typeof(IGameSystem))]
public sealed class LevelSystem(Context context, ISceneContext scenes) : IGameSystem
{
    public int Priority => 80;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        foreach (var (_, existing) in context.View<MarchGameComponent>())
            if (existing.Started)
                return;
        if (scenes.ActiveScene is not { } scene)
            return;

        var gameEntity = scene.CreateEntity("March");
        var game = gameEntity.AddComponent(new MarchGameComponent { Started = true });
        try
        {
            var root = PathBuilder.AssetsPath;
            OriginalAssetBaker.Ensure(root);
            game.Art = AssetPack.Select(root, Environment.GetCommandLineArgs());
            game.Pack = AssetPack.Load(root, game.Art);
            var level = LevelData.Load(Path.Combine(root, "levels", "marches.json"));
            level.Verify();
            Build(scene, game, level);
            game.Built = true;
            Console.WriteLine($"Hounds of the Marches — art pack '{game.Art}'.");
            Console.WriteLine("A/D move, Space jump, W/S ladder, J light, K heavy, L block, Shift dodge, E rest.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Level build failed: {ex}");
        }
    }

    private static void Build(IScene scene, MarchGameComponent game, LevelData level)
    {
        game.Rooms = level.Rooms.ToList();
        game.RespawnX = level.SpawnX;
        game.RespawnY = level.SpawnY;
        game.KillY = level.KillY;
        game.HelpTimer = 6f;
        game.Fade = 1f;

        var rows = level.Rows;
        for (var row = 0; row < rows.Length; row++)
        {
            var y = rows.Length - 1 - row;
            var line = rows[row];
            var x = 0;
            while (x < line.Length)
            {
                var ch = line[x];
                if (!level.Legend.TryGetValue(ch, out var key))
                {
                    x++;
                    continue;
                }

                var start = x;
                while (x < line.Length && line[x] == ch)
                    x++;
                Solid(scene, game.Pack!, start, y, x - start, 1f, key);
            }
        }

        Parallax(scene, game.Pack!, "far", 0.12f, 48f, 7.2f, 52f, 16f, -30f);
        Parallax(scene, game.Pack!, "mid", 0.38f, 48f, 6.4f, 48f, 14f, -20f);
        Parallax(scene, game.Pack!, "near", 0.62f, 48f, 5.2f, 46f, 12f, -10f);

        foreach (var ladder in level.Ladders)
            Zone(scene, game.Pack!, "ladder", "ladder", ladder.X, ladder.Y, ladder.W, ladder.H, 0.55f, ladder.H);
        foreach (var puddle in level.Puddles)
            Zone(scene, game.Pack!, "puddle", "puddle", puddle.X, puddle.Y, puddle.W, 0.45f, puddle.W, 0.45f);
        foreach (var prop in level.Props)
            PropSprite(scene, game.Pack!, prop.Kind, prop.X, prop.Y);
        for (var i = 0; i < level.Checkpoints.Length; i++)
            Shrine(scene, game.Pack!, level.Checkpoints[i], i);
        foreach (var pickup in level.Pickups)
            Pickup(scene, game.Pack!, pickup);
        foreach (var stone in level.Inscriptions)
            Inscription(scene, game.Pack!, stone);
        foreach (var actor in level.Actors)
            Actor(scene, game, actor.Kind, actor.X, actor.Y, actor.W);

        Actor(scene, game, "player", level.SpawnX, level.SpawnY, 0f);
        Bloodstain(scene, game.Pack!);
        Hud(scene, game.Pack!);
    }

    private static void Solid(IScene scene, AssetPack pack, float x, float y, float w, float h, string key)
    {
        var entity = scene.CreateEntity("ground");
        var transform = entity.AddComponent(new TransformComponent());
        transform.Translation = new Vector3(x + w * 0.5f, y + h * 0.5f, 0f);
        transform.Scale = new Vector3(w, h, 1f);
        var path = pack.Image(key);
        entity.AddComponent(new SpriteRendererComponent(path.Length == 0 ? March.Fallback(key) : Vector4.One)
        {
            TexturePath = path.Length == 0 ? null : path,
            TilingFactor = MathF.Max(1f, w),
            SortOrder = 8f
        });
        entity.AddComponent(new RigidBody2DComponent { BodyType = RigidBodyType.Static, FixedRotation = true });
        entity.AddComponent(new BoxCollider2DComponent
        {
            Size = new Vector2(0.5f, 0.5f),
            Friction = 0.95f,
            Restitution = 0f,
            Density = 0f
        });
    }

    private static void Parallax(IScene scene, AssetPack pack, string layer, float factor, float anchor, float y, float w, float h, float sort)
    {
        var key = "parallax_" + layer;
        var entity = Sprite(scene, "parallax-" + layer, anchor, y, w, h, pack.Image(key), sort, March.Fallback(key));
        entity.AddComponent(new PropComponent
        {
            Kind = "parallax",
            Factor = factor,
            Anchor = anchor,
            BaseY = y,
            ImageKey = key
        });
    }

    private static void Zone(IScene scene, AssetPack pack, string kind, string image, float x, float y, float w, float h, float spriteW, float spriteH)
    {
        var entity = Sprite(scene, kind, x + w * 0.5f, y + spriteH * 0.5f, spriteW, spriteH, pack.Image(image), 14f, March.Fallback(image));
        entity.AddComponent(new ZoneComponent { Kind = kind, W = w, H = h });
        if (entity.TryGetComponent<TransformComponent>(out var transform))
            transform.Translation = new Vector3(x + w * 0.5f, y + h * 0.5f, 0f);
    }

    private static void PropSprite(IScene scene, AssetPack pack, string kind, float x, float y)
    {
        var (w, h) = kind switch
        {
            "gate" => (2.4f, 3.4f),
            "banner" => (0.75f, 1.9f),
            "shield" => (0.7f, 0.95f),
            _ => (1f, 1f)
        };
        var entity = Sprite(scene, kind, x, y + h * 0.5f, w, h, pack.Image(kind), 18f, March.Fallback(kind));
        entity.AddComponent(new PropComponent { Kind = kind, ImageKey = kind });
    }

    private static void Shrine(IScene scene, AssetPack pack, LevelData.Spawn spawn, int index)
    {
        const float h = 1.7f;
        var entity = Sprite(scene, "shrine-" + spawn.Kind, spawn.X, spawn.Y + h * 0.5f, 1.15f, h, pack.Image("shrine"), 17f, March.Fallback("shrine"));
        entity.AddComponent(new CheckpointComponent { Id = spawn.Kind, Index = index });
    }

    private static void Pickup(IScene scene, AssetPack pack, LevelData.Spawn spawn)
    {
        var entity = Sprite(scene, "stingers", spawn.X, spawn.Y + 0.35f, 0.55f, 0.55f, pack.Image("coin"), 22f, new Vector4(0.85f, 0.7f, 0.3f, 1f));
        entity.AddComponent(new PropComponent { Kind = "pickup", Amount = spawn.Amount, ImageKey = "coin" });
    }

    private static void Inscription(IScene scene, AssetPack pack, LevelData.Spawn spawn)
    {
        var entity = Sprite(scene, "inscription", spawn.X, spawn.Y + 0.7f, 0.7f, 0.9f, pack.Image("banner"), 19f, March.Fallback("banner"));
        entity.AddComponent(new PropComponent { Kind = "inscription", ImageKey = spawn.Text });
    }

    private static void Bloodstain(IScene scene, AssetPack pack)
    {
        var entity = Sprite(scene, "Bloodstain", 0f, -20f, 0.9f, 0.45f, pack.Image("blood"), 21f, new Vector4(0.45f, 0.05f, 0.08f, 1f));
        entity.AddComponent(new PropComponent { Kind = "blood" });
        March.Show(entity, false);
    }

    private static void Hud(IScene scene, AssetPack pack)
    {
        var camera = March.Named(scene.Context, "Camera")
                     ?? throw new InvalidOperationException("Scene has no Camera entity.");

        Entity Child(string name, float x, float y, float w, float h, Vector4 color, float sort, string? texture)
        {
            var entity = scene.CreateEntity(name);
            var transform = entity.AddComponent(new TransformComponent());
            transform.Translation = new Vector3(x, y, 0f);
            transform.Scale = new Vector3(w, h, 1f);
            entity.AddComponent(new SpriteRendererComponent(color)
            {
                TexturePath = texture,
                SortOrder = sort
            });
            scene.SetParent(entity, camera);
            return entity;
        }

        Child("HpBack", -3.3f, 3.55f, 2.7f, 0.28f, new Vector4(0.08f, 0.07f, 0.08f, 0.92f), 2100f, null);
        Child("HpFill", -4.55f, 3.55f, 2.5f, 0.16f, new Vector4(0.72f, 0.16f, 0.14f, 1f), 2101f, null);
        Child("StamBack", -3.3f, 3.18f, 2.7f, 0.22f, new Vector4(0.08f, 0.07f, 0.08f, 0.92f), 2100f, null);
        Child("StamFill", -4.55f, 3.18f, 2.5f, 0.12f, new Vector4(0.78f, 0.62f, 0.28f, 1f), 2101f, null);
        var heart = pack.Image("heart");
        Child("Heart", -5.05f, 3.55f, 0.48f, 0.48f, Vector4.One, 2102f, heart.Length == 0 ? null : heart);
        var coin = pack.Image("coin");
        Child("Coin", 3.15f, 3.55f, 0.42f, 0.42f, Vector4.One, 2102f, coin.Length == 0 ? null : coin);
        for (var i = 0; i < 3; i++)
            Child("Digit" + i, 3.55f + i * 0.38f, 3.55f, 0.34f, 0.42f, Vector4.One, 2103f, null);
        Child("Banner", 0f, 2.45f, 6.8f, 0.62f, new Vector4(0, 0, 0, 0), 2200f, null);
        Child("Fade", 0f, 0f, 22f, 14f, new Vector4(0.02f, 0.02f, 0.03f, 1f), 3000f, null);
    }

    private static void Actor(IScene scene, MarchGameComponent game, string kind, float feetX, float feetY, float patrol)
    {
        var pack = game.Pack!;
        var (halfW, halfH) = SliceRules.Body(kind);
        var (spriteW, spriteH) = SliceRules.SpriteSize(pack.Id, kind);
        var body = scene.CreateEntity(kind == "player" ? "Player" : kind);
        var transform = body.AddComponent(new TransformComponent());
        transform.Translation = new Vector3(feetX, feetY + halfH, 0f);
        var actor = body.AddComponent(new ActorComponent
        {
            Kind = kind,
            Team = kind == "player" ? 0 : 1,
            HalfW = halfW,
            HalfH = halfH,
            SpawnX = feetX,
            SpawnY = feetY,
            Sheet = kind == "player" ? "player" : kind,
            StingerValue = SliceRules.StingersFor(kind)
        });
        var hp = kind switch
        {
            "boss" => 260f,
            "halberdier" => 110f,
            "swordsman" => 55f,
            _ => SliceRules.PlayerHp
        };
        var poise = kind switch
        {
            "boss" => 80f,
            "halberdier" => 50f,
            "swordsman" => 22f,
            _ => SliceRules.PlayerPoise
        };
        body.AddComponent(new HealthComponent { Current = hp, Max = hp, Poise = poise, PoiseMax = poise });
        body.AddComponent(new CombatComponent());
        if (kind == "player")
        {
            body.AddComponent(new StaminaComponent { Current = SliceRules.PlayerStamina, Max = SliceRules.PlayerStamina });
            body.AddComponent(new RigidBody2DComponent
            {
                BodyType = RigidBodyType.Dynamic,
                FixedRotation = true,
                GravityScale = 1f,
                IsBullet = true
            });
            body.AddComponent(new BoxCollider2DComponent
            {
                Size = new Vector2(halfW, halfH),
                Friction = 0f,
                Restitution = 0f,
                Density = 1f
            });
        }
        else
        {
            body.AddComponent(new AIBrainComponent
            {
                Left = feetX - patrol,
                Right = feetX + patrol,
                Aggro = kind == "boss" ? 9f : 7f,
                Leash = kind == "boss" ? 16f : 13f
            });
        }

        var visual = scene.CreateEntity(body.Name + ".sprite");
        var visualTransform = visual.AddComponent(new TransformComponent());
        visualTransform.Scale = new Vector3(spriteW, spriteH, 1f);
        var sheet = pack.Sheets.GetValueOrDefault(actor.Sheet);
        if (sheet != null && sheet.Get("idle").Length > 0)
        {
            visual.AddComponent(new SubTextureRendererComponent { Color = Vector4.One, SortOrder = 140f });
            visual.AddComponent(new Animator2DComponent { Sheet = actor.Sheet, Clip = "idle" });
        }
        else
        {
            visual.AddComponent(new SpriteRendererComponent(March.Fallback(kind)) { SortOrder = 140f });
        }

        scene.SetParent(visual, body);
        actor.VisualId = visual.Id;
    }

    private static Entity Sprite(IScene scene, string name, float x, float y, float w, float h, string path, float sort, Vector4 fallback)
    {
        var entity = scene.CreateEntity(name);
        var transform = entity.AddComponent(new TransformComponent());
        transform.Translation = new Vector3(x, y, 0f);
        transform.Scale = new Vector3(w, h, 1f);
        entity.AddComponent(new SpriteRendererComponent(path.Length == 0 ? fallback : Vector4.One)
        {
            TexturePath = path.Length == 0 ? null : path,
            SortOrder = sort
        });
        return entity;
    }
}
