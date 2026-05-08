// using System.Numerics;
// using ECS.Systems;
// using SceneComponents;
//
// namespace PingPong;
//
// internal sealed class PingPongSetupSystem(
//     ISceneContext sceneContext) : IGameSystem
// {
//     public int Priority => 1;
//
//     public void OnInit()
//     {
//         var scene = sceneContext.ActiveScene;
//         if (scene == null)
//             return;
//
//         // Skip if scene is already initialized (e.g. loaded from file).
//         if (scene.Entities.Any(e => e.HasComponent<ScoreComponent>()))
//             return;
//
//         CreateCameraEntity(scene);
//         CreateGameplayEntities(scene);
//     }
//
//     public void OnUpdate(TimeSpan deltaTime)
//     {
//     }
//
//     public void OnShutdown()
//     {
//     }
//
//     private static void CreateCameraEntity(IScene scene)
//     {
//         var cameraEntity = scene.CreateEntity("Main Camera");
//         var cameraTransform = cameraEntity.AddComponent<TransformComponent>();
//         cameraTransform.Translation = new Vector3(0.0f, 0.0f, 1.0f);
//
//         var camera = cameraEntity.AddComponent<CameraComponent>();
//         camera.Primary = true;
//         camera.ProjectionType = CameraProjectionTypeData.Orthographic;
//         camera.OrthographicSize = 6.0f;
//         camera.OrthographicNear = -1.0f;
//         camera.OrthographicFar = 1.0f;
//     }
//
//     private static void CreateGameplayEntities(IScene scene)
//     {
//         CreatePaddle(scene, name: "Player Paddle", x: -10.0f, isPlayer: true);
//         CreatePaddle(scene, name: "AI Paddle", x: 10.0f, isPlayer: false);
//         CreateBall(scene);
//         CreateBoundary(scene, "Top Boundary", y: 5.5f, BoundaryPosition.Top);
//         CreateBoundary(scene, "Bottom Boundary", y: -5.5f, BoundaryPosition.Bottom);
//         CreateScore(scene);
//     }
//
//     private static void CreatePaddle(IScene scene, string name, float x, bool isPlayer)
//     {
//         var entity = scene.CreateEntity(name);
//         var transform = entity.AddComponent<TransformComponent>();
//         transform.Translation = new Vector3(x, 0.0f, 0.0f);
//         transform.Scale = new Vector3(0.7f, 4.0f, 1.0f);
//
//         var sprite = entity.AddComponent<SpriteRendererComponent>();
//         sprite.Color = new Vector4(0.95f, 0.95f, 0.95f, 1.0f);
//
//         var paddle = entity.AddComponent<PaddleComponent>();
//         paddle.IsPlayer = isPlayer;
//         paddle.MoveSpeed = 8.0f;
//     }
//
//     private static void CreateBall(IScene scene)
//     {
//         var entity = scene.CreateEntity("Ball");
//         var transform = entity.AddComponent<TransformComponent>();
//         transform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
//         transform.Scale = new Vector3(0.5f, 0.5f, 1.0f);
//
//         var sprite = entity.AddComponent<SpriteRendererComponent>();
//         sprite.Color = new Vector4(0.95f, 0.8f, 0.2f, 1.0f);
//
//         var ball = entity.AddComponent<BallComponent>();
//         ball.Speed = 8.0f;
//         ball.Velocity = new Vector2(ball.Speed, 0.0f);
//     }
//
//     private static void CreateBoundary(IScene scene, string name, float y, BoundaryPosition position)
//     {
//         var entity = scene.CreateEntity(name);
//         var transform = entity.AddComponent<TransformComponent>();
//         transform.Translation = new Vector3(0.0f, y, 0.0f);
//
//         var boundary = entity.AddComponent<BoundaryComponent>();
//         boundary.Position = position;
//     }
//
//     private static void CreateScore(IScene scene)
//     {
//         var entity = scene.CreateEntity("Score");
//         var score = entity.AddComponent<ScoreComponent>();
//         score.MaxScore = 10;
//     }
// }
