namespace Engine.Scripting;

public static class ScriptableEntityTemplates
{
    public static string Generate(string className) => $$"""
        using Audio;
        using ECS;
        using Input;
        using Math;
        using SceneComponents;
        using SceneComponents.Camera;
        using SceneComponents.Rendering;
        using Scripting;

        public class {{className}} : ScriptableEntity
        {
            public {{className}}(IComponentAccessor componentAccessor, IAudio audio, IAudioPlayback audioPlayback, IPhysicsQueries physicsQueries) : base(componentAccessor, audio, audioPlayback, physicsQueries) { }

            public override void OnCreate()
            {
                Console.WriteLine("{{className}} created!");
            }

            public override void OnUpdate(TimeSpan ts)
            {
                // Your update logic here
            }

            public override void OnDestroy()
            {
                Console.WriteLine("{{className}} destroyed!");
            }

            public override void OnKeyPressed(KeyCodes key)
            {
                if (key == KeyCodes.Space)
                {
                    Console.WriteLine("{{className}} action triggered!");
                }
            }
        }
        """;
}
