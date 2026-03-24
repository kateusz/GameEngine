namespace Engine.Renderer.Profiling;

public class RingBuffer<T>(int capacity)
{
    private readonly T[] _buffer = new T[capacity];
    private int _head;

    public int Count { get; private set; }
    public int Capacity => capacity;

    public void Push(T value)
    {
        _buffer[_head] = value;
        _head = (_head + 1) % capacity;
        if (Count < capacity)
            Count++;
    }

    /// <summary>
    /// Index 0 = newest, Count-1 = oldest.
    /// </summary>
    public T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            // _head points to next write position, so newest is at _head - 1
            var actualIndex = ((_head - 1 - index) % capacity + capacity) % capacity;
            return _buffer[actualIndex];
        }
    }

    /// <summary>
    /// Returns items in chronological order (oldest first).
    /// </summary>
    public ReadOnlySpan<T> AsSpan()
    {
        if (Count == 0)
            return ReadOnlySpan<T>.Empty;

        var result = new T[Count];
        var start = ((_head - Count) % capacity + capacity) % capacity;
        for (var i = 0; i < Count; i++)
            result[i] = _buffer[(start + i) % capacity];

        return result;
    }

    public void Clear()
    {
        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            Array.Clear(_buffer, 0, _buffer.Length);
        _head = 0;
        Count = 0;
    }
}
