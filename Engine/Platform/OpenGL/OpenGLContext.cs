using Engine.Events;
using Engine.Renderer;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using IGraphicsContext = Engine.Renderer.IGraphicsContext;

namespace Engine.Platform.OpenGL;

public class OpenGLContext : IGraphicsContext
{
    private readonly GameWindow _gameWindow;

    private IVertexBuffer _vertexBuffer;
    private IIndexBuffer _indexBuffer;
    private IShader _shader;
    private int _vertexArrayObject;
    private uint[] _indices;

    public OpenGLContext(WindowProps props)
    {
        _gameWindow = new GameWindow(GameWindowSettings.Default,
            new NativeWindowSettings
            {
                Size = (props.Width, props.Height),
                Title = props.Title,
                Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible,
            });

        _gameWindow.Load += OnLoad;
        _gameWindow.RenderFrame += OnRenderFrame;
        _gameWindow.UpdateFrame += OnUpdateFrame;
    }

    public event Action<Event> OnEvent;

    public void Init()
    {
        _gameWindow.Run();
    }

    public void SwapBuffers()
    {
        _gameWindow.SwapBuffers();
    }

    private void OnLoad()
    {
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        float[] vertices =
        {
            -0.5f, -0.5f, 0.0f, 0.8f, 0.2f, 0.8f, 1.0f,
            0.5f, -0.5f, 0.0f, 0.2f, 0.3f, 0.8f, 1.0f,
            0.5f, 0.5f, 0.0f, 0.8f, 0.8f, 0.2f, 1.0f
        };

        _vertexBuffer = VertexBufferFactory.Create(vertices);
        _vertexBuffer.Bind();

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);
        
        var layout = new BufferLayout(new[]
        {
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color"),
        });

        _vertexBuffer.Layout = layout;
        
        for (var index = 0; index < layout.Elements.Count; index++)
        {
            var element = layout.Elements[index];
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 
                element.GetComponentCount(),
                element.Type.ToGLBaseType(),
                element.Normalized,
                layout.Stride,
                element.Offset);
        }
        
        _indices = new uint[]
        {
            0, 1, 2
        };

        _indexBuffer = IndexBufferFactory.Create(_indices, 0);
        _indexBuffer.Bind();

        // shaders
        _shader = new OpenGLShader("Shaders/shader.vert", "Shaders/shader.frag");
        _shader.Bind();
    }

    private void OnRenderFrame(FrameEventArgs e)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _shader.Bind();

        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

        var error = GL.GetError();
        SwapBuffers();
    }

    private void OnUpdateFrame(FrameEventArgs e)
    {
        var input = _gameWindow.KeyboardState;
        if (input.IsKeyDown(Keys.Escape))
        {
            _gameWindow.Close();
        }
    }

    protected void OnResize(ResizeEventArgs e)
    {
        var @event = new WindowResizeEvent(e.Width, e.Height);
        OnEvent(@event);
    }

    protected void OnKeyUp(KeyboardKeyEventArgs e)
    {
        var @event = new KeyPressedEvent((int)e.Key, 1);
        OnEvent(@event);
    }

    protected void OnKeyDown(KeyboardKeyEventArgs e)
    {
        var @event = new KeyReleasedEvent((int)e.Key);
        OnEvent(@event);
    }

    protected void OnMouseUp(MouseButtonEventArgs e)
    {
        var @event = new MouseButtonReleasedEvent((int)e.Button);
        OnEvent(@event);
    }

    protected void OnMouseDown(MouseButtonEventArgs e)
    {
        var @event = new MouseButtonPressedEvent((int)e.Button);
        OnEvent(@event);
    }

    private static void CheckError()
    {
        var error = GL.GetError();
    }
}