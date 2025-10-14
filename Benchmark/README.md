# Game Engine Benchmark Suite

Automated performance benchmarking system for the game engine with support for both interactive GUI and headless CI/CD execution.

## Features

- ✅ **Headless Mode**: Run benchmarks without GUI for CI/CD integration
- ✅ **Automated Regression Detection**: Configurable thresholds for performance monitoring
- ✅ **Multiple Test Scenarios**: Renderer stress tests, texture switching, draw call optimization
- ✅ **GitHub Actions Integration**: Automatic benchmarking on commits and PRs
- ✅ **Baseline Comparison**: Track performance over time
- ✅ **Statistical Analysis**: FPS, frame time, percentiles, and custom metrics

## Quick Start

### GUI Mode (Interactive)

```bash
dotnet run --project Benchmark
```

Use the ImGui interface to configure and run benchmarks manually.

### Headless Mode (Automated)

```bash
# Run all benchmarks
dotnet run --project Benchmark -- --headless --tests all

# Run with baseline comparison
dotnet run --project Benchmark -- --headless --tests all --baseline benchmark-baseline.json

# Run specific test
dotnet run --project Benchmark -- --headless --tests Renderer2DStress --duration 10
```

### Using Helper Script

```bash
# Save baseline
./run-benchmark.sh --save-baseline

# Compare with baseline
./run-benchmark.sh --compare-with-baseline --threshold 5

# Custom configuration
./run-benchmark.sh --tests TextureSwitching --duration 10 --entities 5000 --verbose
```

## Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--headless` | `-h` | Run without GUI | `false` |
| `--tests` | `-t` | Tests to run (comma-separated or "all") | `all` |
| `--baseline` | `-b` | Baseline file for comparison | `null` |
| `--output` | `-o` | Output JSON file path | `benchmark-results.json` |
| `--threshold` | | Regression threshold (%) | `10` |
| `--duration` | `-d` | Test duration in seconds | `5` |
| `--entities` | `-e` | Entity count for stress tests | `1000` |
| `--verbose` | `-v` | Enable verbose output | `false` |
| `--no-fail-on-regression` | | Don't exit with error on regression | `false` |
| `--help` | | Show help message | |

## Available Tests

### Renderer2DStress
Tests 2D rendering performance with many animated sprite entities.
- Configurable entity count
- Rotation animations
- Color variations
- Batch rendering stress test

### TextureSwitching
Stresses texture binding and switching performance.
- Multiple textures
- Random texture swapping
- Tests texture cache efficiency

### DrawCallOptimization
Tests draw call batching and optimization.
- Forces draw call breaks
- Different Z-depths
- Texture variations

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success - no regressions detected |
| `1` | Performance regression detected (exceeds threshold) |
| `2` | Error during execution |

## GitHub Actions Integration

The benchmark workflow automatically runs on:
- Pushes to `main` and `dev` branches
- Pull requests to `main`
- Manual workflow dispatch

### Workflow Features

1. **Automatic Execution**: Runs on every commit/PR
2. **Baseline Comparison**: Compares against saved baseline
3. **PR Comments**: Posts results as PR comments with formatted tables
4. **Artifact Upload**: Saves results for 90 days
5. **Regression Blocking**: Fails PR if regression detected
6. **Baseline Updates**: Auto-updates baseline on main branch

### Example Workflow Output

```
╔════════════════════════════════════════════════════════════╗
║        Game Engine Benchmark - Headless Mode              ║
╚════════════════════════════════════════════════════════════╝

Initializing headless benchmark runner...
Running 3 benchmark test(s)...

Running: Renderer2DStress
  Progress: [✓] 100%
    Avg FPS: 1245.67
    Avg Frame Time: 0.80ms
    Total Frames: 6228

Running: TextureSwitching
  Progress: [✓] 100%
    Avg FPS: 987.32
    Avg Frame Time: 1.01ms
    Total Frames: 4936

Running: DrawCallOptimization
  Progress: [✓] 100%
    Avg FPS: 1534.89
    Avg Frame Time: 0.65ms
    Total Frames: 7674

Results saved to: benchmark-results.json

============================================================
BENCHMARK COMPARISON RESULTS
============================================================

✓ IMPROVEMENTS:
  Renderer2DStress:
    FPS:        +45.23 (+3.8%)
    Frame Time: -0.03ms (-3.6%)

✗ REGRESSIONS DETECTED:
  TextureSwitching:
    FPS:        -123.45 (-11.1%)
    Frame Time: +0.11ms (+12.2%)
    Threshold exceeded: 12.2%

============================================================
RESULT: REGRESSION DETECTED ✗
============================================================
```

## Development Workflow

### 1. Establish Baseline

Before making changes:

