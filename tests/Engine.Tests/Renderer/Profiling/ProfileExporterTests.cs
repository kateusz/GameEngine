using Engine.Renderer.Profiling;
using Shouldly;

namespace Engine.Tests.Renderer.Profiling;

public class ProfileExporterTests
{
    [Fact]
    public void ExportCsv_WritesHeaderAndRows()
    {
        var profiler = new PerformanceProfiler(10);
        profiler.Enabled = true;
        profiler.RegisterCounter("DrawCalls");
        profiler.RegisterScope("Test");

        profiler.BeginFrame();
        profiler.IncrementCounter("DrawCalls", 5);
        profiler.EndFrame();

        var exporter = new ProfileExporter(profiler);
        var csv = exporter.ExportCsvString(1);

        csv.ShouldContain("FrameNumber");
        csv.ShouldContain("DrawCalls");
        csv.ShouldContain("Test");
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(2); // header + 1 data row
    }

    [Fact]
    public void ExportJson_ProducesValidJson()
    {
        var profiler = new PerformanceProfiler(10);
        profiler.Enabled = true;
        profiler.RegisterCounter("DrawCalls");

        profiler.BeginFrame();
        profiler.IncrementCounter("DrawCalls", 3);
        profiler.EndFrame();

        var exporter = new ProfileExporter(profiler);
        var json = exporter.ExportJsonString(1);

        json.ShouldStartWith("[");
        json.ShouldEndWith("]");
        json.ShouldContain("DrawCalls");
    }
}
