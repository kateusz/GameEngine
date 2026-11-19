using Engine.Renderer.Buffers.FrameBuffer;

namespace Engine.Renderer.Pipeline;

/// <summary>Owns directional and point-shadow GPU targets for one render host / scene.</summary>
public sealed class ShadowMapStore(IFrameBufferFactory frameBuffers) : IDisposable
{
    private IFrameBuffer? _directional;
    private readonly Dictionary<int, IFrameBuffer> _pointByEntity = new();
    private ShadowQuality? _quality;
    private bool _disposed;

    public IFrameBuffer? Directional => _directional;

    public IFrameBuffer GetOrCreateDirectional(ShadowQuality quality)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SyncQuality(quality);
        if (_directional != null)
            return _directional;

        var tile = (uint)quality.DirectionalResolution;
        var size = tile * (uint)quality.Cascades;
        var spec = new FrameBufferSpecification(size, tile)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthComponent)
                {
                    Filter = FrameBufferTextureFilter.Linear,
                    Wrap = FrameBufferTextureWrap.ClampToBorder
                }
            ])
        };
        _directional = frameBuffers.Create(spec);
        return _directional;
    }

    public IFrameBuffer GetOrCreatePoint(int entityId, ShadowQuality quality)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SyncQuality(quality);
        if (_pointByEntity.TryGetValue(entityId, out var existing))
            return existing;

        var size = (uint)quality.PointResolution;
        var spec = new FrameBufferSpecification(size, size)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthCubemap)
            ])
        };
        var map = frameBuffers.Create(spec);
        _pointByEntity[entityId] = map;
        return map;
    }

    public bool TryGetPoint(int entityId, out IFrameBuffer map) =>
        _pointByEntity.TryGetValue(entityId, out map!);

    /// <summary>Retires maps whose size no longer matches the requested quality. Idempotent.</summary>
    public void SyncQuality(ShadowQuality next)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_quality is not { } previous)
        {
            _quality = next;
            return;
        }

        if (next.DirectionalResolution != previous.DirectionalResolution
            || next.Cascades != previous.Cascades)
        {
            _directional?.Dispose();
            _directional = null;
        }

        if (next.PointResolution != previous.PointResolution)
        {
            foreach (var map in _pointByEntity.Values)
                map.Dispose();
            _pointByEntity.Clear();
        }

        _quality = next;
    }

    public void Clear()
    {
        _directional?.Dispose();
        _directional = null;
        foreach (var map in _pointByEntity.Values)
            map.Dispose();
        _pointByEntity.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Clear();
    }
}
