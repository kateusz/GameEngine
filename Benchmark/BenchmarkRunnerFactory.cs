using Engine.Project;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Serializer;

namespace Benchmark;

public sealed class BenchmarkRunnerFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    SceneFactory sceneFactory,
    ITextureFactory textures,
    IModelFactory models,
    ISceneSerializer scenes,
    IProjectContext projectContext)
{
    private Benchmark2DRunner? _twoD;

    public IBenchmarkRunner? Create(string category) => category switch
    {
        "2D" => TwoD(),
        "3D" => new Benchmark3DRunner(sceneFactory, graphics2D, graphics3D, textures, models, scenes, projectContext),
        "Lighting" => new BenchmarkGridRunner("Lighting", sceneFactory, graphics2D, graphics3D, textures, models),
        "Shadows" => new BenchmarkGridRunner("Shadows", sceneFactory, graphics2D, graphics3D, textures, models),
        _ => null
    };

    public void Prepare(string category)
    {
        if (category == "2D")
            TwoD().Load();
    }

    public void Dispose2D() => _twoD?.Dispose();

    private Benchmark2DRunner TwoD() => _twoD ??= new Benchmark2DRunner(graphics2D, sceneFactory, textures);
}
