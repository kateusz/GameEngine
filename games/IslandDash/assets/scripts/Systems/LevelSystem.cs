using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Lighting;
using Scripting;

namespace IslandDash;

/// <summary>
/// Builds the level from the kit pieces listed in the level file (see <see cref="LevelFile"/>) and rebuilds it
/// on restart. Each piece becomes a gameplay entity (transform + collider + rules component) plus a visual entity.
/// </summary>
[Register(typeof(IGameSystem))]
public class LevelSystem(Context context) : IGameSystem
{
    // Kit unit sizes. The asset manifest scales every model so it matches these (see assets/config/assets.manifest).
    private const float GroundTileSize = 2f;
    private const float SpikeTileSize = 1f;
    private static readonly Vector3 CrateHalfExtents = new(0.5f);
    private static readonly Vector3 EnemyHalfExtents = new(0.42f, 0.55f, 0.42f);
    private static readonly Vector3 GoalHalfExtents = new(1.1f, 1.6f, 1.1f);
    private const float CrystalRadius = 0.55f;
    private const float SpikeHazardHeight = 0.35f;
    private const float SpikeHazardInset = 0.12f;
    private const float CrusherHazardScale = 0.46f;
    private const float GoalGemHeight = 3.3f;

    public int Priority => SystemOrder.Level;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.GameEntity(context) is not { } game)
            return;

        if (!game.TryGetComponent<GameStateComponent>(out var state))
        {
            var settings = game.GetComponent<GameSettingsComponent>();
            state = game.AddComponent(new GameStateComponent
            {
                Phase = settings.ShowTitleScreen ? GamePhase.Title : GamePhase.Playing
            });
        }

        if (state.RebuildRequested)
            Rebuild(game, state);
    }

    private void Rebuild(Entity game, GameStateComponent state)
    {
        var settings = game.GetComponent<GameSettingsComponent>();
        var tuning = World.Get<PlayerTuningComponent>(context);

        Teardown();

        state.RebuildRequested = false;
        state.CameraSnap = true;
        state.Lives = settings.StartLives;
        state.Crystals = 0;
        state.TotalCrystalsCollected = 0;
        state.CratesBroken = 0;
        state.LevelTime = 0f;
        state.KillY = settings.DefaultKillY;
        state.CrystalsInLevel = 0;
        state.CratesInLevel = 0;

        var manifest = AssetManifest.Load(settings.ManifestPath);
        var pieces = LevelFile.Load(settings.LevelPath);
        var builder = new Builder(context, manifest, state, tuning);
        foreach (var piece in pieces)
            builder.Spawn(piece);

        Console.WriteLine($"[IslandDash] Level '{settings.LevelPath}' built: {pieces.Count} pieces, " +
                          $"{state.CrystalsInLevel} crystals, {state.CratesInLevel} crates.");
    }

    private void Teardown()
    {
        var ids = new List<int>();
        foreach (var (entity, _) in context.View<LevelEntityComponent>())
            ids.Add(entity.Id);
        foreach (var id in ids)
            context.Remove(id);
    }

    private sealed class Builder(Context context, AssetManifest manifest, GameStateComponent state, PlayerTuningComponent tuning)
    {
        public void Spawn(PieceSpec p)
        {
            switch (p.Kind)
            {
                case "start": Start(p); break;
                case "kill_plane": state.KillY = p.Position.Y; break;
                case "ground": Ground(p); break;
                case "block": SolidStatic(p, p.Text("model", "block")); break;
                case "platform": SolidStatic(p, p.Text("model", "platform")); break;
                case "mover": Mover(p); break;
                case "vanish": Vanish(p); break;
                case "faller": Faller(p); break;
                case "crate": Crate(p, CrateKind.Normal, "crate", "crystal"); break;
                case "crate_life": Crate(p, CrateKind.ExtraLife, "crate_life", "heart"); break;
                case "crate_checkpoint": Crate(p, CrateKind.Checkpoint, "crate_checkpoint", "checkpoint_flag"); break;
                case "crystal": Crystal(p); break;
                case "spikes": Spikes(p); break;
                case "crusher": Crusher(p); break;
                case "enemy": Enemy(p); break;
                case "goal": Goal(p); break;
                case "deco": Deco(p); break;
                case "camzone": CameraZone(p); break;
                case "light": Light(p); break;
                default:
                    Console.WriteLine($"[IslandDash] level:{p.Line}: unknown piece '{p.Kind}' skipped");
                    break;
            }
        }

        private void Start(PieceSpec p)
        {
            state.Checkpoint = p.Position;
            var player = Spawner.Create(context, "Player", p.Position);
            player.AddComponent(new PlayerComponent());
            player.AddComponent(new ColliderComponent
            {
                HalfExtents = tuning.BodyHalfExtents,
                Offset = new Vector3(0f, tuning.BodyHalfExtents.Y, 0f),
                Solid = false,
                BlocksCamera = false
            });
            const float facingDownTheTrack = MathF.PI;
            Spawner.AttachAnimatedModel(context, player, manifest.Get("player"), AnimationDriverKind.Player, Vector3.One, facingDownTheTrack);
        }

        /// <summary>Position = top-center. Tiled grass blocks for visuals, one box collider.</summary>
        private void Ground(PieceSpec p)
        {
            var size = p.Vector("size", new Vector3(4f, 1f, 4f));
            var model = manifest.Get(p.Text("model", "ground_tile"));
            CreateSolid("Ground", p.Position, size);

            var nx = System.Math.Max(1, (int)MathF.Round(size.X / GroundTileSize));
            var nz = System.Math.Max(1, (int)MathF.Round(size.Z / GroundTileSize));
            var tile = new Vector3(size.X / nx, size.Y, size.Z / nz);
            var tileScale = new Vector3(tile.X / GroundTileSize, tile.Y, tile.Z / GroundTileSize);
            var corner = p.Position - new Vector3(size.X * 0.5f, size.Y, size.Z * 0.5f);
            for (var ix = 0; ix < nx; ix++)
            for (var iz = 0; iz < nz; iz++)
            {
                var position = corner + new Vector3((ix + 0.5f) * tile.X, 0f, (iz + 0.5f) * tile.Z);
                Spawner.CreateModel(context, "GroundTile", model, position, tileScale);
            }
        }

        /// <summary>Position = top-center; collider + a static model (no animator).</summary>
        private void SolidStatic(PieceSpec p, string modelName)
        {
            var size = p.Vector("size", Vector3.One);
            CreateSolid(modelName, p.Position, size);
            Spawner.CreateModel(context, modelName, manifest.Get(modelName), p.Position - new Vector3(0f, size.Y, 0f), size);
        }

        /// <summary>Position = top-center; collider + animated visual (movers / vanish / fallers).</summary>
        private Entity Solid(PieceSpec p, string modelName)
        {
            var size = p.Vector("size", Vector3.One);
            var entity = CreateSolid(modelName, p.Position, size);
            Spawner.AttachAnimatedModel(context, entity, manifest.Get(modelName), AnimationDriverKind.Platform, size,
                ownerToModelBase: new Vector3(0f, -size.Y, 0f));
            return entity;
        }

        private Entity CreateSolid(string name, Vector3 top, Vector3 size)
        {
            var entity = Spawner.Create(context, name, top);
            entity.AddComponent(new ColliderComponent { HalfExtents = size * 0.5f, Offset = new Vector3(0f, -size.Y * 0.5f, 0f) });
            return entity;
        }

        private void Mover(PieceSpec p)
        {
            var entity = Solid(p, p.Text("model", "mover"));
            entity.AddComponent(new MovingPlatformComponent
            {
                Origin = p.Position,
                Amplitude = p.Vector("amp", Vector3.Zero),
                Period = p.Float("period", 4f),
                Phase = p.Float("phase", 0f)
            });
        }

        private void Vanish(PieceSpec p)
        {
            var entity = Solid(p, p.Text("model", "vanish"));
            entity.AddComponent(new VanishingPlatformComponent
            {
                OnTime = p.Float("on", 2.5f),
                OffTime = p.Float("off", 1.5f),
                Phase = p.Float("phase", 0f)
            });
        }

        private void Faller(PieceSpec p)
        {
            var entity = Solid(p, p.Text("model", "faller"));
            entity.AddComponent(new FallingPlatformComponent
            {
                Origin = p.Position,
                ShakeTime = p.Float("delay", 0.55f),
                RespawnTime = p.Float("respawn", 3.5f)
            });
        }

        /// <summary>Position = bottom-center (crates sit on the surface below).</summary>
        private void Crate(PieceSpec p, CrateKind kind, string modelName, string rewardName)
        {
            var entity = Spawner.Create(context, modelName, p.Position);
            entity.AddComponent(new ColliderComponent { HalfExtents = CrateHalfExtents, Offset = new Vector3(0f, CrateHalfExtents.Y, 0f) });
            var crystals = (int)p.Float("crystals", 2f);
            entity.AddComponent(new CrateComponent { Kind = kind, Crystals = crystals, Reward = manifest.Get(rewardName) });
            Spawner.AttachAnimatedModel(context, entity, manifest.Get(modelName), AnimationDriverKind.Pop, Vector3.One, Yaw(p, 0f));
            state.CratesInLevel++;
            if (kind == CrateKind.Normal)
                state.CrystalsInLevel += crystals;
        }

        private void Crystal(PieceSpec p)
        {
            var entity = Spawner.Create(context, "Crystal", p.Position);
            entity.AddComponent(new CollectibleComponent { Radius = CrystalRadius });
            Spawner.AttachAnimatedModel(context, entity, manifest.Get("crystal"), AnimationDriverKind.Collectible, Vector3.One);
            state.CrystalsInLevel++;
        }

        /// <summary>Position = center of the spike field on the floor; <c>size=w,1,d</c> in 1 m tiles. <c>up=/down=</c> makes them pop up on a cycle.</summary>
        private void Spikes(PieceSpec p)
        {
            var area = p.Vector("size", Vector3.One);
            var nx = System.Math.Max(1, (int)MathF.Round(area.X / SpikeTileSize));
            var nz = System.Math.Max(1, (int)MathF.Round(area.Z / SpikeTileSize));
            var model = manifest.Get("spikes");
            var timed = p.Has("up");
            var corner = p.Position - new Vector3(nx * SpikeTileSize * 0.5f, 0f, nz * SpikeTileSize * 0.5f);

            if (!timed)
            {
                var hazard = Spawner.Create(context, "Spikes", p.Position);
                hazard.AddComponent(new HazardComponent());
                hazard.AddComponent(new ColliderComponent
                {
                    HalfExtents = new Vector3(nx * SpikeTileSize * 0.5f - SpikeHazardInset, SpikeHazardHeight * 0.5f, nz * SpikeTileSize * 0.5f - SpikeHazardInset),
                    Offset = new Vector3(0f, SpikeHazardHeight * 0.5f, 0f),
                    Solid = false,
                    BlocksCamera = false
                });
            }

            for (var ix = 0; ix < nx; ix++)
            for (var iz = 0; iz < nz; iz++)
            {
                var position = corner + new Vector3((ix + 0.5f) * SpikeTileSize, 0f, (iz + 0.5f) * SpikeTileSize);
                if (!timed)
                {
                    Spawner.CreateModel(context, "SpikeTile", model, position, Vector3.One);
                    continue;
                }

                var tile = Spawner.Create(context, "TimedSpikes", position);
                tile.AddComponent(new HazardComponent());
                tile.AddComponent(new ColliderComponent
                {
                    HalfExtents = new Vector3(SpikeTileSize * 0.5f - SpikeHazardInset, SpikeHazardHeight * 0.5f, SpikeTileSize * 0.5f - SpikeHazardInset),
                    Offset = new Vector3(0f, SpikeHazardHeight * 0.5f, 0f),
                    Solid = false,
                    BlocksCamera = false
                });
                tile.AddComponent(model.CreateRenderer());
                var transform = tile.GetComponent<TransformComponent>();
                transform.Translation = position + model.Pivot * model.Scale;
                transform.Scale = model.Scale;
                tile.AddComponent(new TimedSpikesComponent
                {
                    UpTime = p.Float("up", 1.2f),
                    DownTime = p.Float("down", 1.2f),
                    Phase = p.Float("phase", 0f) + ix * p.Float("wave", 0f),
                    FloorY = transform.Translation.Y
                });
            }
        }

        /// <summary>Position = floor point the crusher slams onto; <c>height=</c> hover height, <c>size=</c> edge length.</summary>
        private void Crusher(PieceSpec p)
        {
            var size = p.Float("size", 1.8f);
            var height = p.Float("height", 3.2f);
            var entity = Spawner.Create(context, "Crusher", p.Position + new Vector3(0f, height, 0f));
            entity.AddComponent(new HazardComponent());
            entity.AddComponent(new ColliderComponent
            {
                HalfExtents = new Vector3(size * CrusherHazardScale),
                Offset = new Vector3(0f, size * 0.5f, 0f),
                Solid = false,
                BlocksCamera = false
            });
            entity.AddComponent(new CrusherComponent
            {
                Bottom = p.Position,
                Height = height,
                Period = p.Float("period", 3f),
                Phase = p.Float("phase", 0f)
            });
            Spawner.AttachAnimatedModel(context, entity, manifest.Get("crusher"), AnimationDriverKind.Platform, new Vector3(size));
        }

        private void Enemy(PieceSpec p)
        {
            var chase = p.Text("type", "patrol").Equals("chase", StringComparison.OrdinalIgnoreCase);
            var entity = Spawner.Create(context, chase ? "Chaser" : "Patroller", p.Position);
            entity.AddComponent(new ColliderComponent
            {
                HalfExtents = EnemyHalfExtents,
                Offset = new Vector3(0f, EnemyHalfExtents.Y, 0f),
                Solid = false,
                BlocksCamera = false
            });
            var waypoints = p.Path("path").Select(offset => p.Position + offset).ToList();
            if (waypoints.Count == 0)
                waypoints.Add(p.Position);
            entity.AddComponent(new EnemyComponent
            {
                Behavior = chase ? EnemyBehavior.Chase : EnemyBehavior.Patrol,
                Home = p.Position,
                Waypoints = waypoints,
                Speed = p.Float("speed", 2f),
                ChaseSpeed = p.Float("chase_speed", 4.2f),
                ChaseRadius = p.Float("radius", 6f),
                Leash = p.Float("leash", 7f)
            });
            Spawner.AttachAnimatedModel(context, entity, manifest.Get(chase ? "enemy_chase" : "enemy_patrol"), AnimationDriverKind.Enemy, Vector3.One);
        }

        private void Goal(PieceSpec p)
        {
            var goal = Spawner.Create(context, "Goal", p.Position);
            goal.AddComponent(new GoalComponent());
            goal.AddComponent(new ColliderComponent
            {
                HalfExtents = GoalHalfExtents,
                Offset = new Vector3(0f, GoalHalfExtents.Y, 0f),
                Solid = false,
                BlocksCamera = false
            });
            Spawner.CreateModel(context, "GoalFlag", manifest.Get("goal"), p.Position + new Vector3(-1.4f, 0f, 0f), Vector3.One);
            Spawner.CreateModel(context, "GoalFlag", manifest.Get("goal"), p.Position + new Vector3(1.4f, 0f, 0f), Vector3.One, MathF.PI);

            var gem = Spawner.Create(context, "GoalGem", p.Position + new Vector3(0f, GoalGemHeight, 0f));
            var animator = Spawner.AttachAnimatedModel(context, gem, manifest.Get("goal_gem"), AnimationDriverKind.Collectible, Vector3.One);
            animator.SpinSpeed = 1.4f;
            animator.BobHeight = 0.35f;
        }

        private void Deco(PieceSpec p)
        {
            var model = manifest.Get(p.Text("model", "tree"));
            var scale = p.Float("scale", 1f);
            Spawner.CreateModel(context, "Deco", model, p.Position, new Vector3(scale), Yaw(p, GameMath.Hash01(p.Line * 31 + (int)(p.Position.Z * 7f)) * MathF.Tau));
        }

        private void CameraZone(PieceSpec p)
        {
            var zone = Spawner.Create(context, "CameraZone", p.Position);
            zone.AddComponent(new CameraZoneComponent
            {
                Bounds = Box.FromCenter(p.Position, p.Vector("size", new Vector3(10f)) * 0.5f),
                Yaw = Degrees(p, "yaw", 0f),
                Pitch = Degrees(p, "pitch", -20f),
                Distance = p.Float("distance", 7.5f),
                FocusHeight = p.Float("height", 1.4f),
                Fixed = p.Flag("fixed")
            });
        }

        private void Light(PieceSpec p)
        {
            var light = Spawner.Create(context, "Light", p.Position);
            light.AddComponent(new PointLightComponent
            {
                Color = TextData.ParseColor(p.Text("color", "1,0.85,0.6"), Vector4.One),
                Intensity = p.Float("intensity", 2f),
                Range = p.Float("range", 8f)
            });
        }

        private static float Yaw(PieceSpec p, float fallbackRadians) =>
            p.Has("yaw") ? Degrees(p, "yaw", 0f) : fallbackRadians;

        private static float Degrees(PieceSpec p, string key, float fallbackDegrees) =>
            p.Float(key, fallbackDegrees) * MathF.PI / 180f;
    }
}
