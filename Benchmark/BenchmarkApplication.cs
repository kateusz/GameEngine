using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Ui.ImGui;

namespace Benchmark;

public class BenchmarkApplication(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    ITextureFactory textureFactory,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    IModelFactory modelFactory,
    IImGuiLayer imGuiLayer)
    : Application(gameWindow, rendererApi, graphics2D, audio, graphics3D, textureFactory, shaderFactory, meshFactory,
        modelFactory, imGuiLayer, imGuiLayer);
