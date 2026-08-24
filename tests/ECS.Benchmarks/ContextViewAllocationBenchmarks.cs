using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SceneComponents;
using SceneComponents.Audio;
using SceneComponents.Camera;
using SceneComponents.Lighting;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace ECS.Benchmarks;

/// <summary>
/// Measures heap allocations from <see cref="Context.View{T}()"/> snapshotting the component index
/// into a new <c>Entity[]</c> on every call. Use as baseline before zero-allocation query refactors.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(
    launchCount: 1,
    warmupCount: 2,
    iterationCount: 5,
    invocationCount: 100)]
public class ContextViewAllocationBenchmarks
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
            var entity = Entity.Create(i, $"Entity {i}");
            entity.AddComponent(new TransformComponent());

            // Mix of component densities similar to a small game scene.
            if (i % 10 == 0)
                entity.AddComponent(new CameraComponent());
            if (i % 2 == 0)
                entity.AddComponent(new SpriteRendererComponent());
            if (i % 5 == 0)
                entity.AddComponent(new RigidBody2DComponent());
            if (i % 20 == 0)
                entity.AddComponent(new AudioSourceComponent());
            if (i % 100 == 0)
                entity.AddComponent(new NativeScriptComponent());

            _context.Register(entity);
        }
    }

    /// <summary>One system querying transforms — minimum per-frame cost of a single View call.</summary>
    [Benchmark(Baseline = true)]
    public int ViewTransformSingleQuery()
    {
        var sum = 0;
        foreach (var (entity, _) in _context.View<TransformComponent>())
            sum += entity.Id;

        return sum;
    }

    /// <summary>
    /// Approximates engine frame cost: many systems each call View independently
    /// (physics, audio, render, scripts, lights, …).
    /// </summary>
    [Benchmark]
    public int ViewSimulatedFrameManyQueries()
    {
        var sum = 0;

        sum += SumView<TransformComponent>();
        sum += SumView<RigidBody2DComponent, TransformComponent>();
        sum += SumView<RigidBody2DComponent>();
        sum += SumView<RigidBody2DComponent, TransformComponent>();
        sum += SumView<RigidBody2DComponent>();
        sum += SumView<NativeScriptComponent>();
        sum += SumView<TransformComponent>();
        sum += SumView<AudioListenerComponent, TransformComponent>();
        sum += SumView<AudioSourceComponent>();
        sum += SumView<CameraComponent>();
        sum += SumView<SpriteRendererComponent, TransformComponent>();
        sum += SumView<SubTextureRendererComponent, TransformComponent>();
        sum += SumView<ModelRendererComponent, TransformComponent>();
        sum += SumView<AmbientLightComponent>();
        sum += SumView<DirectionalLightComponent>();
        sum += SumView<ModelRendererComponent, TransformComponent>();
        sum += SumView<AmbientLightComponent>();
        sum += SumView<DirectionalLightComponent>();

        return sum;
    }

    /// <summary>Dual-component View picks the smaller index but still allocates one Entity[] snapshot.</summary>
    [Benchmark]
    public int ViewDualComponentQuery()
    {
        var sum = 0;
        foreach (var (entity, _, _) in _context.View<SpriteRendererComponent, TransformComponent>())
            sum += entity.Id;

        return sum;
    }

    /// <summary>
    /// Sparse component (1 % of entities): snapshot size equals match count, not total entity count.
    /// </summary>
    [Benchmark]
    public int ViewSparseComponentQuery()
    {
        var sum = 0;
        foreach (var (entity, _) in _context.View<NativeScriptComponent>())
            sum += entity.Id;

        return sum;
    }

    private int SumView<TComponent>() where TComponent : IComponent
    {
        var sum = 0;
        foreach (var (entity, _) in _context.View<TComponent>())
            sum += entity.Id;

        return sum;
    }

    private int SumView<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
    {
        var sum = 0;
        foreach (var (entity, _, _) in _context.View<T1, T2>())
            sum += entity.Id;

        return sum;
    }
}
