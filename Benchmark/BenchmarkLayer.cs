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

public class BenchmarkLayer(IGraphics2D graphics2D, SceneFactory sceneFactory, ITextureFactory textureFactory)
    : ILayer
{
    private readonly List<BenchmarkResult> _results = [];
    private readonly Stopwatch _frameTimer = new();
    private readonly Queue<float> _frameTimes = new();
    private const int MaxFrameSamples = 120;

    // CPU and Memory monitoring
    private readonly Queue<float> _cpuUsageSamples = new();
    private readonly Queue<long> _memorySamples = new();
    private Process? _currentProcess;
    private DateTime _lastCpuCheck = DateTime.UtcNow;
    private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private float _currentCpuUsage;
    private long _currentMemoryUsageMB;

    // Test scenes
    private IScene? _currentTestScene;
    private IOrthographicCameraController? _cameraController;
    private readonly Dictionary<string, Texture2D> _testTextures = new();

    // Benchmark configurations
    private int _entityCount = 10000;
    private int _drawCallsPerFrame = 10000;
    private int _textureCount = 1000;
    private int _scriptEntityCount = 50;
    private float _testDurationInSeconds = 5.0f;
    private bool _enableVSync;

    private BenchmarkTestType _currentTestType = BenchmarkTestType.None;
    private float _testElapsedTime;
    private bool _isRunning;
    private int _frameCount;
    private List<BenchmarkResult> _baselineResults = [];

    public void OnAttach(IInputSystem inputSystem)
    {
        _cameraController = new OrthographicCameraController(DisplayConfig.DefaultAspectRatio, true);
        LoadTestAssets();

        // Initialize process monitoring
        _currentProcess = Process.GetCurrentProcess();
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        _lastCpuCheck = DateTime.UtcNow;
    }

    public void OnDetach()
    {
        CleanupTestScene();
        _testTextures.Clear();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _frameTimer.Restart();

        // Update CPU and memory metrics
        UpdateSystemMetrics();

        // Clear the screen first
        graphics2D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)); // Dark gray background
        graphics2D.Clear();

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

    public void Draw()
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
        // Use shared white test texture
        _testTextures["white"] = textureFactory.GetWhiteTexture();

        // Create colored test textures with proper data initialization
        var colors = new[] { 0xFF0000FF, 0xFF00FF00, 0xFFFF0000, 0xFFFF00FF, 0xFF00FFFF };
        for (var i = 0; i < colors.Length; i++)
        {
            var texture = textureFactory.Create(1, 1); // Use 1x1 for simplicity
            texture.SetData(colors[i], sizeof(uint)); // FIXED: Actually set the color data
            _testTextures[$"color_{i}"] = texture;
        }

        _testTextures["container"] = textureFactory.Create("assets/textures/container.png");
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
        ImGui.DragFloat("Test Duration (s)", ref _testDurationInSeconds, 0.5f, 1.0f, 60.0f);
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
            ImGui.Text($"Progress: {(_testElapsedTime / _testDurationInSeconds * 100):F1}%");
            ImGui.ProgressBar(_testElapsedTime / _testDurationInSeconds);

            if (ImGui.Button("Stop"))
                StopBenchmark();
        }

        ImGui.End();
    }

    private void RenderResultsWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(DisplayConfig.StandardPopupSize.Width, 500), ImGuiCond.FirstUseEver);
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

            ImGui.SameLine();

            if (ImGui.Button("Export to Markdown"))
                ExportResultsToMarkdown();


            ImGui.Separator();

            foreach (var result in _results)
            {
                ImGui.Text($"{result.TestName}:");
                ImGui.Indent();

                // Performance metrics
                ImGui.Text("Performance:");
                ImGui.Indent();
                ImGui.Text($"Avg FPS: {result.AverageFPS:F2}");
                ImGui.Text($"Min FPS: {result.MinFPS:F2}");
                ImGui.Text($"Max FPS: {result.MaxFPS:F2}");
                ImGui.Text($"Avg Frame Time: {result.AverageFrameTime:F2}ms");
                ImGui.Text($"99th Percentile: {result.Percentile99:F2}ms");
                ImGui.Text($"Total Frames: {result.TotalFrames}");
                ImGui.Unindent();

                // CPU metrics
                ImGui.Text("CPU Usage:");
                ImGui.Indent();
                ImGui.Text($"Average: {result.AverageCpuUsage:F2}%");
                ImGui.Text($"Min: {result.MinCpuUsage:F2}%");
                ImGui.Text($"Max: {result.MaxCpuUsage:F2}%");
                ImGui.Unindent();

                // Memory metrics
                ImGui.Text("Memory Usage:");
                ImGui.Indent();
                ImGui.Text($"Average: {result.AverageMemoryUsageMB} MB");
                ImGui.Text($"Min: {result.MinMemoryUsageMB} MB");
                ImGui.Text($"Max: {result.MaxMemoryUsageMB} MB");
                ImGui.Unindent();

                if (result.CustomMetrics.Count > 0)
                {
                    ImGui.Text("Custom Metrics:");
                    ImGui.Indent();
                    foreach (var metric in result.CustomMetrics)
                    {
                        ImGui.Text($"{metric.Key}: {metric.Value}");
                    }

                    ImGui.Unindent();
                }

                ImGui.Unindent();
                ImGui.Separator();

                var baseline = _baselineResults.FirstOrDefault(b => b.TestName == result.TestName);
                if (baseline != null)
                {
                    ImGui.Text("Comparison with Baseline:");
                    ImGui.Indent();

                    // Performance comparison
                    var fpsDiff = result.AverageFPS - baseline.AverageFPS;
                    var frameTimeDiff = result.AverageFrameTime - baseline.AverageFrameTime;

                    ImGui.PushStyleColor(ImGuiCol.Text,
                        fpsDiff >= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
                    ImGui.Text($"Δ Avg FPS: {fpsDiff:+0.00;-0.00;0.00}");
                    ImGui.PopStyleColor();

                    ImGui.PushStyleColor(ImGuiCol.Text,
                        frameTimeDiff <= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
                    ImGui.Text($"Δ Frame Time: {frameTimeDiff:+0.00;-0.00;0.00}ms");
                    ImGui.PopStyleColor();

                    // CPU comparison
                    var cpuDiff = result.AverageCpuUsage - baseline.AverageCpuUsage;
                    ImGui.PushStyleColor(ImGuiCol.Text,
                        cpuDiff <= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
                    ImGui.Text($"Δ Avg CPU: {cpuDiff:+0.00;-0.00;0.00}%");
                    ImGui.PopStyleColor();

                    // Memory comparison
                    var memoryDiff = result.AverageMemoryUsageMB - baseline.AverageMemoryUsageMB;
                    ImGui.PushStyleColor(ImGuiCol.Text,
                        memoryDiff <= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
                    ImGui.Text($"Δ Avg Memory: {memoryDiff:+0;-0;0} MB");
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

            ImGui.Text("Performance:");
            ImGui.Indent();
            ImGui.Text($"Current FPS: {(1000.0f / avgFrameTime):F2}");
            ImGui.Text($"Frame Time: {avgFrameTime:F2}ms (min: {minFrameTime:F2}, max: {maxFrameTime:F2})");

            // Simple frame time graph
            if (frameTimes.Length > 1) // Ensure we have enough data for plotting
            {
                ImGui.PlotLines("Frame Times", ref frameTimes[0], frameTimes.Length, 0,
                    null, 0, maxFrameTime * 1.2f, new Vector2(0, 80));
            }

            ImGui.Unindent();
        }
        else
        {
            ImGui.Text("Collecting performance data...");
        }

        // System resource usage
        ImGui.Separator();
        ImGui.Text("System Resources:");
        ImGui.Indent();
        ImGui.Text($"CPU Usage: {_currentCpuUsage:F2}%");
        ImGui.Text($"RAM Usage: {_currentMemoryUsageMB} MB");
        ImGui.Text($"CPU Cores: {Environment.ProcessorCount}");
        ImGui.Unindent();

        // Renderer stats
        var stats2D = graphics2D.GetStats();
        ImGui.Separator();
        ImGui.Text("Renderer2D Stats:");
        ImGui.Indent();
        ImGui.Text($"Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Indices: {stats2D.GetTotalIndexCount()}");
        ImGui.Unindent();

        ImGui.End();
    }

    private void StartBenchmark(BenchmarkTestType testType)
    {
        _currentTestType = testType;
        _isRunning = true;
        _testElapsedTime = 0;
        _frameCount = 0;
        _frameTimes.Clear();
        _cpuUsageSamples.Clear();
        _memorySamples.Clear();

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
        if (_testElapsedTime >= _testDurationInSeconds)
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
        _currentTestScene = sceneFactory.Create("Benchmark");

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
            entity.AddComponent<SpriteRendererComponent>(sprite);
        }
    }

    private void SetupTextureSwitchingTest()
    {
        var textureKeys = _testTextures.Keys.ToArray();
        var random = new Random();

        for (var i = 0; i < _entityCount; i++)
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

        for (var i = 0; i < _drawCallsPerFrame; i++)
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
            if (entity.HasComponent<SpriteRendererComponent>() &&
                entity.TryGetComponent<TransformComponent>(out var transform))
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

        graphics2D.BeginScene(_cameraController.Camera);

        // Render all entities in the test scene
        foreach (var entity in _currentTestScene.Entities)
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform)) continue;

            if (entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            {
                graphics2D.DrawSprite(transform.GetTransform(), sprite, entity.Id);
            }
        }

        graphics2D.EndScene();
    }

    private void CleanupTestScene()
    {
        // Dispose scene to cleanup resources (physics, systems, etc.)
        _currentTestScene?.Dispose();
        _currentTestScene = null;
    }

    private void RecordFrameTime(float frameTimeMs)
    {
        _frameTimes.Enqueue(frameTimeMs);
        if (_frameTimes.Count > MaxFrameSamples)
            _frameTimes.Dequeue();
    }

    /// <summary>
    /// Updates CPU and memory usage metrics for monitoring and benchmarking.
    /// CPU usage is calculated as a percentage across all cores.
    /// </summary>
    private void UpdateSystemMetrics()
    {
        if (_currentProcess == null) return;

        try
        {
            // Refresh process to get latest values
            _currentProcess.Refresh();

            // Calculate CPU usage
            var currentTime = DateTime.UtcNow;
            var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

            var timeDiff = (currentTime - _lastCpuCheck).TotalMilliseconds;
            if (timeDiff > 500) // Update CPU every 500ms to smooth out readings
            {
                var cpuTimeDiff = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                var cpuUsagePercent = (float)((cpuTimeDiff / (Environment.ProcessorCount * timeDiff)) * 100.0);

                _currentCpuUsage = Math.Clamp(cpuUsagePercent, 0, 100 * Environment.ProcessorCount);

                _lastCpuCheck = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;
            }

            // Get memory usage in MB
            _currentMemoryUsageMB = _currentProcess.WorkingSet64 / (1024 * 1024);

            // Record samples during benchmark
            if (_isRunning)
            {
                _cpuUsageSamples.Enqueue(_currentCpuUsage);
                _memorySamples.Enqueue(_currentMemoryUsageMB);

                // Limit sample count to prevent unbounded growth
                if (_cpuUsageSamples.Count > 1000)
                    _cpuUsageSamples.Dequeue();
                if (_memorySamples.Count > 1000)
                    _memorySamples.Dequeue();
            }
        }
        catch (Exception ex)
        {
            // Handle cases where process info is unavailable
            Console.WriteLine($"Failed to update system metrics: {ex.Message}");
        }
    }

    private void FinalizeBenchmark()
    {
        var frameTimes = _frameTimes.ToArray();
        if (frameTimes.Length == 0) return;

        Array.Sort(frameTimes);

        // Calculate CPU and memory statistics
        var cpuSamples = _cpuUsageSamples.ToArray();
        var memorySamples = _memorySamples.ToArray();

        var result = new BenchmarkResult
        {
            TestName = _currentTestType.ToString(),
            TotalFrames = _frameCount,
            AverageFrameTime = frameTimes.Average(),
            MinFPS = 1000.0f / frameTimes.Max(),
            MaxFPS = 1000.0f / frameTimes.Min(),
            AverageFPS = 1000.0f / frameTimes.Average(),
            Percentile99 = frameTimes[(int)(frameTimes.Length * 0.99)],
            TestDuration = _testElapsedTime,

            // CPU metrics
            AverageCpuUsage = cpuSamples.Length > 0 ? cpuSamples.Average() : 0,
            MaxCpuUsage = cpuSamples.Length > 0 ? cpuSamples.Max() : 0,
            MinCpuUsage = cpuSamples.Length > 0 ? cpuSamples.Min() : 0,

            // Memory metrics
            AverageMemoryUsageMB = memorySamples.Length > 0 ? (long)memorySamples.Average() : 0,
            MaxMemoryUsageMB = memorySamples.Length > 0 ? memorySamples.Max() : 0,
            MinMemoryUsageMB = memorySamples.Length > 0 ? memorySamples.Min() : 0
        };

        // Add test-specific metrics
        switch (_currentTestType)
        {
            case BenchmarkTestType.Renderer2DStress:
                var stats = graphics2D.GetStats();
                result.CustomMetrics["Avg Draw Calls"] = stats.DrawCalls.ToString();
                result.CustomMetrics["Avg Quads"] = stats.QuadCount.ToString();
                break;
        }

        _results.Add(result);
    }

    /// <summary>
    /// Formats benchmark results as Markdown and saves to a file.
    /// Uses emoji indicators for improvements (🟢) and regressions (🔴) when comparing with baseline.
    /// </summary>
    private void ExportResultsToMarkdown()
    {
        try
        {
            var markdown = new System.Text.StringBuilder();
            markdown.AppendLine("# Benchmark Results");
            markdown.AppendLine();
            markdown.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine($"**Platform:** {Environment.OSVersion.Platform}");
            markdown.AppendLine($"**CPU Cores:** {Environment.ProcessorCount}");
            markdown.AppendLine();

            foreach (var result in _results)
            {
                markdown.AppendLine($"## {result.TestName}");
                markdown.AppendLine();

                // Performance metrics
                markdown.AppendLine("### Performance");
                markdown.AppendLine("| Metric | Value |");
                markdown.AppendLine("|--------|-------|");
                markdown.AppendLine($"| Average FPS | {result.AverageFPS:F2} |");
                markdown.AppendLine($"| Min FPS | {result.MinFPS:F2} |");
                markdown.AppendLine($"| Max FPS | {result.MaxFPS:F2} |");
                markdown.AppendLine($"| Average Frame Time | {result.AverageFrameTime:F2} ms |");
                markdown.AppendLine($"| 99th Percentile | {result.Percentile99:F2} ms |");
                markdown.AppendLine($"| Total Frames | {result.TotalFrames} |");
                markdown.AppendLine($"| Test Duration | {result.TestDuration:F2}s |");
                markdown.AppendLine();

                // CPU metrics
                markdown.AppendLine("### CPU Usage");
                markdown.AppendLine("| Metric | Value |");
                markdown.AppendLine("|--------|-------|");
                markdown.AppendLine($"| Average | {result.AverageCpuUsage:F2}% |");
                markdown.AppendLine($"| Min | {result.MinCpuUsage:F2}% |");
                markdown.AppendLine($"| Max | {result.MaxCpuUsage:F2}% |");
                markdown.AppendLine();

                // Memory metrics
                markdown.AppendLine("### Memory Usage");
                markdown.AppendLine("| Metric | Value |");
                markdown.AppendLine("|--------|-------|");
                markdown.AppendLine($"| Average | {result.AverageMemoryUsageMB} MB |");
                markdown.AppendLine($"| Min | {result.MinMemoryUsageMB} MB |");
                markdown.AppendLine($"| Max | {result.MaxMemoryUsageMB} MB |");
                markdown.AppendLine();

                // Custom metrics
                if (result.CustomMetrics.Count > 0)
                {
                    markdown.AppendLine("### Custom Metrics");
                    markdown.AppendLine("| Metric | Value |");
                    markdown.AppendLine("|--------|-------|");
                    foreach (var metric in result.CustomMetrics)
                    {
                        markdown.AppendLine($"| {metric.Key} | {metric.Value} |");
                    }

                    markdown.AppendLine();
                }

                // Baseline comparison
                var baseline = _baselineResults.FirstOrDefault(b => b.TestName == result.TestName);
                if (baseline != null)
                {
                    markdown.AppendLine("### Comparison with Baseline");
                    markdown.AppendLine("| Metric | Delta | Status |");
                    markdown.AppendLine("|--------|-------|--------|");

                    // FPS comparison
                    var fpsDiff = result.AverageFPS - baseline.AverageFPS;
                    var fpsIndicator = fpsDiff >= 0 ? "🟢" : "🔴";
                    markdown.AppendLine($"| Avg FPS | {fpsDiff:+0.00;-0.00;0.00} | {fpsIndicator} |");

                    // Frame time comparison
                    var frameTimeDiff = result.AverageFrameTime - baseline.AverageFrameTime;
                    var frameTimeIndicator = frameTimeDiff <= 0 ? "🟢" : "🔴";
                    markdown.AppendLine(
                        $"| Avg Frame Time | {frameTimeDiff:+0.00;-0.00;0.00} ms | {frameTimeIndicator} |");

                    // CPU comparison
                    var cpuDiff = result.AverageCpuUsage - baseline.AverageCpuUsage;
                    var cpuIndicator = cpuDiff <= 0 ? "🟢" : "🔴";
                    markdown.AppendLine($"| Avg CPU | {cpuDiff:+0.00;-0.00;0.00}% | {cpuIndicator} |");

                    // Memory comparison
                    var memoryDiff = result.AverageMemoryUsageMB - baseline.AverageMemoryUsageMB;
                    var memoryIndicator = memoryDiff <= 0 ? "🟢" : "🔴";
                    markdown.AppendLine($"| Avg Memory | {memoryDiff:+0;-0;0} MB | {memoryIndicator} |");

                    markdown.AppendLine();
                }

                markdown.AppendLine("---");
                markdown.AppendLine();
            }

            // Generate filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"benchmark_results_{timestamp}.md";

            // Save to file
            File.WriteAllText(filename, markdown.ToString());

            Console.WriteLine($"✓ Benchmark results exported to: {filename}");
            Console.WriteLine($"   File saved in: {Path.GetFullPath(filename)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to export results to Markdown: {ex.Message}");
        }
    }
}