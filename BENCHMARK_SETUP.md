# Benchmark System Setup Guide

## What Was Implemented

Your game engine now has a fully automated benchmarking system with the following features:

### ✅ Headless CLI Mode
- Run benchmarks without GUI for automation
- Command-line argument parsing
- Configurable test parameters
- Exit codes for CI/CD integration

### ✅ Regression Detection
- Automatic performance comparison
- Configurable threshold percentages
- Color-coded output (improvements vs regressions)
- Statistical analysis (FPS, frame time, percentiles)

### ✅ GitHub Actions Integration
- Automatic benchmarks on commits and PRs
- Baseline comparison
- PR comment with results table
- Artifact storage for 90 days
- Automatic baseline updates on main branch

### ✅ Helper Scripts
- `run-benchmark.sh` for easy local execution
- Save/load baseline workflows
- Verbose output for debugging

---

## Quick Start

### 1. Run Your First Benchmark

```bash
# Run in GUI mode (interactive)
dotnet run --project Benchmark

# Run in headless mode
./run-benchmark.sh --tests all
```

### 2. Save a Baseline

Before making any changes to the engine:

```bash
./run-benchmark.sh --save-baseline
git add benchmark-baseline.json
git commit -m "Add benchmark baseline"
```

### 3. Make Changes to Your Engine

Implement your feature or optimization...

### 4. Compare Performance

```bash
./run-benchmark.sh --compare-with-baseline --verbose
```

You'll see output like:

```
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

---

## GitHub Actions Integration

The system automatically runs on:
- **Pushes to `main` and `dev`**: Updates baseline
- **Pull Requests to `main`**: Compares against baseline and comments on PR
- **Manual trigger**: Via GitHub Actions UI

### What Happens Automatically

1. **On PR**:
   - Runs benchmarks
   - Compares with baseline
   - Posts comment with results table
   - **Fails PR** if regression exceeds threshold

2. **On Main Branch Push**:
   - Runs benchmarks
   - Updates `benchmark-baseline.json`
   - Commits new baseline automatically

### Viewing Results

- Check PR comments for benchmark tables
- Download artifacts from Actions tab
- View workflow logs for detailed output

---

## Command Examples

### Basic Usage

```bash
# Run all tests (5 seconds each, 1000 entities)
./run-benchmark.sh

# Run with verbose output
./run-benchmark.sh --verbose

# Run specific test only
./run-benchmark.sh --tests Renderer2DStress
```

### Advanced Usage

```bash
# Stress test with many entities
./run-benchmark.sh --entities 10000 --duration 10

# Compare with strict threshold
./run-benchmark.sh --compare-with-baseline --threshold 3

# Run multiple specific tests
dotnet run --project Benchmark -- --headless --tests "Renderer2DStress,TextureSwitching"

# Save results to custom file
./run-benchmark.sh --output my-benchmark.json
```

### Development Workflow

```bash
# 1. Save current performance as baseline
git checkout main
./run-benchmark.sh --save-baseline

# 2. Create feature branch
git checkout -b feature/optimize-renderer

# 3. Make changes to engine...

# 4. Test performance
./run-benchmark.sh --compare-with-baseline

# 5. If good, commit changes
git add .
git commit -m "Optimize renderer batching (+15% FPS)"

# 6. Push - CI will run benchmarks automatically
git push origin feature/optimize-renderer
```

---

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `--tests` | `all` | Which tests to run |
| `--duration` | `5` | Seconds per test |
| `--entities` | `1000` | Entity count for stress tests |
| `--threshold` | `10` | Regression threshold (%) |
| `--output` | `benchmark-results.json` | Output file path |
| `--baseline` | (none) | Baseline file for comparison |

---

## Understanding Results

### Key Metrics

- **Average FPS**: Target metric for performance
- **Average Frame Time**: Milliseconds per frame (lower is better)
- **Min/Max FPS**: Performance stability indicators
- **99th Percentile**: Worst-case frame time (important for smoothness)

### What's a Regression?

A regression occurs when:
- **FPS drops** by more than threshold percentage, OR
- **Frame time increases** by more than threshold percentage

Example with 10% threshold:
- Baseline: 1000 FPS
- Current: 890 FPS
- Drop: 11% → **REGRESSION**

### Threshold Tuning

- **Strict (3-5%)**: For critical rendering systems
- **Normal (10%)**: For general development
- **Relaxed (15-20%)**: For less critical features

---

## Troubleshooting

### Build Fails

```bash
dotnet clean
dotnet restore
dotnet build
```

### Headless Mode Doesn't Work on macOS

macOS requires a display for OpenGL. Options:
1. Use GUI mode for local testing
2. Run in Docker/VM
3. Use GitHub Actions (Linux-based)

### High Variance in Results

- Close other applications
- Ensure power/performance settings are consistent
- Increase test duration: `--duration 10`
- Run multiple times and average

### GitHub Actions Fails

Check workflow logs:
1. Go to GitHub → Actions tab
2. Click failed workflow
3. Expand "Run benchmarks" step
4. Check for OpenGL/display errors

Common fixes:
- Ensure `xvfb` is installed (already in workflow)
- Check `DISPLAY` environment variable

---

## Project Structure

```
Benchmark/
├── BenchmarkConfig.cs              # CLI argument parsing
├── HeadlessBenchmarkRunner.cs     # Headless execution engine
├── BenchmarkLayer.cs               # GUI benchmark layer
├── RegressionDetector.cs           # Performance comparison
├── BenchmarkResult.cs              # Result data structure
├── BenchmarkStorage.cs             # JSON persistence
├── BenchmarkTestType.cs            # Test enumeration
├── Program.cs                      # Entry point (GUI/headless)
└── README.md                       # Detailed documentation

.github/workflows/
└── benchmark.yml                   # GitHub Actions workflow

run-benchmark.sh                    # Helper script
benchmark-baseline.json             # Baseline results (git tracked)
benchmark-results.json              # Latest results (git ignored)
```

---

## Next Steps

### Optional Enhancements

1. **Add More Tests**:
   - ECS iteration performance
   - Physics simulation stress test
   - Asset loading benchmarks

2. **Advanced Analytics**:
   - Historical trend graphs
   - Performance dashboard
   - Email notifications on regression

3. **BenchmarkDotNet Integration**:
   - Micro-benchmarks for specific functions
   - Memory allocation tracking
   - Statistical analysis

4. **Cross-Platform Testing**:
   - macOS benchmarks
   - Windows benchmarks
   - Performance comparison across platforms

### Maintenance

- **Update Baseline**: After intentional performance changes
- **Review Thresholds**: Adjust based on variance in results
- **Monitor Trends**: Track performance over time
- **Document Changes**: Note performance impact in commit messages

---

## Getting Help

- **Detailed Documentation**: See `Benchmark/README.md`
- **GitHub Actions Logs**: Check workflow output for errors
- **Verbose Mode**: Use `--verbose` flag for debugging
- **Test Locally First**: Before relying on CI results

---

## Summary

You now have a complete automated benchmarking system! 🎉

**Local Development**:
```bash
./run-benchmark.sh --compare-with-baseline
```

**CI/CD**:
- Automatic on every PR
- Regression detection
- Baseline management

**Key Files**:
- `run-benchmark.sh` - Main entry point
- `benchmark-baseline.json` - Reference performance
- `.github/workflows/benchmark.yml` - CI configuration

Happy optimizing! 🚀
