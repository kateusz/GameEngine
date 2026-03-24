using Engine.Renderer.Profiling;
using Shouldly;

namespace Engine.Tests.Renderer.Profiling;

public class RingBufferTests
{
    [Fact]
    public void Push_SingleItem_CountIsOne()
    {
        var buffer = new RingBuffer<int>(10);
        buffer.Push(42);
        buffer.Count.ShouldBe(1);
    }

    [Fact]
    public void Push_BeyondCapacity_OverwritesOldest()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Push(1);
        buffer.Push(2);
        buffer.Push(3);
        buffer.Push(4); // overwrites 1
        buffer.Count.ShouldBe(3);
        buffer[0].ShouldBe(4); // newest
        buffer[2].ShouldBe(2); // oldest
    }

    [Fact]
    public void Indexer_ZeroIsNewest()
    {
        var buffer = new RingBuffer<int>(10);
        buffer.Push(1);
        buffer.Push(2);
        buffer.Push(3);
        buffer[0].ShouldBe(3);
        buffer[1].ShouldBe(2);
        buffer[2].ShouldBe(1);
    }

    [Fact]
    public void AsSpan_ReturnsChronologicalOrder()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Push(1);
        buffer.Push(2);
        buffer.Push(3);
        buffer.Push(4); // overwrites 1
        var span = buffer.AsSpan();
        span.Length.ShouldBe(3);
        span[0].ShouldBe(2); // oldest
        span[1].ShouldBe(3);
        span[2].ShouldBe(4); // newest
    }

    [Fact]
    public void Empty_CountIsZero()
    {
        var buffer = new RingBuffer<int>(5);
        buffer.Count.ShouldBe(0);
    }

    [Fact]
    public void Capacity_ReturnsConstructorValue()
    {
        var buffer = new RingBuffer<int>(42);
        buffer.Capacity.ShouldBe(42);
    }

    [Fact]
    public void Clear_ResetsCountToZero()
    {
        var buffer = new RingBuffer<int>(5);
        buffer.Push(1);
        buffer.Push(2);
        buffer.Clear();
        buffer.Count.ShouldBe(0);
    }
}
