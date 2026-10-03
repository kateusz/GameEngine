using System.Numerics;
using Engine.Project;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace Benchmark;

public sealed class Benchmark2DRunner(IGraphics2D graphics2D, SceneFactory sceneFactory, ITextureFactory textureFactory)
    : IBenchmarkRunner, IDisposable
{
    private readonly Dictionary<string, Texture2D> _testTextures = new();
    private readonly Random _rng = new();
    private SceneCamera? _camera;
    private int _entityCount = 10000;
    private int _drawCallsPerFrame = 10000;
    private int _textureCount = 1000;
    private int _scriptEntityCount = 50;
    private float _duration = 5f;
    private bool _enableVSync;

    public string Title => "2D";
    public BenchmarkSettings Settings => new(_entityCount, _duration);

    public IReadOnlyList<BenchmarkTestButton> Tests { get; } =
    [
        new(nameof(BenchmarkTestType.Renderer2DStress), "Renderer2D Stress Test"),
        new(nameof(BenchmarkTestType.TextureSwitching), "Texture Switching Test"),
        new(nameof(BenchmarkTestType.DrawCallOptimization), "Draw Call Test"),
        new(nameof(BenchmarkTestType.Profiling2D), "Profiling2D Preset (5k sprites)"),
        new(nameof(BenchmarkTestType.Physics2DStress), "Physics2D Stress Test")
    ];

    public void Load()
    {
        _camera = new SceneCamera();
        _camera.SetOrthographic(10f, -10f, 10f);
        _camera.SetViewportSize(800, 600);

        _testTextures["white"] = textureFactory.GetWhiteTexture();
        var colors = new[] { 0xFF0000FF, 0xFF00FF00, 0xFFFF0000, 0xFFFF00FF, 0xFF00FFFF };
        for (var i = 0; i < colors.Length; i++)
        {
            var texture = textureFactory.Create(1, 1);
            texture.SetData(colors[i], sizeof(uint));
            _testTextures[$"color_{i}"] = texture;
        }

        _testTextures["container"] = textureFactory.Create("assets/textures/container.png");
    }

    public void AdjustZoom(float yOffset) => _camera?.AdjustOrthographicSize(yOffset);

    public void DrawControls()
    {
        ImGui.DragInt("Entity Count", ref _entityCount, 100, 100, 50000);
        ImGui.DragInt("Draw Calls/Frame", ref _drawCallsPerFrame, 10, 10, 10000);
        ImGui.DragInt("Texture Count", ref _textureCount, 1, 1, 32);
        ImGui.DragInt("Script Entities", ref _scriptEntityCount, 10, 0, 1000);
        ImGui.DragFloat("Test Duration (s)", ref _duration, 0.5f, 1f, 60f);
        ImGui.Checkbox("VSync", ref _enableVSync);
    }

    public bool TryCreate(string testId, BenchmarkSettings settings, out BenchmarkRun? run, out string? error)
    {
        run = null;
        error = null;
        if (!Enum.TryParse<BenchmarkTestType>(testId, out var type) || type == BenchmarkTestType.None)
        {
            error = $"Unknown test: {testId}";
            return false;
        }

        if (type == BenchmarkTestType.Profiling2D)
        {
            _entityCount = 5000;
            _duration = 5f;
            run = CreateRun(type, profilingPhase: 1, chain: true);
            return true;
        }

        if (type == BenchmarkTestType.Physics2DStress)
        {
            _entityCount = 500;
            _duration = 5f;
        }
        else
        {
            _entityCount = settings.Count;
            _duration = settings.DurationSeconds;
        }

        run = CreateRun(type, profilingPhase: 0, chain: false);
        return true;
    }

    public void Dispose()
    {
        foreach (var kvp in _testTextures)
        {
            if (kvp.Key.StartsWith("color_", StringComparison.Ordinal)
                || kvp.Key.StartsWith("profile_", StringComparison.Ordinal))
                kvp.Value.Dispose();
        }

        _testTextures.Clear();
    }

    private Benchmark2DRun CreateRun(BenchmarkTestType type, int profilingPhase, bool chain)
    {
        var scene = sceneFactory.Create("Benchmark");
        var cameraEntity = scene.CreateEntity("BenchmarkCamera");
        cameraEntity.AddComponent<TransformComponent>();
        var cameraComponent = cameraEntity.AddComponent<CameraComponent>();
        var ecs = type == BenchmarkTestType.Physics2DStress;
        if (ecs)
        {
            cameraComponent.Primary = true;
            cameraComponent.OrthographicSize = 20f;
        }

        var run = new Benchmark2DRun(
            type, profilingPhase, _duration, scene, ecs, graphics2D, textureFactory, _camera!, _testTextures, _rng,
            chain ? () => CreateRun(BenchmarkTestType.Profiling2D, profilingPhase: 2, chain: false) : null);

        switch (type)
        {
            case BenchmarkTestType.Renderer2DStress:
                SetupRenderer2DStress(scene);
                break;
            case BenchmarkTestType.TextureSwitching:
                SetupTextureSwitching(scene);
                break;
            case BenchmarkTestType.DrawCallOptimization:
                SetupDrawCall(scene);
                break;
            case BenchmarkTestType.Profiling2D:
                SetupProfiling(scene, multiTexture: profilingPhase == 2);
                break;
            case BenchmarkTestType.Physics2DStress:
                SetupPhysics(scene);
                break;
        }

        if (ecs)
            scene.OnRuntimeStart();

        return run;
    }

    private void SetupProfiling(IScene scene, bool multiTexture)
    {
        while (_testTextures.Count < 32)
        {
            var i = _testTextures.Count;
            var texture = textureFactory.Create(1, 1);
            texture.SetData((uint)(0xFF000000 | (uint)(i * 8 % 256) << 16 | (uint)(i * 4 % 256) << 8 | (uint)(i * 2 % 256)), sizeof(uint));
            _testTextures[$"profile_{i}"] = texture;
        }

        var texturePaths = _testTextures.Values.Select(t => t.Path).Where(p => !string.IsNullOrEmpty(p)).ToArray();
        if (texturePaths.Length == 0)
            texturePaths = [_testTextures["white"].Path!];
        var singlePath = texturePaths[0];

        for (var i = 0; i < _entityCount; i++)
        {
            var entity = scene.CreateEntity($"ProfileSprite_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3((float)(_rng.NextDouble() * 20 - 10), (float)(_rng.NextDouble() * 20 - 10), 0);
            transform.Scale = new Vector3(0.5f, 0.5f, 1f);
            var sprite = entity.AddComponent<SpriteRendererComponent>();
            sprite.TexturePath = multiTexture ? texturePaths[i % texturePaths.Length] : singlePath;
            sprite.Color = Vector4.One;
        }
    }

    private void SetupRenderer2DStress(IScene scene)
    {
        for (var i = 0; i < _entityCount; i++)
        {
            var entity = scene.CreateEntity($"Sprite_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3((float)(_rng.NextDouble() * 20 - 10), (float)(_rng.NextDouble() * 20 - 10), 0);
            transform.Scale = new Vector3(0.5f, 0.5f, 1f);
            entity.AddComponent(new SpriteRendererComponent
            {
                Color = new Vector4((float)_rng.NextDouble(), (float)_rng.NextDouble(), (float)_rng.NextDouble(), 1f)
            });
        }
    }

    private void SetupTextureSwitching(IScene scene)
    {
        var textureKeys = _testTextures.Keys.ToArray();
        for (var i = 0; i < _entityCount; i++)
        {
            var entity = scene.CreateEntity($"TexturedSprite_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3((float)(_rng.NextDouble() * 20 - 10), (float)(_rng.NextDouble() * 20 - 10), 0);
            var sprite = entity.AddComponent<SpriteRendererComponent>();
            sprite.TexturePath = _testTextures[textureKeys[i % textureKeys.Length]].Path;
        }
    }

    private void SetupPhysics(IScene scene)
    {
        const float boxSize = 0.5f;
        const float spacing = 0.52f;
        const int maxRows = 3;
        const float spawnBaseY = 10f;
        var cols = System.Math.Max(1, (int)System.Math.Ceiling(_entityCount / (double)maxRows));
        var groundWidth = cols * spacing + 2f;

        var ground = scene.CreateEntity("Ground");
        ground.AddComponent<TransformComponent>();
        var groundTransform = ground.GetComponent<TransformComponent>();
        groundTransform.Translation = new Vector3(0, -8, 0);
        groundTransform.Scale = new Vector3(groundWidth, 1, 1);
        ground.AddComponent<RigidBody2DComponent>().BodyType = RigidBodyType.Static;
        ground.AddComponent<BoxCollider2DComponent>().Size = new Vector2(groundWidth, 1);
        ground.AddComponent<SpriteRendererComponent>().Color = new Vector4(0.3f, 0.3f, 0.35f, 1f);

        var xOffset = (cols - 1) * spacing * 0.5f;
        for (var i = 0; i < _entityCount; i++)
        {
            var entity = scene.CreateEntity($"PhysicsBox_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3(i % cols * spacing - xOffset, spawnBaseY + i / cols * spacing, 0);
            entity.AddComponent<RigidBody2DComponent>().BodyType = RigidBodyType.Dynamic;
            entity.AddComponent<BoxCollider2DComponent>().Size = new Vector2(boxSize, boxSize);
            entity.AddComponent<SpriteRendererComponent>().Color = new Vector4(
                (float)(_rng.NextDouble() * 0.5 + 0.5),
                (float)(_rng.NextDouble() * 0.5 + 0.5),
                (float)(_rng.NextDouble() * 0.5 + 0.5),
                1f);
        }
    }

    private void SetupDrawCall(IScene scene)
    {
        for (var i = 0; i < _drawCallsPerFrame; i++)
        {
            var entity = scene.CreateEntity($"DrawCall_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3(
                (float)(_rng.NextDouble() * 20 - 10),
                (float)(_rng.NextDouble() * 20 - 10),
                i * 0.001f);
            var sprite = entity.AddComponent<SpriteRendererComponent>();
            if (i % 2 == 0 && _testTextures.Count > 1)
                sprite.TexturePath = _testTextures.Values.ElementAt(i % _testTextures.Count).Path;
        }
    }

    private sealed class Benchmark2DRun : BenchmarkRun
    {
        private readonly BenchmarkTestType _type;
        private readonly int _profilingPhase;
        private readonly IScene _scene;
        private readonly bool _ecs;
        private readonly IGraphics2D _graphics2D;
        private readonly ITextureFactory _textureFactory;
        private readonly SceneCamera _camera;
        private readonly Dictionary<string, Texture2D> _textures;
        private readonly Random _rng;
        private readonly Func<BenchmarkRun>? _next;
        private bool _disposed;

        public Benchmark2DRun(
            BenchmarkTestType type,
            int profilingPhase,
            float durationSeconds,
            IScene scene,
            bool ecs,
            IGraphics2D graphics2D,
            ITextureFactory textureFactory,
            SceneCamera camera,
            Dictionary<string, Texture2D> textures,
            Random rng,
            Func<BenchmarkRun>? next)
        {
            _type = type;
            _profilingPhase = profilingPhase;
            DurationSeconds = durationSeconds;
            _scene = scene;
            _ecs = ecs;
            _graphics2D = graphics2D;
            _textureFactory = textureFactory;
            _camera = camera;
            _textures = textures;
            _rng = rng;
            _next = next;
            ResultName = type switch
            {
                BenchmarkTestType.Profiling2D when profilingPhase == 1 => "Profiling2D_SingleTexture",
                BenchmarkTestType.Profiling2D when profilingPhase == 2 => "Profiling2D_MultiTexture",
                _ => type.ToString()
            };
        }

        public override string ResultName { get; }
        public override float DurationSeconds { get; }
        public override bool SamplesRenderer2D => true;

        public override BenchmarkRun? NextPhase() => _next?.Invoke();

        public override void Tick(TimeSpan delta)
        {
            if (!_ecs)
            {
                switch (_type)
                {
                    case BenchmarkTestType.Renderer2DStress:
                    case BenchmarkTestType.Profiling2D:
                        SpinSprites();
                        break;
                    case BenchmarkTestType.DrawCallOptimization:
                        JitterDrawCalls();
                        break;
                    case BenchmarkTestType.TextureSwitching:
                        SwitchTextures();
                        break;
                }
            }

            _graphics2D.ResetStats();
            if (_ecs)
                _scene.OnUpdateRuntime(delta);
            else
                DrawScene();
        }

        public override void Contribute(BenchmarkResult result)
        {
            result.CustomMetrics["Render Path"] = _ecs
                ? "ECS (SceneRenderPipeline)"
                : "direct draw (BenchmarkLayer)";
            if (_type == BenchmarkTestType.Physics2DStress)
                result.CustomMetrics["Physics Bodies"] =
                    _scene.Entities.Count(entity => entity.HasComponent<RigidBody2DComponent>()).ToString();
        }

        public override void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (_ecs)
                _scene.OnRuntimeStop();
            _scene.Dispose();
        }

        private void SpinSprites()
        {
            foreach (var entity in _scene.Entities)
            {
                if (entity.HasComponent<SpriteRendererComponent>() &&
                    entity.TryGetComponent<TransformComponent>(out var transform))
                    transform.Rotation = new Vector3(0, 0, transform.Rotation.Z + 0.01f);
            }
        }

        private void JitterDrawCalls()
        {
            var textures = _textures.Values.ToArray();
            foreach (var entity in _scene.Entities)
            {
                if (!entity.TryGetComponent<TransformComponent>(out var transform))
                    continue;
                transform.Translation += new Vector3(
                    (float)(_rng.NextDouble() * 0.02 - 0.01),
                    (float)(_rng.NextDouble() * 0.02 - 0.01),
                    0);
                transform.Rotation = transform.Rotation with { Z = transform.Rotation.Z + 0.01f };
                if (textures.Length > 1 && entity.TryGetComponent<SpriteRendererComponent>(out var sprite) && _rng.NextDouble() < 0.05)
                    sprite.TexturePath = textures[_rng.Next(textures.Length)].Path;
            }
        }

        private void SwitchTextures()
        {
            var textureValues = _textures.Values.ToArray();
            foreach (var entity in _scene.Entities)
            {
                if (_rng.NextDouble() < 0.1 && entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                    sprite.TexturePath = textureValues[_rng.Next(textureValues.Length)].Path;
            }
        }

        private void DrawScene()
        {
            _graphics2D.BeginScene(new SceneView(_camera.GetProjectionMatrix()));
            foreach (var entity in _scene.Entities)
            {
                if (!entity.TryGetComponent<TransformComponent>(out var transform))
                    continue;
                if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                    continue;

                var trs = transform.GetTransform();
                if (!string.IsNullOrWhiteSpace(sprite.TexturePath))
                {
                    try
                    {
                        var texture = _textureFactory.Create(PathBuilder.Resolve(sprite.TexturePath));
                        _graphics2D.DrawQuad(trs, texture, [
                            new Vector2(0f, 0f),
                            new Vector2(1f, 0f),
                            new Vector2(1f, 1f),
                            new Vector2(0f, 1f)
                        ], sprite.TilingFactor, sprite.Color, entity.Id);
                        continue;
                    }
                    catch
                    {
                        // fall through to solid color
                    }
                }

                _graphics2D.DrawQuad(trs, sprite.Color, entity.Id);
            }

            _graphics2D.EndScene();
        }
    }
}
