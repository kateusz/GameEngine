namespace Engine.Renderer.Buffers.FrameBuffer;

public static class FrameBufferBindings
{
    /// <summary>Binds a target until the using scope ends, including exceptional exits.</summary>
    public static FrameBufferBinding BindScoped(this IFrameBuffer target) => new(target);
}

/// <summary>One balanced binding. A target cannot be bound again until this scope ends.</summary>
public readonly ref struct FrameBufferBinding
{
    private readonly IFrameBuffer _target;

    internal FrameBufferBinding(IFrameBuffer target)
    {
        target.Bind();
        _target = target;
    }

    public void Dispose() => _target.Unbind();
}
