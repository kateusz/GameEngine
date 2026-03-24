using Engine.Renderer.Profiling;
using Shouldly;

namespace Engine.Tests.Renderer.Profiling;

public class PerformanceProfilerTests
{
    [Fact]
    public void RegisterScope_ReturnsDifferentIdsForDifferentNames()
    {
        var profiler = new PerformanceProfiler();
        var id1 = profiler.RegisterScope("A");
        var id2 = profiler.RegisterScope("B");
        id1.ShouldNotBe(id2);
    }

    [Fact]
    public void RegisterScope_ReturnsSameIdForSameName()
    {
        var profiler = new PerformanceProfiler();
        var id1 = profiler.RegisterScope("A");
        var id2 = profiler.RegisterScope("A");
        id1.ShouldBe(id2);
    }

    [Fact]
    public void BeginScope_String_ImplicitlyRegisters()
    {
        var profiler = new PerformanceProfiler();
        profiler.Enabled = true;
        profiler.BeginFrame();
        using (var scope = profiler.BeginScope("Test"))
        {
            // scope exists
        }
        profiler.EndFrame();

        var data = profiler.GetData();
        data.RegisteredScopes.ShouldContain("Test");
    }

    [Fact]
    public void IncrementCounter_TracksValue()
    {
        var profiler = new PerformanceProfiler();
        profiler.Enabled = true;
        var id = profiler.RegisterCounter("DrawCalls");
        profiler.BeginFrame();
        profiler.IncrementCounter(id, 5);
        profiler.IncrementCounter(id, 3);
        profiler.EndFrame();

        profiler.GetData().GetCounterValue("DrawCalls").ShouldBe(8u);
    }

    [Fact]
    public void SetGauge_TracksLatestValue()
    {
        var profiler = new PerformanceProfiler();
        profiler.Enabled = true;
        var id = profiler.RegisterGauge("BatchEfficiency");
        profiler.BeginFrame();
        profiler.SetGauge(id, 0.85);
        profiler.EndFrame();

        profiler.GetData().GetGaugeValue("BatchEfficiency").ShouldBe(0.85, tolerance: 0.001);
    }

    [Fact]
    public void Disabled_BeginScope_ReturnsNoOp()
    {
        var profiler = new PerformanceProfiler();
        profiler.Enabled = false;
        profiler.BeginFrame();
        using var scope = profiler.BeginScope("Test");
        // should not throw, no-op
        profiler.EndFrame();
    }

    [Fact]
    public void EndFrame_SnapshotsToRingBuffer()
    {
        var profiler = new PerformanceProfiler();
        profiler.Enabled = true;
        profiler.RegisterCounter("C");
        profiler.BeginFrame();
        profiler.IncrementCounter("C", 10);
        profiler.EndFrame();

        profiler.BeginFrame();
        profiler.IncrementCounter("C", 20);
        profiler.EndFrame();

        var data = profiler.GetData();
        data.Latest.FrameNumber.ShouldBe(2);
        data.GetCounterValue("C").ShouldBe(20u);

        var history = data.GetHistory(2);
        history.Length.ShouldBe(2);
    }

    [Fact]
    public void ScopeTimings_RecordedCorrectly()
    {
        var profiler = new PerformanceProfiler();
        profiler.Enabled = true;
        var id = profiler.RegisterScope("Work");
        profiler.BeginFrame();
        using (var scope = profiler.BeginScope(id))
        {
            Thread.Sleep(10); // ensure measurable time
        }
        profiler.EndFrame();

        profiler.GetData().GetScopeTimingMs("Work").ShouldBeGreaterThan(0);
    }
}
