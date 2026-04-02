using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Engine.Scene.Components;

namespace ECS.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ContextBenchmarks
{
    private Context _context = null!;

    [Params(100, 1_000, 10_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _context = new Context();

        for (var i = 0; i < EntityCount; i++)
        {
            var entity =  Entity.Create(i, $"Entity {i}");
            entity.AddComponent(new TransformComponent());
            entity.AddComponent(new CameraComponent());

            _context.Register(entity);
        }
    }

    [Benchmark]
    public void GetById()
    {
        var id = EntityCount / 2;
        _ = _context.GetById(id);
    }

    [Benchmark]
    public void View()
    {
        foreach (var (_, position) in _context.View<TransformComponent>())
        {
            Consume(position);
        }
    }

    private static void Consume<T>(T value)
    {
        // Prevents JIT from optimizing away the loop
        _ = value?.GetHashCode();
    }
}