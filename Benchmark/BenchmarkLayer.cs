using System.Diagnostics;
using System.Numerics;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;

namespace Benchmark;

public class BenchmarkLayer : ILayer
{
    private readonly List<BenchmarkResult> _results = new();
    private readonly Stopwatch _frameTimer = new();
    private readonly Queue<float> _frameTimes = new();
    private const int MaxFrameSamples = 120;
        
    // Test scenes
    private Scene? _currentTestScene;
    private OrthographicCameraController? _cameraController;
    private readonly Dictionary<string, Texture2D> _testTextures = new();
        
    // Benchmark configurations - using fields for ImGui compatibility
    private int _entityCount = 10000;
    private int _drawCallsPerFrame = 10000;
    private int _textureCount = 1000;
    private int _scriptEntityCount = 50;
    private float _testDuration = 10.0f; // seconds
    private bool _enableVSync = false;
        
    // Benchmark state
    private BenchmarkTestType _currentTestType = BenchmarkTestType.None;
    private float _testElapsedTime;
    private bool _isRunning;
    private int _frameCount;
    private List<BenchmarkResult> _baselineResults = new();
    
    public void OnAttach(IInputSystem inputSystem)
    {
        _cameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        LoadTestAssets();
    }

    public void OnDetach()
    {
        CleanupTestScene();
        _testTextures.Clear();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _frameTimer.Restart();
        
        // Clear the screen first
        Graphics2D.Instance.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)); // Dark gray background
        Graphics2D.Instance.Clear();
            
        if (_isRunning && _currentTestType != BenchmarkTestType.None)
        {
            UpdateBenchmark(timeSpan);
        }
            
        // Always update camera for viewport control
        _cameraController?.OnUpdate(timeSpan);
            
        // Render current test scene if active
        if (_currentTestScene != null && _isRunning)
        {
            RenderTestScene();
        }
            
        _frameTimer.Stop();
        RecordFrameTime((float)_frameTimer.Elapsed.TotalMilliseconds);
    }

    public void OnImGuiRender()
    {
        RenderBenchmarkUI();
        RenderResultsWindow();
        RenderPerformanceMonitor();
    }
    
    public void HandleInputEvent(InputEvent windowEvent) => HandleEvent(windowEvent);

    public void HandleWindowEvent(WindowEvent windowEvent) => HandleEvent(windowEvent);

    private void HandleEvent(Event @event) => _cameraController?.OnEvent(@event);

    private void LoadTestAssets()
    {
        // Create white test texture with proper data
        _testTextures["white"] = TextureFactory.Create(1, 1);
        _testTextures["white"].SetData(0xFFFFFFFF, sizeof(uint));
            
        // Create colored test textures with proper data initialization
        var colors = new uint[] { 0xFF0000FF, 0xFF00FF00, 0xFFFF0000, 0xFFFF00FF, 0xFF00FFFF };
        for (int i = 0; i < colors.Length; i++)
        {
            var texture = TextureFactory.Create(1, 1); // Use 1x1 for simplicity
            texture.SetData(colors[i], sizeof(uint)); // FIXED: Actually set the color data
            _testTextures[$"color_{i}"] = texture;
        }
        
        _testTextures["container"] = TextureFactory.Create("assets/textures/container.png");
    }

    private void RenderBenchmarkUI()
    {
        ImGui.Begin("Benchmark Control", ImGuiWindowFlags.AlwaysVerticalScrollbar);
            
        ImGui.Text("Benchmark Configuration");
        ImGui.Separator();
            
        ImGui.DragInt("Entity Count", ref _entityCount, 100, 100, 50000);
        ImGui.DragInt("Draw Calls/Frame", ref _drawCallsPerFrame, 10, 10, 10000);
        ImGui.DragInt("Texture Count", ref _textureCount, 1, 1, 32);
        ImGui.DragInt("Script Entities", ref _scriptEntityCount, 10, 0, 1000);
        ImGui.DragFloat("Test Duration (s)", ref _testDuration, 0.5f, 1.0f, 60.0f);
        ImGui.Checkbox("VSync", ref _enableVSync);
            
        ImGui.Separator();
            
        if (!_isRunning)
        {
            ImGui.Text("Select Benchmark Test:");
                
            if (ImGui.Button("Renderer2D Stress Test"))
                StartBenchmark(BenchmarkTestType.Renderer2DStress);
                
            if (ImGui.Button("Texture Switching Test"))
                StartBenchmark(BenchmarkTestType.TextureSwitching);
                
            if (ImGui.Button("Draw Call Test"))
                StartBenchmark(BenchmarkTestType.DrawCallOptimization);
        }
        else
        {
            ImGui.Text($"Running: {_currentTestType}");
            ImGui.Text($"Progress: {(_testElapsedTime / _testDuration * 100):F1}%");
            ImGui.ProgressBar(_testElapsedTime / _testDuration);
                
            if (ImGui.Button("Stop"))
                StopBenchmark();
        }
            
        ImGui.End();
    }

    private void RenderResultsWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(600, 500), ImGuiCond.FirstUseEver);
        ImGui.Begin("Benchmark Results");
            
        if (_results.Count > 0)
        {
            if (ImGui.Button("Clear Results"))
                _results.Clear();
            
            if (ImGui.Button("Save as Baseline"))
                BenchmarkStorage.SaveBaseline(_results);

            ImGui.SameLine();

            if (ImGui.Button("Load Baseline"))
                _baselineResults = BenchmarkStorage.LoadBaseline();

                
            ImGui.Separator();
                
            foreach (var result in _results)
            {
                ImGui.Text($"{result.TestName}:");
                ImGui.Indent();
                ImGui.Text($"Avg FPS: {result.AverageFPS:F2}");
                ImGui.Text($"Min FPS: {result.MinFPS:F2}");
                ImGui.Text($"Max FPS: {result.MaxFPS:F2}");
                ImGui.Text($"Avg Frame Time: {result.AverageFrameTime:F2}ms");
                ImGui.Text($"99th Percentile: {result.Percentile99:F2}ms");
                ImGui.Text($"Total Frames: {result.TotalFrames}");
                    
                if (result.CustomMetrics.Count > 0)
                {
                    ImGui.Text("Custom Metrics:");
                    foreach (var metric in result.CustomMetrics)
                    {
                        ImGui.Text($"  {metric.Key}: {metric.Value}");
                    }
                }
                    
                ImGui.Unindent();
                ImGui.Separator();
                
                var baseline = _baselineResults.FirstOrDefault(b => b.TestName == result.TestName);
                if (baseline != null)
                {
                    ImGui.Text("Comparison with Baseline:");
                    ImGui.Indent();

                    float fpsDiff = result.AverageFPS - baseline.AverageFPS;
                    float frameTimeDiff = result.AverageFrameTime - baseline.AverageFrameTime;

                    // FPS
                    ImGui.PushStyleColor(ImGuiCol.Text, fpsDiff >= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
                    ImGui.Text($"Δ Avg FPS: {fpsDiff:+0.00;-0.00;0.00}");
                    ImGui.PopStyleColor();

                    // Frame time
                    ImGui.PushStyleColor(ImGuiCol.Text, frameTimeDiff <= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
                    ImGui.Text($"Δ Frame Time: {frameTimeDiff:+0.00;-0.00;0.00}ms");
                    ImGui.PopStyleColor();

                    ImGui.Unindent();
                }

            }
        }
        else
        {
            ImGui.Text("No benchmark results yet.");
        }
            
        ImGui.End();
    }

    private void RenderPerformanceMonitor()
    {
        ImGui.Begin("Performance Monitor##Benchmark"); // Added unique ID suffix
            
        var frameTimes = _frameTimes.ToArray();
        if (frameTimes.Length > 0)
        {
            var avgFrameTime = frameTimes.Average();
            var minFrameTime = frameTimes.Min();
            var maxFrameTime = frameTimes.Max();
                
            ImGui.Text($"Current FPS: {(1000.0f / avgFrameTime):F2}");
            ImGui.Text($"Frame Time: {avgFrameTime:F2}ms (min: {minFrameTime:F2}, max: {maxFrameTime:F2})");
                
            // Simple frame time graph
            if (frameTimes.Length > 1) // Ensure we have enough data for plotting
            {
                ImGui.PlotLines("Frame Times", ref frameTimes[0], frameTimes.Length, 0, 
                    null, 0, maxFrameTime * 1.2f, new Vector2(0, 80));
            }
        }
        else
        {
            ImGui.Text("Collecting performance data...");
        }
            
        // Renderer stats
        var stats2D = Graphics2D.Instance.GetStats();
        ImGui.Separator();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Text($"Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats2D.GetTotalIndexCount()}");
            
        ImGui.End();
    }

    private void StartBenchmark(BenchmarkTestType testType)
    {
        _currentTestType = testType;
        _isRunning = true;
        _testElapsedTime = 0;
        _frameCount = 0;
        _frameTimes.Clear();
            
        CleanupTestScene();
        SetupTestScene(testType);
    }

    private void StopBenchmark()
    {
        if (_isRunning && _currentTestType != BenchmarkTestType.None)
        {
            FinalizeBenchmark();
        }
            
        _isRunning = false;
        _currentTestType = BenchmarkTestType.None;
        CleanupTestScene();
    }

    private void UpdateBenchmark(TimeSpan ts)
    {
        _testElapsedTime += (float)ts.TotalSeconds;
        _frameCount++;
            
        // Update test scene
        if (_currentTestScene != null)
        {
            // TODO: align to 2d camera
            //_currentTestScene.OnUpdateEditor(ts, new Engine.Renderer.EditorCamera()); // Fixed: use OnUpdateEditor instead of OnUpdateRuntime
                
            // Add test-specific updates
            switch (_currentTestType)
            {
                case BenchmarkTestType.Renderer2DStress:
                    UpdateRenderer2DStress();
                    break;
                case BenchmarkTestType.DrawCallOptimization:
                    UpdateDrawCall();
                    break;
                case BenchmarkTestType.TextureSwitching:
                    UpdateTextureSwitching();
                    break;
            }
        }
            
        // Check if test is complete
        if (_testElapsedTime >= _testDuration)
        {
            StopBenchmark();
        }
    }

    private void UpdateDrawCall()
    {
        if (_currentTestScene == null) return;

        var random = new Random();

        foreach (var entity in _currentTestScene.Entities)
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform)) 
                continue;

            // Slightly animate position or rotation to force updates
            transform.Translation += new Vector3(
                (float)(random.NextDouble() * 0.02 - 0.01),
                (float)(random.NextDouble() * 0.02 - 0.01),
                0);

            transform.Rotation = transform.Rotation with { Z = transform.Rotation.Z + 0.01f };

            // Optionally, cycle textures to break batching and increase draw calls
            if (_testTextures.Count > 1 && entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            {
                if (random.NextDouble() < 0.05) // 5% chance per frame
                {
                    sprite.Texture = _testTextures.Values.ElementAt(random.Next(_testTextures.Count));
                }
            }
        }
    }

    private void SetupTestScene(BenchmarkTestType testType)
    {
        _currentTestScene = new Engine.Scene.Scene("Benchmark");
            
        // Add camera entity
        var cameraEntity = _currentTestScene.CreateEntity("BenchmarkCamera");
        cameraEntity.AddComponent<TransformComponent>(); // Add required TransformComponent
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
            
        // Create many sprite entities
        for (var i = 0; i < _entityCount; i++)
        {
            var entity = _currentTestScene.CreateEntity($"Sprite_{i}");
            entity.AddComponent<TransformComponent>(); // Explicitly add TransformComponent
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
            
        for (int i = 0; i < _entityCount; i++)
        {
            var entity = _currentTestScene.CreateEntity($"TexturedSprite_{i}");
            entity.AddComponent<TransformComponent>(); // Add required component
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
        // Create entities that will force many draw calls
        var random = new Random();
            
        for (int i = 0; i < _drawCallsPerFrame; i++)
        {
            var entity = _currentTestScene.CreateEntity($"DrawCall_{i}");
            entity.AddComponent<TransformComponent>(); // Add required component
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = new Vector3(
                (float)(random.NextDouble() * 20 - 10),
                (float)(random.NextDouble() * 20 - 10),
                (float)i * 0.001f); // Different Z values to prevent batching
                
            var sprite = entity.AddComponent<SpriteRendererComponent>();
                
            // Use different textures to force draw call breaks
            if (i % 2 == 0 && _testTextures.Count > 1)
            {
                sprite.Texture = _testTextures.Values.ElementAt(i % _testTextures.Count);
            }
        }
    }

    private void UpdateRenderer2DStress()
    {
        // Animate sprites
        foreach (var entity in _currentTestScene.Entities)
        {
            if (entity.HasComponent<SpriteRendererComponent>() && entity.TryGetComponent<TransformComponent>(out var transform))
            {
                transform.Rotation = new Vector3(0, 0, transform.Rotation.Z + 0.01f);
            }
        }
    }

    private void UpdateTextureSwitching()
    {
        // Randomly switch textures to stress texture binding
        var random = new Random();
        var textureValues = _testTextures.Values.ToArray();
            
        foreach (var entity in _currentTestScene.Entities)
        {
            if (random.NextDouble() < 0.1 && entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            {
                sprite.Texture = textureValues[random.Next(textureValues.Length)];
            }
        }
    }

    private void RenderTestScene()
    {
        if (_cameraController?.Camera == null) return;
        
        Graphics2D.Instance.BeginScene(_cameraController.Camera);
            
        // Render all entities in the test scene
        foreach (var entity in _currentTestScene.Entities)
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform)) continue;
                
            if (entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            {
                Graphics2D.Instance.DrawSprite(transform.GetTransform(), sprite, entity.Id);
            }
        }
            
        Graphics2D.Instance.EndScene();
    }

    private void CleanupTestScene()
    {
        _currentTestScene = null;
    }

    private void RecordFrameTime(float frameTimeMs)
    {
        _frameTimes.Enqueue(frameTimeMs);
        if (_frameTimes.Count > MaxFrameSamples)
            _frameTimes.Dequeue();
    }

    private void FinalizeBenchmark()
    {
        var frameTimes = _frameTimes.ToArray();
        if (frameTimes.Length == 0) return;
            
        Array.Sort(frameTimes);
            
        var result = new BenchmarkResult
        {
            TestName = _currentTestType.ToString(),
            TotalFrames = _frameCount,
            AverageFrameTime = frameTimes.Average(),
            MinFPS = 1000.0f / frameTimes.Max(),
            MaxFPS = 1000.0f / frameTimes.Min(),
            AverageFPS = 1000.0f / frameTimes.Average(),
            Percentile99 = frameTimes[(int)(frameTimes.Length * 0.99)],
            TestDuration = _testElapsedTime
        };
            
        // Add test-specific metrics
        switch (_currentTestType)
        {
            case BenchmarkTestType.Renderer2DStress:
                var stats = Graphics2D.Instance.GetStats();
                result.CustomMetrics["Avg Draw Calls"] = stats.DrawCalls.ToString();
                result.CustomMetrics["Avg Quads"] = stats.QuadCount.ToString();
                break;
        }
            
        _results.Add(result);
    }
}