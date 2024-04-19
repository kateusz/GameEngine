using System.Numerics;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using ImGuiNET;
using NLog;

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _cameraController;
    private Texture2D _texture;
    private Texture2D _spriteSheet;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;

    public EditorLayer(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController.OnUpdate(timeSpan);
        
        _frameBuffer.Bind();
        
        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

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
        _texture = TextureFactory.Create("assets/textures/Checkerboard.png");
        //_spriteSheet = TextureFactory.Create("assets/game/textures/RPGpack_sheet_2X.png");

        var frameBufferSpec = new FrameBufferSpecification(1280, 720);
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
    

    private void SubmitUI()
    {
        var dockspaceOpen = true;
        const bool fullscreenPersistant = true;
        var dockspaceFlags = ImGuiDockNodeFlags.None;

        var windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        if (fullscreenPersistant)
        {
            var viewPort = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewPort.Pos);
            ImGui.SetNextWindowSize(viewPort.Size);
            ImGui.SetNextWindowViewport(viewPort.ID);
        }

        ImGui.Begin("DockSpace Demo", ref dockspaceOpen, windowFlags);

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
        ImGui.End();
        
        ImGui.Begin("Viewport");
        var viewportPanelSize = ImGui.GetContentRegionAvail();
        if (_viewportSize != viewportPanelSize)
        {
            _frameBuffer.Resize((uint)viewportPanelSize.X, (uint)viewportPanelSize.Y);
            _viewportSize = new Vector2(viewportPanelSize.X, viewportPanelSize.Y);

            var @resizeEvent = new WindowResizeEvent((int)viewportPanelSize.X, (int)viewportPanelSize.Y);
            _cameraController.OnEvent(@resizeEvent);
        }
        
        var textureId = _frameBuffer.GetColorAttachmentRendererId();
        var texturePointer = new IntPtr(textureId);
        ImGui.Image(texturePointer, new Vector2(_viewportSize.X, _viewportSize.Y), new Vector2(0, 1), new Vector2(1, 0));
        ImGui.End();

        // var textureId = _frameBuffer.GetColorAttachmentRendererId();
        // var texturePointer = new IntPtr(textureId);
        //
        // ImGui.Image(texturePointer, new Vector2(800, 600));

        ImGui.End();
        ImGui.End();
    }
}