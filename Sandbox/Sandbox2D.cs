using Engine;
using Engine.Events;
using Engine.Renderer;
using NLog;
using OpenTK.Mathematics;

namespace Sandbox;

public class Sandbox2D : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private IVertexBuffer _vertexBuffer;
    private IIndexBuffer _indexBuffer;
    private IVertexArray _vertexArray;
    private IShader _textureShader;
    private uint[] _indices;
    private OrthographicCameraController _cameraController;
    private Texture2D _texture;

    public Sandbox2D(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController.OnUpdate(timeSpan);

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        Renderer2D.Instance.BeginScene(_cameraController.Camera);
        Renderer2D.Instance.DrawQuad(new Vector2(-0.5f, 0.0f), new Vector2(0.8f, 0.8f),
            new Vector4(0.8f, 0.2f, 0.3f, 1.0f));
        
        Renderer2D.Instance.DrawQuad(new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f),
            new Vector4(0.2f, 0.3f, 0.8f, 1.0f));
        
        //Renderer2D.Instance.DrawQuad(new Vector2(0.2f, 0.5f), new Vector2(0.5f, 0.5f), _texture);
        Renderer2D.Instance.EndScene();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);
        base.HandleEvent(@event);

        _cameraController.OnEvent(@event);
    }

    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");

        _cameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }
}