using Benchmark;

try
{
    var app = new BenchmarkApplication();
    app.Run();
}
catch (Exception e)
{
    Console.WriteLine($"Błąd aplikacji: {e.Message}");
}