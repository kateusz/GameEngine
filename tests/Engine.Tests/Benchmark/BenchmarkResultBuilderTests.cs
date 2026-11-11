using Benchmark;
using Shouldly;

namespace Engine.Tests.Benchmark;

public class BenchmarkResultBuilderTests
{
    [Fact]
    public void Median_of_odd_count()
    {
        BenchmarkResultBuilder.Median([10f, 30f, 20f]).ShouldBe(20f);
    }

    [Fact]
    public void Median_of_even_count_uses_percentile_rule()
    {
        BenchmarkResultBuilder.Median([10f, 40f, 20f, 30f]).ShouldBe(20f);
    }
}
