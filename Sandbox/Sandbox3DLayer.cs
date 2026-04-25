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
using Engine.Scene.Components.Lights;
using ImGuiNET;
using Serilog;

namespace Sandbox;

public class Sandbox3DLayer(
    IGraphics3D graphics3D,
    SceneFactory sceneFactory,
    ModelSceneImporter modelSceneImporter,
    IFrameBufferFactory frameBufferFactory,
    IHdrToneMapper hdrToneMapper) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<Sandbox3DLayer>();
    private const float HdrExposure = 1.8f;

    private IScene? _scene;
    private PerspectiveCameraController? _cameraController;
    private Entity? _cameraEntity;
    private IFrameBuffer? _hdrFrameBuffer;
    private IFrameBuffer? _toneMappedFrameBuffer;
    private uint _viewportWidth = DisplayConfig.DefaultWindowWidth;
    private uint _viewportHeight = DisplayConfig.DefaultWindowHeight;
    private float _fps;
    private float _fpsTimer;
    private int _fpsFrames;

    private const string ModelPath = "assets/models/BistroExterior.fbx";

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("Sandbox3DLayer OnAttach - loading scene");

        _hdrFrameBuffer = frameBufferFactory.Create();
        _toneMappedFrameBuffer = frameBufferFactory.Create(new FrameBufferSpecification(
            DisplayConfig.DefaultWindowWidth,
            DisplayConfig.DefaultWindowHeight)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8)
            ])
        });
        hdrToneMapper.Init();

        _scene = sceneFactory.Create("Sandbox3D", "Sandbox3D");

        if (!File.Exists(ModelPath))
        {
            Logger.Error("Model not found at {Path}", ModelPath);
            _scene = null;
            return;
        }

        var result = modelSceneImporter.Import(_scene, ModelPath, addDefaultLighting: false, addCamera: true);
        Logger.Information("Scene loaded from {Path}: {MeshCount} mesh entities", ModelPath, result.MeshEntities.Count);

        var sunLight = _scene.CreateEntity("Sun_Light");
        var sunTransform = sunLight.AddComponent<TransformComponent>();
        sunTransform.Translation = new Vector3(-1.39f, 1f, 1f);
        var directionalLight = sunLight.AddComponent<DirectionalLightComponent>();
        directionalLight.Type = LightType.Directional;
        directionalLight.Direction = new Vector3(0.6f, -0.25f, 0.4f);
        directionalLight.Color = new Vector3(1f, 0.72f, 0.45f);
        directionalLight.Strength = 3.5f;

        var ambientEntity = _scene.CreateEntity("Ambient");
        var ambientLight = ambientEntity.AddComponent<AmbientLightComponent>();
        ambientLight.Type = LightType.Ambient;
        ambientLight.Color = new Vector3(0.45f, 0.55f, 0.75f);
        ambientLight.Strength = 0.35f;

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

        _scene.OnRuntimeStart();
    }

    public void OnDetach()
    {
        _scene?.OnRuntimeStop();
        _scene?.Dispose();
        _hdrFrameBuffer?.Dispose();
        _toneMappedFrameBuffer?.Dispose();
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

        if (_hdrFrameBuffer != null && _toneMappedFrameBuffer != null)
        {
            _hdrFrameBuffer.Bind();
            graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
            graphics3D.Clear();

            _scene?.OnUpdateRuntime(timeSpan);

            _hdrFrameBuffer.Unbind();
            hdrToneMapper.RenderToFramebuffer(
                _hdrFrameBuffer.GetColorAttachmentRendererId(),
                _toneMappedFrameBuffer,
                HdrExposure);
            return;
        }

        _scene?.OnUpdateRuntime(timeSpan);
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        _cameraController?.OnEvent(windowEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent && _scene != null)
        {
            _viewportWidth = (uint)resizeEvent.Width;
            _viewportHeight = (uint)resizeEvent.Height;
            _hdrFrameBuffer?.Resize(_viewportWidth, _viewportHeight);
            _toneMappedFrameBuffer?.Resize(_viewportWidth, _viewportHeight);
            _scene.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }

    public void Draw()
    {
        const float padding = 10f;
        var io = ImGui.GetIO();
        var drawList = ImGui.GetForegroundDrawList();
        if (_toneMappedFrameBuffer != null)
        {
            var imagePtr = new IntPtr(_toneMappedFrameBuffer.GetColorAttachmentRendererId());
            ImGui.GetBackgroundDrawList().AddImage(
                imagePtr,
                new Vector2(0, 0),
                io.DisplaySize,
                new Vector2(0, 1),
                new Vector2(1, 0));
        }

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
