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
        if (shader == ShaderId.Tonemap)
            return (Path.Combine(dir, "fxaa.vert"), Path.Combine(dir, "tonemap.frag"));
        if (shader == ShaderId.BloomExtract)
            return (Path.Combine(dir, "fxaa.vert"), Path.Combine(dir, "bloomExtract.frag"));
        if (shader == ShaderId.BloomBlur)
            return (Path.Combine(dir, "fxaa.vert"), Path.Combine(dir, "bloomBlur.frag"));
        if (shader == ShaderId.ViewNormal)
            return (Path.Combine(dir, "viewNormal.vert"), Path.Combine(dir, "viewNormal.frag"));
        if (shader == ShaderId.ViewNormalModel)
            return (Path.Combine(dir, "viewNormalModel.vert"), Path.Combine(dir, "viewNormal.frag"));
        if (shader == ShaderId.Ssao)
            return (Path.Combine(dir, "fxaa.vert"), Path.Combine(dir, "ssao.frag"));
        if (shader == ShaderId.SsaoBlur)
            return (Path.Combine(dir, "fxaa.vert"), Path.Combine(dir, "ssaoBlur.frag"));
        if (shader == ShaderId.Emissive)
            return (Path.Combine(dir, "modelShader.vert"), Path.Combine(dir, "emissive.frag"));

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
