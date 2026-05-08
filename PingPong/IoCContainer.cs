// using ECS.Systems;
//
// namespace PingPong;
//
// public static class IoCContainer
// {
//     public static void Register(Container container)
//     {
//         container.Register<IPongInputState, PongInputState>(Reuse.Singleton);
//         container.Register<IGameSystem, PingPongSetupSystem>(Reuse.Singleton);
//         container.Register<IGameSystem, PaddleInputSystem>(Reuse.Singleton);
//         container.Register<IGameSystem, PaddleAiSystem>(Reuse.Singleton);
//         container.Register<IGameSystem, BallMovementSystem>(Reuse.Singleton);
//         container.Register<IGameSystem, PongCollisionSystem>(Reuse.Singleton);
//         container.Register<IGameSystem, PongScoringSystem>(Reuse.Singleton);
//     }
// }