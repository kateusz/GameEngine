using System.Numerics;
using System.Runtime.CompilerServices;
using ECS;
using Editor.State;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Scene;
using ZLinq;

namespace Editor.Components;

public interface IEditorViewport
{
    Vector2 ViewportSize { get; }
    bool IsHovered { get; }
    bool IsFocused { get; }
    Entity? HoveredEntity { get; }
    
    void Initialize(uint width, uint height);
    void HandleResize();
    void UpdateMousePicking();
    void BindFrameBuffer();
    void UnbindFrameBuffer();
    uint GetColorAttachmentId();
    void ClearAttachment(int index, int value);
    void UpdateViewportBounds(Vector2 min, Vector2 max);
    void SetViewportSize(Vector2 size);
    void SetHoveredState(bool hovered);
    void SetFocusedState(bool focused);
    void Dispose();
}

public class EditorViewport : IEditorViewport, IDisposable
{
    private readonly EditorViewportState _state;
    private IFrameBuffer _frameBuffer;
    private Entity? _hoveredEntity;
    private bool _disposed;

    public Vector2 ViewportSize => _state.ViewportSize;
    public bool IsHovered => _state.ViewportHovered;
    public bool IsFocused => _state.ViewportFocused;
    public Entity? HoveredEntity => _hoveredEntity;

    public EditorViewport(EditorViewportState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void Initialize(uint width, uint height)
    {
        var frameBufferSpec = new FrameBufferSpecification(width, height)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth),
            ])
        };
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);
        _state.ViewportSize = new Vector2(width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleResize()
    {
        var spec = _frameBuffer.GetSpecification();
        if (_state.ViewportSize is { X: > 0.0f, Y: > 0.0f } && 
            (spec.Width != (uint)_state.ViewportSize.X || spec.Height != (uint)_state.ViewportSize.Y))
        {
            _frameBuffer.Resize((uint)_state.ViewportSize.X, (uint)_state.ViewportSize.Y);
        }
    }

    public void UpdateMousePicking()
    {
        if (!_state.IsMouseInViewport) return;

        var mouseX = (int)_state.RelativeMousePosition.X;
        var mouseY = (int)_state.RelativeMousePosition.Y;

        if (mouseX >= 0 && mouseY >= 0 && 
            mouseX < (int)_state.ViewportSize.X && mouseY < (int)_state.ViewportSize.Y)
        {
            var entityId = _frameBuffer.ReadPixel(1, mouseX, mouseY);
            var entity = CurrentScene.Instance.Entities.AsValueEnumerable().FirstOrDefault(x => x.Id == entityId);
            _hoveredEntity = entity;
        }
        else
        {
            _hoveredEntity = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindFrameBuffer()
    {
        _frameBuffer.Bind();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindFrameBuffer()
    {
        _frameBuffer.Unbind();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetColorAttachmentId()
    {
        return _frameBuffer.GetColorAttachmentRendererId();
    }

    public void ClearAttachment(int index, int value)
    {
        _frameBuffer.ClearAttachment(index, value);
    }

    public void UpdateViewportBounds(Vector2 min, Vector2 max)
    {
        _state.ViewportBounds[0] = min;
        _state.ViewportBounds[1] = max;
    }

    public void SetViewportSize(Vector2 size)
    {
        _state.ViewportSize = size;
    }

    public void SetHoveredState(bool hovered)
    {
        _state.ViewportHovered = hovered;
    }

    public void SetFocusedState(bool focused)
    {
        _state.ViewportFocused = focused;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        // FrameBuffer cleanup handled by graphics system
        _disposed = true;
    }
}