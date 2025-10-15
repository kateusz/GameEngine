using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Components;

namespace Benchmark;

/// <summary>
/// Runs benchmarks in headless mode without GUI
/// </summary>
public class HeadlessBenchmarkRunner
{
    private readonly BenchmarkConfig _config;
    private readonly List<BenchmarkResult> _results = new();
    private readonly Dictionary<string, Texture2D> _testTextures = new();

    private Scene? _currentTestScene;
    private OrthographicCamera? _camera;

    private readonly Stopwatch _frameTimer = new();
    private readonly Queue<float> _frameTimes = new();
    private const int MaxFrameSamples = 120;

    private int _frameCount;
    private float _testElapsedTime;

    public HeadlessBenchmarkRunner(BenchmarkConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Run all configured benchmarks
    /// </summary>
    public int Run()
    {
        try
        {
            Console.WriteLine("Initializing headless benchmark runner...");
            Initialize();

            var testsToRun = _config.GetTestsToRun();
            Console.WriteLine($"Running {testsToRun.Count} benchmark test(s)...\n");

            foreach (var testType in testsToRun)
            {
                RunBenchmark(testType);
            }

            // Save results
            SaveResults();

            // Compare with baseline if provided
            if (!string.IsNullOrEmpty(_config.BaselinePath))
            {
                return CompareWithBaseline();
            }

            Console.WriteLine("\nBenchmark completed successfully!");
            PrintResultsSummary();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nError during benchmark execution: {ex.Message}");
            if (_config.Verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            Console.ResetColor();
            return 2; // Error exit code
        }
        finally
        {
            Cleanup();
        }
    }

    private void Initialize()
    {
        try
        {
            // Initialize graphics
            Graphics2D.Instance.Init();
            Graphics3D.Instance.Init();

            // Create camera for rendering
            _camera = new OrthographicCamera(-10, 10, -10, 10);

            // Load test assets
            LoadTestAssets();

            if (_config.Verbose)
            {
                Console.WriteLine("Initialization complete.");
                Console.WriteLine($"  Entity Count: {_config.EntityCount}");
                Console.WriteLine($"  Test Duration: {_config.TestDuration}s");
                Console.WriteLine($"  Regression Threshold: {_config.RegressionThreshold}%");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize OpenGL context for headless benchmarking. " +
                "On macOS, you need to run with a display context. " +
                "On Linux, use 'xvfb-run' to create a virtual framebuffer. " +
                "Alternatively, run the benchmark in GUI mode without --headless flag.", ex);
        }
    }

    private void LoadTestAssets()
    {
        // Create white test texture
        _testTextures["white"] = TextureFactory.Create(1, 1);
        _testTextures["white"].SetData(0xFFFFFFFF, sizeof(uint));

        // Create colored test textures
        var colors = new uint[] { 0xFF0000FF, 0xFF00FF00, 0xFFFF0000, 0xFFFF00FF, 0xFF00FFFF };
        for (int i = 0; i < colors.Length; i++)
        {
            var texture = TextureFactory.Create(1, 1);
            texture.SetData(colors[i], sizeof(uint));
            _testTextures[$"color_{i}"] = texture;
        }

        // Try to load container texture if exists
        try
        {
            if (File.Exists("assets/textures/container.png"))
            {
                _testTextures["container"] = TextureFactory.Create("assets/textures/container.png");
            }
        }
        catch
        {
            // Ignore if texture doesn't exist
        }
    }

    private void RunBenchmark(BenchmarkTestType testType)
    {
        Console.WriteLine($"Running: {testType}");
        Console.Write("  Progress: ");

        _frameCount = 0;
        _testElapsedTime = 0;
        _frameTimes.Clear();

        SetupTestScene(testType);

        var startTime = DateTime.Now;
        var lastProgressUpdate = DateTime.Now;
        var progressChars = new[] { '|', '/', '-', '\\' };
        var progressIndex = 0;

        // Run test loop
        while (_testElapsedTime < _config.TestDuration)
        {
            _frameTimer.Restart();

            // Simulate frame update
            var deltaTime = TimeSpan.FromSeconds(0.016); // ~60 FPS target
            UpdateTest(testType, deltaTime);
            RenderTest();

            _frameTimer.Stop();
            RecordFrameTime((float)_frameTimer.Elapsed.TotalMilliseconds);

            _frameCount++;
            _testElapsedTime = (float)(DateTime.Now - startTime).TotalSeconds;

            // Update progress indicator
            if ((DateTime.Now - lastProgressUpdate).TotalMilliseconds > 100)
            {
                var progress = (_testElapsedTime / _config.TestDuration) * 100;
                Console.Write($"\r  Progress: [{progressChars[progressIndex]}] {progress:F0}%");
                progressIndex = (progressIndex + 1) % progressChars.Length;
                lastProgressUpdate = DateTime.Now;
            }
        }

        Console.WriteLine($"\r  Progress: [✓] 100%");

        FinalizeBenchmark(testType);
        CleanupTestScene();

        if (_config.Verbose)
        {
            var result = _results.Last();
            Console.WriteLine($"    Avg FPS: {result.AverageFPS:F2}");
            Console.WriteLine($"    Avg Frame Time: {result.AverageFrameTime:F2}ms");
            Console.WriteLine($"    Total Frames: {result.TotalFrames}");
        }

        Console.WriteLine();
    }

    private void SetupTestScene(BenchmarkTestType testType)
    {
        _currentTestScene = new Scene("Benchmark");

        var cameraEntity = _currentTestScene.CreateEntity("BenchmarkCamera");
        cameraEntity.AddComponent<TransformComponent>();
        cameraEntity.AddComponent<CameraComponent>();

        switch (testType)
        {
            case BenchmarkTestType.Renderer2DStress:
                SetupRenderer2DStressTest();
                break;
            case BenchmarkTestType.TextureSwitching:
                SetupTextureSwitchingTest();
                break;
            case BenchmarkTestType.DrawCallOptimization:
                SetupDrawCallTest();
                break;
        }
    }

    private void SetupRenderer2DStressTest()
    {
        var random = new Random();

        for (int i = 0; i < _config.EntityCount; i++)
        {
            var entity = _currentTestScene!.CreateEntity($"Sprite_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3(
                (float)(random.NextDouble() * 20 - 10),
                (float)(random.NextDouble() * 20 - 10),
                0);
            transform.Scale = new Vector3(0.5f, 0.5f, 1.0f);

            var sprite = new SpriteRendererComponent
            {
                Color = new Vector4(
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    1.0f)
            };
            entity.AddComponent(sprite);
        }
    }

    private void SetupTextureSwitchingTest()
    {
        var textureKeys = _testTextures.Keys.ToArray();
        var random = new Random();

        for (int i = 0; i < _config.EntityCount; i++)
        {
            var entity = _currentTestScene!.CreateEntity($"TexturedSprite_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3(
                (float)(random.NextDouble() * 20 - 10),
                (float)(random.NextDouble() * 20 - 10),
                0);

            var sprite = entity.AddComponent<SpriteRendererComponent>();
            sprite.Texture = _testTextures[textureKeys[i % textureKeys.Length]];
        }
    }

    private void SetupDrawCallTest()
    {
        var random = new Random();

        for (int i = 0; i < 100; i++)
        {
            var entity = _currentTestScene!.CreateEntity($"DrawCall_{i}");
            entity.AddComponent<TransformComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3(
                (float)(random.NextDouble() * 20 - 10),
                (float)(random.NextDouble() * 20 - 10),
                (float)i * 0.001f);

            var sprite = entity.AddComponent<SpriteRendererComponent>();

            if (i % 2 == 0 && _testTextures.Count > 1)
            {
                sprite.Texture = _testTextures.Values.ElementAt(i % _testTextures.Count);
            }
        }
    }

    private void UpdateTest(BenchmarkTestType testType, TimeSpan deltaTime)
    {
        if (_currentTestScene == null) return;

        switch (testType)
        {
            case BenchmarkTestType.Renderer2DStress:
                UpdateRenderer2DStress();
                break;
            case BenchmarkTestType.TextureSwitching:
                UpdateTextureSwitching();
                break;
            case BenchmarkTestType.DrawCallOptimization:
                UpdateDrawCall();
                break;
        }
    }

    private void UpdateRenderer2DStress()
    {
        foreach (var entity in _currentTestScene!.Entities)
        {
            if (entity.HasComponent<SpriteRendererComponent>())
            {
                var transform = entity.GetComponent<TransformComponent>();
                transform.Rotation = new Vector3(0, 0, transform.Rotation.Z + 0.01f);
            }
        }
    }

    private void UpdateTextureSwitching()
    {
        var random = new Random();
        var textureValues = _testTextures.Values.ToArray();

        foreach (var entity in _currentTestScene!.Entities)
        {
            if (entity.HasComponent<SpriteRendererComponent>() && random.NextDouble() < 0.1)
            {
                var sprite = entity.GetComponent<SpriteRendererComponent>();
                sprite.Texture = textureValues[random.Next(textureValues.Length)];
            }
        }
    }

    private void UpdateDrawCall()
    {
        var random = new Random();

        foreach (var entity in _currentTestScene!.Entities)
        {
            if (!entity.HasComponent<TransformComponent>())
                continue;

            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation += new Vector3(
                (float)(random.NextDouble() * 0.02 - 0.01),
                (float)(random.NextDouble() * 0.02 - 0.01),
                0);

            transform.Rotation = transform.Rotation with { Z = transform.Rotation.Z + 0.01f };

            if (entity.HasComponent<SpriteRendererComponent>() && _testTextures.Count > 1 && random.NextDouble() < 0.05)
            {
                var sprite = entity.GetComponent<SpriteRendererComponent>();
                sprite.Texture = _testTextures.Values.ElementAt(random.Next(_testTextures.Count));
            }
        }
    }

    private void RenderTest()
    {
        if (_camera == null || _currentTestScene == null) return;

        Graphics2D.Instance.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        Graphics2D.Instance.Clear();

        Graphics2D.Instance.BeginScene(_camera);

        foreach (var entity in _currentTestScene.Entities)
        {
            if (!entity.HasComponent<TransformComponent>()) continue;

            var transform = entity.GetComponent<TransformComponent>();

            if (entity.HasComponent<SpriteRendererComponent>())
            {
                var sprite = entity.GetComponent<SpriteRendererComponent>();
                Graphics2D.Instance.DrawSprite(transform.GetTransform(), sprite, entity.Id);
            }
        }

        Graphics2D.Instance.EndScene();
    }

    private void RecordFrameTime(float frameTimeMs)
    {
        _frameTimes.Enqueue(frameTimeMs);
        if (_frameTimes.Count > MaxFrameSamples)
            _frameTimes.Dequeue();
    }

    private void FinalizeBenchmark(BenchmarkTestType testType)
    {
        var frameTimes = _frameTimes.ToArray();
        if (frameTimes.Length == 0) return;

        Array.Sort(frameTimes);

        var result = new BenchmarkResult
        {
            TestName = testType.ToString(),
            TotalFrames = _frameCount,
            AverageFrameTime = frameTimes.Average(),
            MinFPS = 1000.0f / frameTimes.Max(),
            MaxFPS = 1000.0f / frameTimes.Min(),
            AverageFPS = 1000.0f / frameTimes.Average(),
            Percentile99 = frameTimes[(int)(frameTimes.Length * 0.99)],
            TestDuration = _testElapsedTime
        };

        // Add test-specific metrics
        if (testType == BenchmarkTestType.Renderer2DStress)
        {
            var stats = Graphics2D.Instance.GetStats();
            result.CustomMetrics["Avg Draw Calls"] = stats.DrawCalls.ToString();
            result.CustomMetrics["Avg Quads"] = stats.QuadCount.ToString();
        }

        _results.Add(result);
    }

    private void CleanupTestScene()
    {
        _currentTestScene = null;
    }

    private void SaveResults()
    {
        var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_config.OutputPath, json);
        Console.WriteLine($"Results saved to: {_config.OutputPath}");
    }

    private int CompareWithBaseline()
    {
        if (!File.Exists(_config.BaselinePath))
        {
            Console.WriteLine($"Warning: Baseline file not found: {_config.BaselinePath}");
            return 0;
        }

        var baselineJson = File.ReadAllText(_config.BaselinePath);
        var baseline = JsonSerializer.Deserialize<List<BenchmarkResult>>(baselineJson);

        if (baseline == null || baseline.Count == 0)
        {
            Console.WriteLine("Warning: No baseline data to compare against.");
            return 0;
        }

        var detector = new RegressionDetector(_config.RegressionThreshold);
        var analysis = detector.Analyze(_results, baseline);

        analysis.PrintSummary(_config.Verbose);

        if (analysis.HasRegressions && _config.FailOnRegression)
        {
            return 1; // Regression detected exit code
        }

        return 0;
    }

    private void PrintResultsSummary()
    {
        Console.WriteLine("\nBenchmark Results:");
        Console.WriteLine(new string('-', 60));

        foreach (var result in _results)
        {
            Console.WriteLine($"{result.TestName}:");
            Console.WriteLine($"  Avg FPS:        {result.AverageFPS:F2}");
            Console.WriteLine($"  Avg Frame Time: {result.AverageFrameTime:F2}ms");
            Console.WriteLine($"  Min FPS:        {result.MinFPS:F2}");
            Console.WriteLine($"  Max FPS:        {result.MaxFPS:F2}");
            Console.WriteLine($"  99th Percentile: {result.Percentile99:F2}ms");
            Console.WriteLine($"  Total Frames:   {result.TotalFrames}");

            if (result.CustomMetrics.Count > 0)
            {
                Console.WriteLine("  Custom Metrics:");
                foreach (var metric in result.CustomMetrics)
                {
                    Console.WriteLine($"    {metric.Key}: {metric.Value}");
                }
            }
            Console.WriteLine();
        }
    }

    private void Cleanup()
    {
        _testTextures.Clear();
        _currentTestScene = null;
    }
}
