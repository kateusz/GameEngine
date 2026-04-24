using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;
using Serilog;

namespace Sandbox;

public class Sandbox3DLayer(
    IGraphics3D graphics3D,
    SceneFactory sceneFactory,
    IFrameBufferFactory frameBufferFactory,
    IBloomRenderer bloomRenderer,
    ModelSceneImporter modelSceneImporter) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<Sandbox3DLayer>();
    private static readonly BloomSettings ExampleBloomSettings = new(
        Enabled: true,
        Threshold: 0.85f,
        SoftKnee: 0.65f,
        Intensity: 1.25f,
        BlurPasses: 8,
        DownsampleFactor: 2,
        Exposure: 1.05f,
        Gamma: 2.2f);

    private IScene? _scene;
    private IFrameBuffer? _sceneFrameBuffer;
    private uint _viewportTextureId;
    private PerspectiveCameraController? _cameraController;
    private Entity? _cameraEntity;
    private float _fps;
    private float _fpsTimer;
    private int _fpsFrames;

    private const string ModelPath = "assets/models/BistroInterior_Wine.fbx";

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("Sandbox3DLayer OnAttach - loading scene");

        _sceneFrameBuffer = frameBufferFactory.Create(new FrameBufferSpecification(
            DisplayConfig.DefaultWindowWidth,
            DisplayConfig.DefaultWindowHeight)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA16F),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth)
            ])
        });
        bloomRenderer.Resize(DisplayConfig.DefaultWindowWidth, DisplayConfig.DefaultWindowHeight);
        _viewportTextureId = _sceneFrameBuffer.GetColorAttachmentRendererId(0);

        _scene = sceneFactory.Create("Sandbox3D", "Sandbox3D");

        if (!File.Exists(ModelPath))
        {
            Logger.Error("Model not found at {Path}", ModelPath);
            _scene = null;
            return;
        }

        var result = modelSceneImporter.Import(_scene, ModelPath, addDefaultLighting: true, addCamera: true);
        Logger.Information("Scene loaded from {Path}: {MeshCount} mesh entities", ModelPath, result.MeshEntities.Count);

        _cameraEntity = result.CameraEntity;

        var startPos = new Vector3(-4.72f, 3.39f, 22.50f);
        var initialYaw = 0f;
        if (_cameraEntity != null)
        {
            var t = _cameraEntity.GetComponent<TransformComponent>();
            
            // todo: hardcoded
            t.Translation = startPos;

            // Compute yaw to look toward scene center
            var toCenter = result.SceneCenter - startPos;
            if (toCenter.LengthSquared() > 0.001f)
                initialYaw = MathF.Atan2(-toCenter.X, -toCenter.Z);
        }

        _cameraController = new PerspectiveCameraController(startPos, initialYaw);

        _scene.OnViewportResize(DisplayConfig.DefaultWindowWidth, DisplayConfig.DefaultWindowHeight);
        _scene.OnRuntimeStart();
    }

    public void OnDetach()
    {
        _scene?.OnRuntimeStop();
        _scene?.Dispose();
        _sceneFrameBuffer?.Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController?.OnUpdate(timeSpan);

        if (_cameraEntity != null && _cameraController != null)
        {
            var transform = _cameraEntity.GetComponent<TransformComponent>();
            transform.Translation = _cameraController.Position;
            transform.Rotation = new Vector3(_cameraController.Pitch, _cameraController.Yaw, 0);
        }

        _fpsTimer += (float)timeSpan.TotalSeconds;
        _fpsFrames++;
        if (_fpsTimer >= 0.5f)
        {
            _fps = _fpsFrames / _fpsTimer;
            _fpsTimer = 0f;
            _fpsFrames = 0;
        }

        _sceneFrameBuffer?.Bind();
        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
        graphics3D.Clear();
        _scene?.OnUpdateRuntime(timeSpan);
        _sceneFrameBuffer?.Unbind();

        if (_sceneFrameBuffer is not null)
        {
            _viewportTextureId = bloomRenderer.Apply(
                _sceneFrameBuffer.GetColorAttachmentRendererId(0),
                ExampleBloomSettings);
        }
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        _cameraController?.OnEvent(windowEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent &&
            resizeEvent.Width > 0 &&
            resizeEvent.Height > 0 &&
            _scene != null)
        {
            var width = (uint)resizeEvent.Width;
            var height = (uint)resizeEvent.Height;
            _sceneFrameBuffer?.Resize(width, height);
            bloomRenderer.Resize(width, height);
            _scene.OnViewportResize(width, height);
        }
    }

    public void Draw()
    {
        const float padding = 10f;
        var io = ImGui.GetIO();
        if (_viewportTextureId != 0)
        {
            var background = ImGui.GetBackgroundDrawList();
            background.AddImage(new IntPtr(_viewportTextureId), Vector2.Zero, io.DisplaySize, new Vector2(0, 1),
                new Vector2(1, 0));
        }

        var drawList = ImGui.GetForegroundDrawList();
        var white = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));

        var fpsText = $"FPS: {_fps:F0}";
        var fpsSize = ImGui.CalcTextSize(fpsText);
        var fpsPos = new Vector2(io.DisplaySize.X - fpsSize.X - padding, padding);
        drawList.AddText(fpsPos, white, fpsText);

        if (_cameraController != null)
        {
            var p = _cameraController.Position;
            var posText = $"X: {p.X:F2}  Y: {p.Y:F2}  Z: {p.Z:F2}";
            var posSize = ImGui.CalcTextSize(posText);
            var posPos = new Vector2(io.DisplaySize.X - posSize.X - padding, fpsPos.Y + fpsSize.Y + 2f);
            drawList.AddText(posPos, white, posText);
        }
    }
}
