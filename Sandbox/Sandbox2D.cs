using System.Numerics;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using NLog;

namespace Sandbox;

public class Sandbox2D : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    private OrthographicCameraController _cameraController;
    private Texture2D _texture;
    private Texture2D _spriteSheet;

    public Sandbox2D(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        if (_cameraController is null)
            return;
        
        _cameraController.OnUpdate(timeSpan);

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        Renderer2D.Instance.BeginScene(_cameraController.Camera);
        
        // red
        Renderer2D.Instance.DrawRotatedQuad(new Vector2(-0.5f, 0.0f), new Vector2(0.8f, 0.8f),
           45.0f ,new Vector4(0.8f, 0.2f, 0.3f, 1.0f));
        
        //blue
        Renderer2D.Instance.DrawQuad(new Vector2(-0.3f, 0.2f), new Vector2(0.5f, 0.5f),
            new Vector4(0.2f, 0.3f, 0.8f, 1.0f));
        
        //texture
        Renderer2D.Instance.DrawQuad(
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector2(10.0f, 10.0f),
            _texture);
        
        Renderer2D.Instance.EndScene();
        
        // Renderer2D.Instance.BeginScene(_cameraController.Camera);
        // Renderer2D.Instance.DrawQuad(Vector2.Zero, new Vector2(1.0f, 1.0f), _spriteSheet);
        // Renderer2D.Instance.EndScene();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);

        _cameraController.OnEvent(@event);
    }

    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");

        _cameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        _texture = TextureFactory.Create("assets/Checkerboard.png");
        _spriteSheet = TextureFactory.Create("assets/game/textures/RPGpack_sheet_2X.png");
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }
}