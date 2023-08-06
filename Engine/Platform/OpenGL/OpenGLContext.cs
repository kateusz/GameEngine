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

    public OpenGLContext(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
        _gameWindow.Load += OnLoad;
        _gameWindow.RenderFrame += OnRenderFrame;
        _gameWindow.UpdateFrame += OnUpdateFrame;
    }
    
    public event Action<Event> OnEvent;
    
    public void Init()
    {
        _gameWindow.Run();
        // chyba niepotrzebne przy opentk?
    }

    public void SwapBuffers()
    {
        _gameWindow.SwapBuffers();
    }

    private void OnLoad()
    {
        // This will be the color of the background after we clear it, in normalized colors.
        // Normalized colors are mapped on a range of 0.0 to 1.0, with 0.0 representing black, and 1.0 representing
        // the largest possible value for that channel.
        // This is a deep green.
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        
        float[] vertices =
        {
            -0.5f, -0.5f, 0.0f, // Bottom-left vertex
            0.5f, -0.5f, 0.0f, // Bottom-right vertex
            0.0f, 0.5f, 0.0f // Top vertex
        };

        int[] indices = new int[] { 0, 1, 2 };

        _vertexBuffer = VertexBufferFactory.Create(vertices);

        // vertex array
        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        // Enable variable 0 in the shader.
        GL.EnableVertexAttribArray(0);

        _indexBuffer = IndexBufferFactory.Create(indices, 3);

        // shaders
        _shader = new OpenGLShader("Shaders/shader.vert", "Shaders/shader.frag");

        // Now, enable the shader.
        // Just like the VBO, this is global, so every function that uses a shader will modify this one until a new one is bound instead.
        _shader.Bind();
    }

    private void OnUpdateFrame(FrameEventArgs e)
    {
        var input = _gameWindow.KeyboardState;
        if (input.IsKeyDown(Keys.Escape))
        {
            _gameWindow.Close();
        }
    }
    
    // Now that initialization is done, let's create our render loop.
    private void OnRenderFrame(FrameEventArgs e)
    {
        // This clears the image, using what you set as GL.ClearColor earlier.
        // OpenGL provides several different types of data that can be rendered.
        // You can clear multiple buffers by using multiple bit flags.
        // However, we only modify the color, so ColorBufferBit is all we need to clear.
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // To draw an object in OpenGL, it's typically as simple as binding your shader,
        // setting shader uniforms (not done here, will be shown in a future tutorial)
        // binding the VAO,
        // and then calling an OpenGL function to render.

        // Bind the shader
        _shader.Bind();

        // Bind the VAO
        GL.BindVertexArray(_vertexArrayObject);

        // And then call our drawing function.
        // For this tutorial, we'll use GL.DrawArrays, which is a very simple rendering function.
        // Arguments:
        //   Primitive type; What sort of geometric primitive the vertices represent.
        //     OpenGL used to support many different primitive types, but almost all of the ones still supported
        //     is some variant of a triangle. Since we just want a single triangle, we use Triangles.
        //   Starting index; this is just the start of the data you want to draw. 0 here.
        //   How many vertices you want to draw. 3 for a triangle.
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // OpenTK windows are what's known as "double-buffered". In essence, the window manages two buffers.
        // One is rendered to while the other is currently displayed by the window.
        // This avoids screen tearing, a visual artifact that can happen if the buffer is modified while being displayed.
        // After drawing, call this function to swap the buffers. If you don't, it won't display what you've rendered.

        SwapBuffers();

        // And that's all you have to do for rendering! You should now see a yellow triangle on a black screen.
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
}