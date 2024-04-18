using System.Numerics;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using ImGuiNET;
using NLog;

namespace Sandbox;

public class Sandbox2DLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _cameraController;
    private Texture2D _texture;
    private Texture2D _spriteSheet;
    private IFrameBuffer _frameBuffer;

    public Sandbox2DLayer(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController.OnUpdate(timeSpan);

        ResetStats();

        Renderer2D.Instance.BeginScene(_cameraController.Camera);

        // red
        Renderer2D.Instance.DrawRotatedQuad(new Vector2(-0.5f, 0.0f), new Vector2(0.8f, 0.8f),
            45.0f, new Vector4(0.8f, 0.2f, 0.3f, 1.0f));

        // // blue
        Renderer2D.Instance.DrawQuad(new Vector2(-0.3f, 0.2f), new Vector2(0.5f, 0.5f),
            new Vector4(0.2f, 0.3f, 0.8f, 1.0f));
        //
        // //texture
        Renderer2D.Instance.DrawQuad(
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector2(10.0f, 10.0f),
            _texture);

        Renderer2D.Instance.EndScene();
        
        _frameBuffer.Unbind();

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
        //_spriteSheet = TextureFactory.Create("assets/game/textures/RPGpack_sheet_2X.png");

        var frameBufferSpec = new FramebufferSpecification(1280, 720);
        _frameBuffer = FrameBufferFactory.Create(frameBufferSpec);
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }

    public override void OnImGuiRender()
    {
        SubmitUI();
    }
    
    public void ResetStats()
    {
        _frameBuffer.Bind();
        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();
    }

    private void SubmitUI()
    {
        unsafe
        {
            var dockspaceOpen = true;
            var fullscreenPersistant = true;
            var dockspaceFlags = ImGuiDockNodeFlags.None;

            var windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
            if (fullscreenPersistant)
            {
                var viewPort = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(viewPort.Pos);
                ImGui.SetNextWindowSize(viewPort.Size);
                ImGui.SetNextWindowViewport(viewPort.ID);
            }

            ImGui.Begin("Dockspace demo", ref dockspaceOpen, windowFlags);

            var io = ImGui.GetIO();

            var dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new Vector2(0.0f, 0.0f), dockspaceFlags);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Exit"))
                    {
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            ImGui.Begin("Settings");
            ImGui.Text("testujemy");

            //ImGui.ColorEdit4("Square color", )

            //var textureId = _frameBuffer.GetColorAttachmentRendererId();
            var textureId = _texture.GetRendererId();
            var texturePointer = new IntPtr(textureId++);
            ImGui.Image(texturePointer, new Vector2(1280, 720));

            ImGui.End();
            ImGui.End();
        }
    }
}