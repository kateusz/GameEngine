using DryIoc;
using ECS.Systems;
using Engine.Core.Window;
using Engine.Project;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Scripting;

namespace Benchmark;

public sealed class BenchmarkRunnerFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    SceneFactory sceneFactory,
    ITextureFactory textures,
    IModelFactory models,
    IVertexArrayFactory vertexArrays,
    IVertexBufferFactory vertexBuffers,
    IIndexBufferFactory indexBuffers,
    ISceneSerializer scenes,
    IProjectContext projectContext,
    IGameWindow window,
    FxaaPass hdrTarget,
    TonemapPass tonemap,
    ISceneContext sceneContext,
    IScriptEngine scriptEngine,
    IComponentSerializerRegistry serializers,
    Func<IEnumerable<IGameSystem>> resolveGameSystems,
    Container container)
{
    private Benchmark2DRunner? _twoD;

    public IBenchmarkRunner? Create(string category) => category switch
    {
        "2D" => TwoD(),
        "3D" => new Benchmark3DRunner(sceneFactory, graphics2D, graphics3D, textures, models,
            vertexArrays, vertexBuffers, indexBuffers, scenes, projectContext, window, hdrTarget, tonemap,
            sceneContext, scriptEngine, serializers, resolveGameSystems, container),
        "Lighting" => new BenchmarkGridRunner("Lighting", sceneFactory, graphics2D, graphics3D, textures, models,
            vertexArrays, vertexBuffers, indexBuffers),
        "Shadows" => new BenchmarkGridRunner("Shadows", sceneFactory, graphics2D, graphics3D, textures, models,
            vertexArrays, vertexBuffers, indexBuffers),
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
