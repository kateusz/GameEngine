namespace ECS;

public readonly struct ComponentView<TComponent>(HashSet<Entity> entities)
    where TComponent : IComponent
{
    public Enumerator GetEnumerator() => new(entities);

    public struct Enumerator(HashSet<Entity> entities)
    {
        private HashSet<Entity>.Enumerator _inner = entities.GetEnumerator();

        public (Entity Entity, TComponent Component) Current { get; private set; }

        public bool MoveNext()
        {
            while (_inner.MoveNext())
            {
                if (_inner.Current.TryGetComponent<TComponent>(out var component))
                {
                    Current = (_inner.Current, component);
                    return true;
                }
            }

            Current = default;
            return false;
        }

        public void Dispose() => _inner.Dispose();
    }
}

public readonly struct DualComponentView<T1, T2>(HashSet<Entity> entities)
    where T1 : IComponent
    where T2 : IComponent
{
    public Enumerator GetEnumerator() => new(entities);

    public struct Enumerator(HashSet<Entity> entities)
    {
        private HashSet<Entity>.Enumerator _inner = entities.GetEnumerator();

        public (Entity Entity, T1 Component1, T2 Component2) Current { get; private set; }

        public bool MoveNext()
        {
            while (_inner.MoveNext())
            {
                if (!_inner.Current.TryGetComponent<T1>(out var component1)
                    || !_inner.Current.TryGetComponent<T2>(out var component2))
                    continue;

                Current = (_inner.Current, component1, component2);
                return true;
            }

            Current = default;
            return false;
        }

        public void Dispose() => _inner.Dispose();
    }
}
