using System.Numerics;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Components.Pong;
using Engine.Scene.Systems.Pong;

namespace Sandbox;

public sealed class PingPongLayer(
    SceneFactory sceneFactory,
    IGraphics2D graphics2D,
    IPongInputState pongInputState) : ILayer
{
    private IScene? _scene;

    public void OnAttach(IInputSystem inputSystem)
    {
        _scene = sceneFactory.Create("PingPong", "PingPong");
        CreateCameraEntity(_scene);
        CreateGameplayEntities(_scene);
        _scene.OnViewportResize(DisplayConfig.DefaultWindowWidth, DisplayConfig.DefaultWindowHeight);
        _scene.OnRuntimeStart();
    }

    public void OnDetach()
    {
        pongInputState.MoveUpPressed = false;
        pongInputState.MoveDownPressed = false;

        _scene?.OnRuntimeStop();
        _scene?.Dispose();
        _scene = null;
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        graphics2D.SetClearColor(new Vector4(0.06f, 0.06f, 0.08f, 1.0f));
        graphics2D.Clear();
        _scene?.OnUpdateRuntime(timeSpan);
    }

    public void Draw()
    {
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        switch (windowEvent)
        {
            case KeyPressedEvent { KeyCode: KeyCodes.W }:
                pongInputState.MoveUpPressed = true;
                break;
            case KeyPressedEvent { KeyCode: KeyCodes.S }:
                pongInputState.MoveDownPressed = true;
                break;
            case KeyReleasedEvent { KeyCode: KeyCodes.W }:
                pongInputState.MoveUpPressed = false;
                break;
            case KeyReleasedEvent { KeyCode: KeyCodes.S }:
                pongInputState.MoveDownPressed = false;
                break;
        }
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent && resizeEvent.Width > 0 && resizeEvent.Height > 0)
            _scene?.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
    }

    private static void CreateCameraEntity(IScene scene)
    {
        var cameraEntity = scene.CreateEntity("Main Camera");
        var cameraTransform = cameraEntity.AddComponent<TransformComponent>();
        cameraTransform.Translation = new Vector3(0.0f, 0.0f, 1.0f);

        var camera = cameraEntity.AddComponent<CameraComponent>();
        camera.Primary = true;
        camera.Camera.SetOrthographic(size: 6.0f, nearClip: -1.0f, farClip: 1.0f);
    }

    private static void CreateGameplayEntities(IScene scene)
    {
        CreatePaddle(scene, name: "Player Paddle", x: -10.0f, isPlayer: true);
        CreatePaddle(scene, name: "AI Paddle", x: 10.0f, isPlayer: false);
        CreateBall(scene);
        CreateBoundary(scene, "Top Boundary", y: 5.5f, BoundaryPosition.Top);
        CreateBoundary(scene, "Bottom Boundary", y: -5.5f, BoundaryPosition.Bottom);
        CreateScore(scene);
    }

    private static void CreatePaddle(IScene scene, string name, float x, bool isPlayer)
    {
        var entity = scene.CreateEntity(name);
        var transform = entity.AddComponent<TransformComponent>();
        transform.Translation = new Vector3(x, 0.0f, 0.0f);
        transform.Scale = new Vector3(0.7f, 4.0f, 1.0f);

        var sprite = entity.AddComponent<SpriteRendererComponent>();
        sprite.Color = new Vector4(0.95f, 0.95f, 0.95f, 1.0f);

        var paddle = entity.AddComponent<PaddleComponent>();
        paddle.IsPlayer = isPlayer;
        paddle.MoveSpeed = 8.0f;
    }

    private static void CreateBall(IScene scene)
    {
        var entity = scene.CreateEntity("Ball");
        var transform = entity.AddComponent<TransformComponent>();
        transform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        transform.Scale = new Vector3(0.5f, 0.5f, 1.0f);

        var sprite = entity.AddComponent<SpriteRendererComponent>();
        sprite.Color = new Vector4(0.95f, 0.8f, 0.2f, 1.0f);

        var ball = entity.AddComponent<BallComponent>();
        ball.Speed = 8.0f;
        ball.Velocity = new Vector2(ball.Speed, 0.0f);
    }

    private static void CreateBoundary(IScene scene, string name, float y, BoundaryPosition position)
    {
        var entity = scene.CreateEntity(name);
        var transform = entity.AddComponent<TransformComponent>();
        transform.Translation = new Vector3(0.0f, y, 0.0f);

        var boundary = entity.AddComponent<BoundaryComponent>();
        boundary.Position = position;
    }

    private static void CreateScore(IScene scene)
    {
        var entity = scene.CreateEntity("Score");
        var score = entity.AddComponent<ScoreComponent>();
        score.MaxScore = 10;
    }
}
