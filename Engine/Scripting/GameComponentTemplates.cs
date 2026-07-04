namespace Engine.Scripting;

public static class GameComponentTemplates
{
    public static string ToClassName(string baseName) => $"{baseName}Component";

    public static string Generate(string className) => $$"""
        using ECS;

        [SerializableComponent]
        public class {{className}} : IGameComponent
        {
            public IComponent Clone() => new {{className}}();
        }
        """;
}
