using Engine;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox;

public class Sandbox2D : Layer
{
    private const float CameraSpeed = 1f;

    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private IVertexBuffer _vertexBuffer;
    private IIndexBuffer _indexBuffer;
    private IVertexArray _vertexArray;
    private IShader _textureShader;
    private uint[] _indices;
    private OrthographicCamera _camera;
    private Vector3 _cameraPosition = Vector3.Zero;
    private Texture2D _texture;

    public Sandbox2D(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        if (Input.KeyboardState.IsKeyDown(Keys.Left))
            _cameraPosition.X -= CameraSpeed * (float)timeSpan.TotalSeconds;
        else if (Input.KeyboardState.IsKeyDown(Keys.Right))
            _cameraPosition.X += CameraSpeed * (float)timeSpan.TotalSeconds;
        else if (Input.KeyboardState.IsKeyDown(Keys.Down))
            _cameraPosition.Y -= CameraSpeed * (float)timeSpan.TotalSeconds;
        else if (Input.KeyboardState.IsKeyDown(Keys.Up))
            _cameraPosition.Y += CameraSpeed * (float)timeSpan.TotalSeconds;

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        _camera.SetPosition(_cameraPosition);

        Renderer.Instance.BeginScene(_camera);

        _texture.Bind();

        Renderer.Instance.Submit(_textureShader, _vertexArray, Matrix4.CreateScale(1.0f));
        Renderer.Instance.EndScene();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);
        base.HandleEvent(@event);
    }

    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");

        _camera = new OrthographicCamera(-1.0f, 1.0f, -1.0f, 1.0f);
        _vertexArray = VertexArrayFactory.Create();

        float[] vertices =
        {
            // Position         Texture coordinates
            0.5f, 0.5f, 0.0f, 1.0f, 1.0f, // top right
            0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // bottom right
            -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, // bottom left
            -0.5f, 0.5f, 0.0f, 0.0f, 1.0f // top left
        };

        _vertexBuffer = VertexBufferFactory.Create(vertices);

        var layout = new BufferLayout(new[]
        {
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
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
        _textureShader = ShaderFactory.Create("Shaders/textureShader.vert", "Shaders/textureShader.frag");

        // todo: remove blocking access
        _texture = TextureFactory.Create("assets/container.png").GetAwaiter().GetResult();

        _textureShader.Bind();
        _textureShader.UploadUniformInt("u_Texture", 0);
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }
}