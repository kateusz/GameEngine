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
    EditorViewportState State { get; }
    
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
    private IFrameBuffer _frameBuffer;
    private bool _disposed;

    public EditorViewport(EditorViewportState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public required EditorViewportState State { get; init; }

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
        State.ViewportSize = new Vector2(width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleResize()
    {
        var spec = _frameBuffer.GetSpecification();
        if (State.ViewportSize is { X: > 0.0f, Y: > 0.0f } && 
            (spec.Width != (uint)State.ViewportSize.X || spec.Height != (uint)State.ViewportSize.Y))
        {
            _frameBuffer.Resize((uint)State.ViewportSize.X, (uint)State.ViewportSize.Y);
        }
    }

    public void UpdateMousePicking()
    {
        if (!State.IsMouseInViewport) return;

        var mouseX = (int)State.RelativeMousePosition.X;
        var mouseY = (int)State.RelativeMousePosition.Y;

        if (mouseX >= 0 && mouseY >= 0 && 
            mouseX < (int)State.ViewportSize.X && mouseY < (int)State.ViewportSize.Y)
        {
            var entityId = _frameBuffer.ReadPixel(1, mouseX, mouseY);
            var entity = CurrentScene.Instance.Entities.AsValueEnumerable().FirstOrDefault(x => x.Id == entityId);
            State.HoveredEntity = entity;
        }
        else
        {
            State.HoveredEntity = null;
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
        State.ViewportBounds[0] = min;
        State.ViewportBounds[1] = max;
    }

    public void SetViewportSize(Vector2 size)
    {
        State.ViewportSize = size;
    }

    public void SetHoveredState(bool hovered)
    {
        State.ViewportHovered = hovered;
    }

    public void SetFocusedState(bool focused)
    {
        State.ViewportFocused = focused;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        // FrameBuffer cleanup handled by graphics system
        _disposed = true;
    }
}