using System.Numerics;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Cameras;
using ImGuiNET;
using NLog;
using Application = Engine.Core.Application;

namespace Editor;

public class EditorLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _cameraController;
    private IFrameBuffer _frameBuffer;
    private Vector2 _viewportSize;
    private bool _viewportFocused;
    private bool _viewportHovered;

    public EditorLayer(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        if (_viewportFocused)
            _cameraController.OnUpdate(timeSpan);

        _frameBuffer.Bind();

        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();

        Renderer2D.Instance.BeginScene(_cameraController.Camera);

        // red
        // Renderer2D.Instance.DrawRotatedQuad(new Vector2(-0.5f, 0.0f), new Vector2(0.8f, 0.8f),
        //     45.0f, new Vector4(0.8f, 0.2f, 0.3f, 1.0f));

        // // blue
        Renderer2D.Instance.DrawQuad(new Vector2(-1.0f, 0.0f), new Vector2(0.8f, 0.8f),
            new Vector4(0.8f, 0.2f, 0.3f, 1.0f ));
        //
        // //texture
        // Renderer2D.Instance.DrawQuad(
        //     new Vector3(0.0f, 0.0f, -0.5f),
        //     new Vector2(10.0f, 10.0f),
        //     _texture);

        Renderer2D.Instance.EndScene();

        _frameBuffer.Unbind();
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
        {
            var dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new Vector2(0.0f, 0.0f), dockspaceFlags);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Exit"))
                    {
                        // todo
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            string testString = "";
            ImGui.Begin("Settings");
            {
                ImGui.Text("Testing!");
                ImGui.InputText("##InputText", ref testString, 50);
                ImGui.End();
            }

            ImGui.Begin("Viewport");
            {
                _viewportFocused = ImGui.IsWindowFocused();
                _viewportHovered = ImGui.IsWindowHovered();
                Application.ImGuiLayer.BlockEvents = !_viewportFocused || !_viewportHovered;

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
                ImGui.Image(texturePointer, new Vector2(_viewportSize.X, _viewportSize.Y), new Vector2(0, 1),
                    new Vector2(1, 0));
                ImGui.End();
            }
        }
    }
}