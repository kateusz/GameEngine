using System.Numerics;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using ImGuiNET;
using NLog;

namespace Sandbox;

public class Sandbox2DLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _orthographicCameraController;
    private Texture2D _spriteSheet;
    private SubTexture2D _textureBarrel;

    public Sandbox2DLayer(string name) : base(name)
    {
    }
    
    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");

        _orthographicCameraController = new OrthographicCameraController(1280.0f / 720.0f, true);
        _spriteSheet = TextureFactory.Create("assets/game/textures/RPGpack_sheet_2X.png");
        
        _textureBarrel = SubTexture2D.CreateFromCoords(_spriteSheet, new Vector2(8, 2), new Vector2(128, 128), new Vector2(1, 1));
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        _orthographicCameraController.OnUpdate(timeSpan);

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        Renderer2D.Instance.BeginScene(_orthographicCameraController.Camera);

        // sub texture
        Renderer2D.Instance.DrawQuad(new Vector3(2.0f, 2.0f, 0.5f), new Vector2(1, 1), rotation: 0, _textureBarrel);

        // yellow
        Renderer2D.Instance.DrawQuad(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector4(0.8f, 0.8f, 0.3f, 1.0f));
        
        // red
        Renderer2D.Instance.DrawQuad(new Vector2(-1.0f, 0.0f), new Vector2(0.8f, 0.8f),
            new Vector4(0.8f, 0.2f, 0.3f, 1.0f));
        
        Renderer2D.Instance.EndScene();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);

        _orthographicCameraController.OnEvent(@event);
    }

    public override void OnImGuiRender()
    {
        SubmitUI();
    }
    
    private void SubmitUI()
    {
        //ImGui.ShowDemoWindow();
    }
}