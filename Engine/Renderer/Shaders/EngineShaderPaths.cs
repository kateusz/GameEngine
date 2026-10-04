namespace Engine.Renderer.Shaders;

internal static class EngineShaderPaths
{
    public static (string Vert, string Frag) Resolve(ShaderId shader)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        if (shader == ShaderId.SelectionOutline)
            return (Path.Combine(dir, "fxaa.vert"), Path.Combine(dir, "selectionOutline.frag"));
        if (shader == ShaderId.EquirectToCube)
            return (Path.Combine(dir, "skybox.vert"), Path.Combine(dir, "equirectToCube.frag"));
        if (shader == ShaderId.Irradiance)
            return (Path.Combine(dir, "skybox.vert"), Path.Combine(dir, "irradiance.frag"));
        if (shader == ShaderId.Prefilter)
            return (Path.Combine(dir, "skybox.vert"), Path.Combine(dir, "prefilter.frag"));

        var name = shader switch
        {
            ShaderId.Texture => "textureShader",
            ShaderId.Line => "lineShader",
            ShaderId.Cube => "cube",
            ShaderId.Model => "modelShader",
            ShaderId.Depth => "depth",
            ShaderId.PointDepth => "pointDepth",
            ShaderId.Fxaa => "fxaa",
            ShaderId.Skybox => "skybox",
            ShaderId.BrdfLut => "brdfLut",
            _ => throw new ArgumentOutOfRangeException(nameof(shader), shader, null)
        };

        return (Path.Combine(dir, name + ".vert"), Path.Combine(dir, name + ".frag"));
    }
}
