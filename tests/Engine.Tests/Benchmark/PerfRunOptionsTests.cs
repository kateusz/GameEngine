using Benchmark.Cli;
using Shouldly;

namespace Engine.Tests.Benchmark;

public class PerfRunOptionsTests
{
    [Fact]
    public void TryParse_requires_output()
    {
        var ok = PerfRunOptions.TryParse(["--perf-run", "--scene", "3D:InstancedMesh"], out _, out var err);
        ok.ShouldBeFalse();
        err.ShouldContain("output");
    }

    [Fact]
    public void TryParse_accepts_scene_shorthand()
    {
        var ok = PerfRunOptions.TryParse(
        [
            "--perf-run",
            "--scene", "3D:InstancedMesh",
            "--output", "/tmp/out.json",
            "--measure-seconds", "10",
            "--warmup-seconds", "5"
        ],
            out var options,
            out _);

        ok.ShouldBeTrue();
        options!.Category.ShouldBe("3D");
        options.TestId.ShouldBe("InstancedMesh");
        options.MeasureSeconds.ShouldBe(10f);
    }

    [Fact]
    public void TryParse_defaults_warmup_measure_iterations()
    {
        var ok = PerfRunOptions.TryParse(
        [
            "--perf-run",
            "--scene", "3D:InstancedMesh",
            "--output", "/tmp/out.json"
        ],
            out var options,
            out _);

        ok.ShouldBeTrue();
        options!.WarmupSeconds.ShouldBe(5f);
        options.MeasureSeconds.ShouldBe(5f);
        options.MeasureIterations.ShouldBe(5);
    }

    [Fact]
    public void TryParse_measure_iterations_flag()
    {
        var ok = PerfRunOptions.TryParse(
        [
            "--perf-run",
            "--scene", "3D:InstancedMesh",
            "--output", "/tmp/out.json",
            "--measure-iterations", "3"
        ],
            out var options,
            out _);

        ok.ShouldBeTrue();
        options!.MeasureIterations.ShouldBe(3);
    }
}
