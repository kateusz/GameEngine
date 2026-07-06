using Engine.Core;

namespace Engine.Scripting;

[SkipUnitTests]
public static class GameSystemTemplates
{
    public static string ToClassName(string baseName) => $"{baseName}System";

    public static string Generate(string className) => $$"""
        using ECS;
        using ECS.Systems;
        using Input;
        using Scripting;

        [Register(typeof(IGameSystem))]
        public class {{className}}(IContext context, IKeyboardInput keyboardInput) : IGameSystem
        {
            public int Priority => 100;

            public void OnInit() { }

            public void OnUpdate(TimeSpan deltaTime) { }

            public void OnShutdown() { }
        }
        """;
}
