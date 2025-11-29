using System.Text.Json;

namespace Benchmark;

public static class BenchmarkStorage
{
    private const string BaselineFilePath = "benchmark_baseline.json";

    public static void SaveBaseline(List<BenchmarkResult> results)
    {
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(BaselineFilePath, json);
    }

    public static List<BenchmarkResult> LoadBaseline()
    {
        if (!File.Exists(BaselineFilePath))
            return [];

        var json = File.ReadAllText(BaselineFilePath);
        return JsonSerializer.Deserialize<List<BenchmarkResult>>(json) ?? new List<BenchmarkResult>();
    }
}