using SceneComponents.Lighting;
using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Rendering;
using Serilog;

namespace Sandbox;

public class Sandbox3DLayer(
    IGraphics3D graphics3D,
    SceneFactory sceneFactory,
    IFrameBufferFactory frameBufferFactory,
    PostProcessOrchestrator postProcessOrchestrator) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<Sandbox3DLayer>();

    private IScene? _scene;
    private PerspectiveCameraController? _cameraController;
    private Entity? _cameraEntity;
    private IFrameBuffer? _hdrFrameBuffer;
    private IFrameBuffer? _sdrFrameBuffer;
    private float _fps;
    private float _fpsTimer;
    private int _fpsFrames;

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("Sandbox3DLayer OnAttach - creating cube scene");

        _scene = sceneFactory.Create("Sandbox3D", "Sandbox3D");

        var cameraEntity = _scene.CreateEntity("Camera");
        var cameraTransform = cameraEntity.AddComponent<TransformComponent>();
        cameraTransform.Translation = new Vector3(0f, 2f, 5f);
        var cameraComponent = cameraEntity.AddComponent<CameraComponent>();
        cameraComponent.Primary = true;
        cameraComponent.ProjectionType = CameraProjectionTypeData.Perspective;
        cameraComponent.PerspectiveFOV = MathF.PI / 4f;
        cameraComponent.PerspectiveNear = 0.1f;
        cameraComponent.PerspectiveFar = 100f;
        _cameraEntity = cameraEntity;

        var cubeEntity = _scene.CreateEntity("Cube");
        cubeEntity.AddComponent<TransformComponent>();
        cubeEntity.AddComponent<ModelRendererComponent>(new ModelRendererComponent(Vector4.One));

        var floorEntity = _scene.CreateEntity("Floor");
        var floorTransform = floorEntity.AddComponent<TransformComponent>();
        floorTransform.Translation = new Vector3(0f, -0.55f, 0f);
        floorTransform.Scale = new Vector3(20f, 0.1f, 20f);
        floorEntity.AddComponent<ModelRendererComponent>(new ModelRendererComponent(new Vector4(0.82f, 0.82f, 0.8f, 1f)));

        var skyEntity = _scene.CreateEntity("Sky");
        var skyLight = skyEntity.AddComponent<SkyLightComponent>();
        skyLight.HdrPath = "assets/textures/skies/sky_1k.hdr";

        var sunEntity = _scene.CreateEntity("Sun");
        var sun = sunEntity.AddComponent<DirectionalLightComponent>();
        sun.Direction = new Vector3(-0.4f, -0.8f, -0.3f);
        sun.Color = new Vector3(1f, 0.95f, 0.9f);

        var lamp = _scene.CreateEntity("Point Light");
        var lampTransform = lamp.AddComponent<TransformComponent>();
        lampTransform.Translation = new Vector3(0f, 2.5f, 0f);
        lamp.AddComponent<PointLightComponent>();

        _hdrFrameBuffer = frameBufferFactory.Create();
        _sdrFrameBuffer = frameBufferFactory.Create(new FrameBufferSpecification(
            DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8),
            ])
        });
        postProcessOrchestrator.Initialize();

        for (var m = 0; m < 3; m++)
        {
            for (var r = 0; r < 3; r++)
            {
                var sphere = _scene.CreateEntity($"Sphere m{m} r{r}");
                var sphereTransform = sphere.AddComponent<TransformComponent>();
                sphereTransform.Translation = new Vector3((m - 1) * 1.5f, 0.75f + r * 1.5f, -2f);
                var renderer = sphere.AddComponent<ModelRendererComponent>(new ModelRendererComponent(Vector4.One));
                renderer.ModelPath = "builtin:sphere";
                renderer.MetallicOverride = m / 2f;
                renderer.RoughnessOverride = 0.1f + 0.4f * r;
            }
        }

        _cameraController = new PerspectiveCameraController(new Vector3(0f, 2f, 5f), 0f);
        _scene.OnRuntimeStart();
    }

    public void OnDetach()
    {
        _scene?.OnRuntimeStop();
        _scene?.Dispose();
        _hdrFrameBuffer?.Dispose();
        _hdrFrameBuffer = null;
        _sdrFrameBuffer?.Dispose();
        _sdrFrameBuffer = null;
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

        _hdrFrameBuffer!.Bind();
        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
        graphics3D.Clear();
        _scene?.OnUpdateRuntime(timeSpan);
        _hdrFrameBuffer.Unbind();
        var spec = _hdrFrameBuffer!.GetSpecification();
        postProcessOrchestrator.Run(
            _hdrFrameBuffer.GetColorAttachmentRendererId(),
            spec.Width,
            spec.Height,
            PostProcessSettings.FromScene(_scene!.PostProcess, fxaaEnabled: true),
            _sdrFrameBuffer,
            fxaaToBackbuffer: true);
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        _cameraController?.OnEvent(windowEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent && _scene != null)
        {
            _scene.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
            _hdrFrameBuffer?.Resize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
            _sdrFrameBuffer?.Resize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }

    public void Draw()
    {
        const float padding = 10f;
        var io = ImGui.GetIO();
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
