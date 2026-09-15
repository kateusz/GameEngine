namespace Engine.Renderer.Shaders;

internal static class EngineShaderPaths
{
    public static (string Vert, string Frag) Resolve(ShaderId shader)
    {
        var name = shader switch
        {
            ShaderId.Texture => "textureShader",
            ShaderId.Line => "lineShader",
            ShaderId.Cube => "cube",
            ShaderId.Model => "modelShader",
            _ => throw new ArgumentOutOfRangeException(nameof(shader), shader, null)
        };

        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        return (Path.Combine(dir, name + ".vert"), Path.Combine(dir, name + ".frag"));
    }
}
