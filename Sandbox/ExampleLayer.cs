using Engine;
using Engine.Events;
using Engine.Platform.OpenGL;
using Engine.Renderer;
using NLog;
using OpenTK.Mathematics;

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

    public ExampleLayer(string name) : base(name)
    {
        OnAttach += HandleOnAttach;
        OnDetach += HandleOnDetach;
        
        _camera = new OrthographicCamera(-1.0f, 1.0f, -1.0f, 1.0f);
        //_camera = new OrthographicCamera(-2.0f, 2.0f, -2.0f, 2.0f);
        
        RendererCommand.SetClearColor(new Vector4(0.2f, 0.3f, 0.3f, 1.0f));
        RendererCommand.Clear();

        _vertexArray = VertexArrayFactory.Create();

        float[] vertices =
        {
            -0.5f, -0.5f, 0.0f, 0.8f, 0.2f, 0.8f, 1.0f,
            0.5f, -0.5f, 0.0f, 0.2f, 0.3f, 0.8f, 1.0f,
            0.5f, 0.5f, 0.0f, 0.8f, 0.8f, 0.2f, 1.0f
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
            0, 1, 2
        };

        _indexBuffer = IndexBufferFactory.Create(_indices, 3);
        _vertexArray.SetIndexBuffer(_indexBuffer);
        
        // shaders
        _shader = new OpenGLShader("Shaders/shader.vert", "Shaders/shader.frag");
        _shader.Bind();
    }

    
    public void HandleOnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");
    }

    public void HandleOnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }

    public override void OnUpdate()
    {
        Logger.Debug("ExampleLayer OnUpdate.");
        
        RendererCommand.Clear();
        
        //_camera.SetPosition(new Vector3(-0.5f, 0.0f, 0.0f));
        //_camera.SetRotation(90.0f);
        
        OpenGLRendererAPI.BeginScene(_camera);
        OpenGLRendererAPI.Submit(_shader, _vertexArray);
        OpenGLRendererAPI.EndScene();
    }

    public override void HandleEvent(Event @event)
    {
        base.HandleEvent(@event);
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);
    }
}