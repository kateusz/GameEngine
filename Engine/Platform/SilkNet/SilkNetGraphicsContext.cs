using Engine.Renderer;
using Engine.Renderer.Exceptions;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.Platform.SilkNet;

internal sealed class SilkNetGraphicsContext : IGraphicsContext
{
    private readonly IWindow _window;
    private readonly Func<IWindow, GL> _createGl;
    private GL? _gl;

    public SilkNetGraphicsContext(IWindow window)
        : this(window, w => w.CreateOpenGL())
    {
    }

    internal SilkNetGraphicsContext(IWindow window, Func<IWindow, GL> createGl)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(createGl);
        _window = window;
        _createGl = createGl;
    }

    public bool IsCreated => _gl is not null;

    public void Create()
    {
        if (IsCreated)
            throw new InvalidOperationException("Graphics context has already been created.");

        try
        {
            _gl = _createGl(_window);
            SilkNetContext.GL = _gl;
        }
        catch (Exception ex)
        {
            throw new RendererInitializationException("Failed to create OpenGL graphics context.", ex);
        }
    }

    public void Dispose()
    {
        if (_gl is null)
            return;

        var gl = _gl;
        _gl = null;
        gl.Dispose();

        if (ReferenceEquals(SilkNetContext.GL, gl))
            SilkNetContext.GL = null!;
    }
}
