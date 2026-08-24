namespace ECS;

/// <summary>
/// Scoped lock + iteration over a component-type index. Dispose releases the lock.
/// </summary>
internal struct ComponentIndexScope(Context context)
{
    private HashSet<Entity>.Enumerator _inner;
    private bool _started;
    private bool _empty;

    internal bool MoveNext(Type primary, Type? secondary, out Entity entity)
    {
        if (!_started)
        {
            context.EnterViewIndex(primary, secondary, out _inner, out _empty);
            _started = true;
        }

        if (_empty || !_inner.MoveNext())
        {
            entity = null!;
            return false;
        }

        entity = _inner.Current;
        return true;
    }

    internal void Dispose()
    {
        if (!_started)
            return;

        if (!_empty)
            _inner.Dispose();

        context.ExitViewIndex();
        _started = false;
    }
}

/// <summary>
/// Zero-allocation view over entities with <typeparamref name="TComponent"/>.
/// </summary>
public readonly struct ComponentView<TComponent> : IEnumerable<(Entity Entity, TComponent Component)>
    where TComponent : IComponent
{
    private readonly Context _context;

    internal ComponentView(Context context) => _context = context;

    public Enumerator GetEnumerator() => new(_context);

    IEnumerator<(Entity Entity, TComponent Component)> IEnumerable<(Entity Entity, TComponent Component)>.GetEnumerator() =>
        GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<(Entity Entity, TComponent Component)>
    {
        private ComponentIndexScope _scope;

        internal Enumerator(Context context) => _scope = new ComponentIndexScope(context);

        public (Entity Entity, TComponent Component) Current { get; private set; }

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (_scope.MoveNext(typeof(TComponent), null, out var entity))
            {
                if (entity.TryGetComponent<TComponent>(out var component))
                {
                    Current = (entity, component);
                    return true;
                }
            }

            Current = default;
            return false;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose() => _scope.Dispose();
    }
}

/// <summary>
/// Zero-allocation view over entities with both component types (iterates the smaller index).
/// </summary>
public readonly struct DualComponentView<T1, T2> : IEnumerable<(Entity Entity, T1 Component1, T2 Component2)>
    where T1 : IComponent
    where T2 : IComponent
{
    private readonly Context _context;

    internal DualComponentView(Context context) => _context = context;

    public Enumerator GetEnumerator() => new(_context);

    IEnumerator<(Entity Entity, T1 Component1, T2 Component2)> IEnumerable<(Entity Entity, T1 Component1, T2 Component2)>.GetEnumerator() =>
        GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<(Entity Entity, T1 Component1, T2 Component2)>
    {
        private ComponentIndexScope _scope;

        internal Enumerator(Context context) => _scope = new ComponentIndexScope(context);

        public (Entity Entity, T1 Component1, T2 Component2) Current { get; private set; }

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (_scope.MoveNext(typeof(T1), typeof(T2), out var entity))
            {
                if (!entity.TryGetComponent<T1>(out var component1) || !entity.TryGetComponent<T2>(out var component2))
                    continue;

                Current = (entity, component1, component2);
                return true;
            }

            Current = default;
            return false;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose() => _scope.Dispose();
    }
}
