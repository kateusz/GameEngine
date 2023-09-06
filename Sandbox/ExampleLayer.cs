using Engine;
using Engine.Core;
using Engine.Events;
using Engine.Platform.OpenGL;
using Engine.Renderer;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox;

public class ExampleLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private IVertexBuffer _vertexBuffer;
    private IIndexBuffer _indexBuffer;
    private IShader _shader;
    private IVertexArray _vertexArray;
    private IVertexArray _squareVertexArray;
    private uint[] _indices;
    private OrthographicCamera _camera;
    private Vector3 _cameraPosition = Vector3.Zero;
    private float _cameraSpeed = 0.01f;
    

    public ExampleLayer(string name) : base(name)
    {
        OnAttach += HandleOnAttach;
        OnDetach += HandleOnDetach;

        _camera = new OrthographicCamera(-1.0f, 1.0f, -1.0f, 1.0f);
        RendererCommand.SetClearColor(new Vector4(0.2f, 0.3f, 0.3f, 1.0f));
        RendererCommand.Clear();

        _vertexArray = VertexArrayFactory.Create();

        float[] vertices =
        {
            0.5f, 0.5f, 0.0f, 0.8f, 0.2f, 0.8f, 1.0f,
            0.5f, -0.5f, 0.0f, 0.2f, 0.3f, 0.8f, 1.0f,
            -0.5f, -0.5f, 0.0f, 0.8f, 0.8f, 0.2f, 1.0f,
            -0.5f,  0.5f, 0.0f, 0.2f, 0.3f, 0.8f, 1.0f,
        };

        _vertexBuffer = VertexBufferFactory.Create(vertices);

        var layout = new BufferLayout(new[]
        {
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color"),
        });

        _vertexBuffer.SetLayout(layout);
        _vertexArray.AddVertexBuffer(_vertexBuffer);

        _indices = new uint[]
        {
            0, 1, 3,
            1, 2, 3
        };

        _indexBuffer = IndexBufferFactory.Create(_indices, 6);
        _vertexArray.SetIndexBuffer(_indexBuffer);

        // shaders
        _shader = new OpenGLShader("Shaders/shader.vert", "Shaders/shader.frag");
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        Logger.Debug("ExampleLayer OnUpdate. Time: {0}s {1}ms", timeSpan.TotalSeconds, timeSpan.TotalMilliseconds);

        if (Input.KeyboardState.IsKeyPressed(Keys.Left))
            _cameraPosition.X -= _cameraSpeed * (float)timeSpan.TotalSeconds;
        else if (Input.KeyboardState.IsKeyPressed(Keys.Right))
            _cameraPosition.X += _cameraSpeed * (float)timeSpan.TotalSeconds;
        else if (Input.KeyboardState.IsKeyPressed(Keys.Down))
            _cameraPosition.Y -= _cameraSpeed * (float)timeSpan.TotalSeconds;
        else if (Input.KeyboardState.IsKeyPressed(Keys.Up))
            _cameraPosition.Y += _cameraSpeed * (float)timeSpan.TotalSeconds;

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        _camera.SetPosition(_cameraPosition);

        OpenGLRendererAPI.BeginScene(_camera);
        OpenGLRendererAPI.Submit(_shader, _vertexArray);
        OpenGLRendererAPI.EndScene();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);
        base.HandleEvent(@event);
    }

    public void HandleOnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");
    }

    public void HandleOnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }
}