using Engine.Renderer;
using Engine.Renderer.Exceptions;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.Platform.SilkNet;

internal sealed class SilkNetGraphicsContext : IGraphicsContext
{
    private readonly Func<IWindow, GL> _createGl;
    private GL? _gl;

    public SilkNetGraphicsContext()
        : this(window => window.CreateOpenGL())
    {
    }

    internal SilkNetGraphicsContext(Func<IWindow, GL> createGl)
    {
        _createGl = createGl;
    }

    public bool IsCreated => _gl is not null;

    public void Create(IWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (IsCreated)
            throw new InvalidOperationException("Graphics context has already been created.");

        try
        {
            _gl = _createGl(window);
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
