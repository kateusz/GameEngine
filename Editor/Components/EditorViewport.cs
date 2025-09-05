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
        
        // Apply macOS Retina scaling for high-DPI displays
        var scaleFactor = GetDisplayScaleFactor();
        var width = Math.Max(32, (uint)(State.ViewportSize.X * scaleFactor));
        var height = Math.Max(32, (uint)(State.ViewportSize.Y * scaleFactor));
        
        Console.WriteLine($"Frame buffer resize - LogicalSize: {State.ViewportSize}, ScaleFactor: {scaleFactor}, PhysicalSize: {width}x{height}");
        
        if (State.ViewportSize is { X: > 0.0f, Y: > 0.0f } && 
            (spec.Width != width || spec.Height != height))
        {
            try
            {
                _frameBuffer.Resize(width * 2, height * 2);
                
                // Update state to reflect actual resize
                State.ViewportSize = new Vector2(width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resizing framebuffer to {width}x{height}: {ex.Message}");
                
                // Fallback to a safe size if resize fails
                if (spec.Width < 32 || spec.Height < 32)
                {
                    _frameBuffer.Resize(320, 240);
                    State.ViewportSize = new Vector2(320, 240);
                }
            }
        }
    }

    public void UpdateMousePicking()
    {
        if (!State.IsMouseInViewport) return;

        // Apply Retina scaling to mouse coordinates
        var scaleFactor = GetDisplayScaleFactor();
        var mouseX = (int)(State.RelativeMousePosition.X * scaleFactor);
        var mouseY = (int)(State.RelativeMousePosition.Y * scaleFactor);

        var scaledWidth = (int)(State.ViewportSize.X * scaleFactor);
        var scaledHeight = (int)(State.ViewportSize.Y * scaleFactor);

        if (mouseX >= 0 && mouseY >= 0 && 
            mouseX < scaledWidth && mouseY < scaledHeight)
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

    private float GetDisplayScaleFactor()
    {
        // Get the actual framebuffer scale from ImGui which handles Retina displays properly
        var viewport = ImGuiNET.ImGui.GetMainViewport();
        return viewport.DpiScale;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        // FrameBuffer cleanup handled by graphics system
        _disposed = true;
    }
}