```bash
git checkout main
./run-benchmark.sh --save-baseline
git add benchmark-baseline.json
git commit -m "Update benchmark baseline"
```

### 2. Make Changes

Implement your feature or optimization.

### 3. Test Performance

```bash
./run-benchmark.sh --compare-with-baseline --verbose
```

### 4. Interpret Results

- **Green (Improvements)**: Performance improved! 🎉
- **Red (Regressions)**: Performance degraded, investigate or tune threshold
- **Within Threshold**: Acceptable variance

### 5. Update Baseline (if acceptable)

If regressions are intentional or justified:

```bash
./run-benchmark.sh --save-baseline
```

## Customizing Benchmarks

### Adding New Tests

1. Add new enum value to `BenchmarkTestType.cs`:

```csharp
public enum BenchmarkTestType
{
    None,
    Renderer2DStress,
    TextureSwitching,
    DrawCallOptimization,
    MyNewTest  // Add here
}
```

2. Implement setup in `HeadlessBenchmarkRunner.cs`:

```csharp
private void SetupTestScene(BenchmarkTestType testType)
{
    // ... existing code ...
    case BenchmarkTestType.MyNewTest:
        SetupMyNewTest();
        break;
}

private void SetupMyNewTest()
{
    // Create test entities and setup
}
```

3. Implement update logic:

```csharp
private void UpdateTest(BenchmarkTestType testType, TimeSpan deltaTime)
{
    // ... existing code ...
    case BenchmarkTestType.MyNewTest:
        UpdateMyNewTest();
        break;
}
```

### Adjusting Thresholds

Different tests may have different acceptable variance:

```bash
# Strict threshold for critical rendering
./run-benchmark.sh --tests Renderer2DStress --threshold 3

# Relaxed threshold for less critical features
./run-benchmark.sh --tests TextureSwitching --threshold 15
```

## Troubleshooting

### Headless Mode Fails on macOS

macOS requires a display for OpenGL. Use GUI mode or run in Docker/VM.

### High Variance in Results

- Close other applications
- Ensure consistent power settings
- Run multiple times and average
- Increase test duration: `--duration 10`

### CI Benchmark Flakiness

GitHub Actions runners have variable performance. Consider:
- Using self-hosted runners
- Increasing regression threshold
- Running multiple iterations and averaging

### GPU Not Available in CI

Linux CI uses software rendering (Mesa). Results won't match local GPU performance but are consistent for regression detection.

## Performance Tips

### For Faster Benchmarks

```bash
# Shorter duration for quick checks
./run-benchmark.sh --duration 3

# Fewer entities for quick tests
./run-benchmark.sh --entities 500
```

### For More Accurate Results

```bash
# Longer duration for stability
./run-benchmark.sh --duration 15

# More entities for stress testing
./run-benchmark.sh --entities 10000
```

## Architecture

```
Benchmark/
├── BenchmarkConfig.cs           # CLI argument parsing
├── HeadlessBenchmarkRunner.cs   # Headless execution engine
├── BenchmarkLayer.cs            # GUI benchmark layer
├── RegressionDetector.cs        # Performance comparison logic
├── BenchmarkResult.cs           # Result data structure
├── BenchmarkStorage.cs          # JSON serialization
└── BenchmarkTestType.cs         # Test enumeration
```

## Integration Examples

### Pre-Commit Hook

```bash
#!/bin/bash
# .git/hooks/pre-push

echo "Running performance benchmarks..."
./run-benchmark.sh --compare-with-baseline --threshold 10 --duration 3

if [ $? -ne 0 ]; then
    echo "Performance regression detected! Push aborted."
    exit 1
fi
```

### Custom CI Script

```yaml
# Custom benchmark job
- name: Performance Test
  run: |
    ./run-benchmark.sh \
      --compare-with-baseline \
      --threshold 5 \
      --duration 10 \
      --output pr-benchmark.json

    # Upload to artifact storage
    aws s3 cp pr-benchmark.json s3://benchmarks/
```

## Best Practices

1. ✅ **Run benchmarks on consistent hardware**
2. ✅ **Close resource-intensive applications**
3. ✅ **Use adequate test duration** (5-10 seconds minimum)
4. ✅ **Update baseline after intentional performance changes**
5. ✅ **Document performance tradeoffs in commits**
6. ✅ **Monitor trends over time, not just single runs**
7. ✅ **Use verbose mode when investigating regressions**
8. ✅ **Test multiple scenarios (entity counts, durations)**

## Contributing

When adding performance-critical features:

1. Run baseline before changes
2. Implement feature
3. Run comparison benchmark
4. Document performance impact in PR
5. Update baseline if acceptable

---

For questions or issues, please refer to the main [project documentation](../README.md).
