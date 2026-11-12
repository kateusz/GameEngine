using System.Numerics;
using Benchmark.Platform;
using DryIoc;
using ECS.Systems;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Project;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using ImGuiNET;
using SceneComponents.Camera;
using SceneComponents.Rendering;
using Scripting;

namespace Benchmark;

public sealed class Benchmark3DRunner(
    SceneFactory sceneFactory,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
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
    Container container) : IBenchmarkRunner
{
    private readonly EditorScenePresenter _editorDisplay = new(window, hdrTarget, tonemap);
    private int _count = 1000;
    private float _duration = 5f;
    private string _projectPathInput = "";
    private string? _projectError;
    private string[] _scenePaths = [];
    private int _selectedScene = -1;
    private string? _sceneLoadError;
    private PipelineBenchmarkRun? _preview;

    public string Title => "3D";
    public BenchmarkSettings Settings => new(_count, _duration);

    public IReadOnlyList<BenchmarkTestButton> Tests { get; } =
    [
        new("Cubes", "Cube count"),
        new("InstancedMesh", "Instanced mesh"),
        new("MergedMesh", "Merged mesh"),
        new("UniqueMeshes", "Unique meshes"),
        new("GlbModels", "Project scene")
    ];

    public void DrawControls()
    {
        DrawProjectAndScenePicker();
        ImGui.Separator();
        var max = 20000;
        ImGui.DragInt("Object Count", ref _count, 100, 100, max);
        _count = System.Math.Clamp(_count, 100, max);
        ImGui.DragFloat("Test Duration (s)", ref _duration, 0.5f, 1f, 60f);
    }

    public void ClearIdle() => SetPreview(null);

    public bool IdleTick(TimeSpan delta)
    {
        if (_preview is null)
            return false;
        _preview.Tick(delta);
        return true;
    }

    public void IdleInput(InputEvent windowEvent, bool pointerOverUi) =>
        _preview?.HandleInput(windowEvent, pointerOverUi);

    public bool TryCreate(string testId, BenchmarkSettings settings, out BenchmarkRun? run, out string? error)
    {
        run = null;
        ScenePlan plan;
        try
        {
            plan = ScenePlans.Plan("3D", testId);
        }
        catch (ArgumentOutOfRangeException)
        {
            error = $"Unknown test: {testId}";
            return false;
        }

        _duration = settings.DurationSeconds;
        if (testId == "GlbModels")
            return TryCreateProjectScene(plan, out run, out error);

        var count = testId == "UniqueMeshes"
            ? System.Math.Clamp(settings.Count, 100, BenchmarkScenes.UniqueMeshCap)
            : System.Math.Clamp(settings.Count, 100, 20000);
        _count = count;

        var scene = sceneFactory.Create("Benchmark", SceneDimension.ThreeD);
        BenchmarkScenes.AddAmbient(scene);

        if (testId == "Cubes")
        {
            BenchmarkScenes.AddDistrict(scene, count);
        }
        else if (testId is "InstancedMesh" or "MergedMesh")
        {
            var path = PathBuilder.Resolve(BenchmarkScenes.TemplateAsset);
            if (models.Create(path, null) == null)
            {
                scene.Dispose();
                error = $"Mesh failed to load: {path}";
                return false;
            }

            BenchmarkScenes.AddDistrict(scene, count, path);
        }
        else if (testId == "UniqueMeshes")
        {
            byte[] bytes;
            try
            {
                var template = PathBuilder.Resolve(BenchmarkScenes.TemplateAsset);
                if (models.Create(template, null) == null)
                {
                    scene.Dispose();
                    error = $"Mesh failed to load: {template}";
                    return false;
                }

                bytes = File.ReadAllBytes(template + ".mesh");
            }
            catch (Exception ex)
            {
                scene.Dispose();
                error = ex.Message;
                return false;
            }

            if (!BenchmarkScenes.TryAddUniqueMeshes(scene, models, count, bytes, out error))
                return false;
        }
        else
        {
            scene.Dispose();
            error = $"Unknown test: {testId}";
            return false;
        }

        run = new PipelineBenchmarkRun(
            plan.ResultName, _duration, scene,
            plan.DirectionalShadows, plan.PointShadows,
            graphics2D, graphics3D, textures, models,
            plan.StaticMeshMerging ? vertexArrays : null,
            plan.StaticMeshMerging ? vertexBuffers : null,
            plan.StaticMeshMerging ? indexBuffers : null,
            clearModelCache: testId == "UniqueMeshes",
            ExpectedShadow.None);
        error = null;
        return true;
    }

    public void SetCliScenePath(string? scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        var full = Path.IsPathRooted(scenePath)
            ? scenePath
            : projectContext.Root is { } root
                ? Path.Combine(root, scenePath)
                : Path.GetFullPath(scenePath);
        if (!File.Exists(full))
            return;

        BenchmarkProjectAccess.TryApplyProjectForSceneFile(full, projectContext);
        if (projectContext.Root is not null)
            _projectPathInput = projectContext.Root;

        _scenePaths = [full];
        _selectedScene = 0;
    }

    void DrawProjectAndScenePicker()
    {
        ImGui.TextUnformatted("Project (like editor Open Project)");
        if (projectContext.HasProject)
            ImGui.TextUnformatted($"Open: {projectContext.Root}");
        else
            ImGui.TextUnformatted("No project open — open one to list .scene files.");

        ImGui.InputText("Project folder", ref _projectPathInput, 512);
        if (FolderPicker.IsAvailable)
        {
            ImGui.SameLine();
            if (ImGui.Button("Browse…"))
            {
                var initial = string.IsNullOrWhiteSpace(_projectPathInput)
                    ? projectContext.Root
                    : _projectPathInput;
                if (FolderPicker.PickFolder("Select Project Folder", initial) is { } picked)
                    _projectPathInput = picked;
            }
        }

        if (ImGui.Button("Open project"))
            TryOpenProjectAndListScenes();

        if (_projectError is not null)
            ImGui.TextWrapped(_projectError);

        if (_scenePaths.Length == 0)
        {
            ImGui.TextUnformatted("No .scene files under assets/scenes.");
            return;
        }

        var label = _selectedScene >= 0 ? SceneListLabel(_selectedScene) : "(choose scene)";
        ImGui.TextUnformatted("Scene:");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##benchmarkScene", label))
        {
            for (var i = 0; i < _scenePaths.Length; i++)
            {
                var selected = i == _selectedScene;
                if (ImGui.Selectable(SceneListLabel(i), selected))
                {
                    _selectedScene = i;
                    EnableSelectedScene();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        if (_sceneLoadError is not null)
            ImGui.TextWrapped(_sceneLoadError);
        else if (_preview != null)
            ImGui.TextUnformatted("Scene is on — orbit / pan / scroll like during a run.");
    }

    string SceneListLabel(int index)
    {
        var full = _scenePaths[index];
        if (projectContext.Root is not null && full.StartsWith(projectContext.Root, StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(projectContext.Root, full).Replace('\\', '/');
        return Path.GetFileName(full);
    }

    void TryOpenProjectAndListScenes()
    {
        if (!BenchmarkProjectAccess.TryOpenProject(_projectPathInput, projectContext, out _projectError))
            return;

        _projectError = null;
        _projectPathInput = projectContext.Root ?? _projectPathInput;
        _sceneLoadError = null;
        SetPreview(null);
        _scenePaths = BenchmarkProjectAccess.EnumerateScenes(projectContext);
        _selectedScene = -1;
    }

    void EnableSelectedScene()
    {
        _sceneLoadError = null;
        SetPreview(null);
        if (SelectedScenePath() is not { } path)
            return;

        if (TryLoadProjectScenePipeline(path, durationSeconds: 0f, out var pipeline, out var error))
            SetPreview(pipeline);
        else
            _sceneLoadError = error;
    }

    bool TryCreateProjectScene(ScenePlan plan, out BenchmarkRun? run, out string? error)
    {
        run = null;
        if (SelectedScenePath() is not { } path)
        {
            error = "Open a project, pick a scene, then run Project scene.";
            return false;
        }

        SetPreview(null);
        if (!TryLoadProjectScenePipeline(path, _duration, out var pipeline, out error))
            return false;

        run = pipeline;
        return true;
    }

    bool TryLoadProjectScenePipeline(
        string path, float durationSeconds, out PipelineBenchmarkRun? pipeline, out string? error)
    {
        pipeline = null;
        error = null;
        if (!BenchmarkProjectAccess.TryApplyProjectForSceneFile(path, projectContext))
        {
            error = "Could not find a project root (assets/) for this scene.";
            return false;
        }

        var projectRoot = projectContext.Root
                          ?? throw new InvalidOperationException("Project root missing after Apply.");
        var startedPlay = TryLoadAndRegisterGameAssembly(projectRoot, out var gameAsmError);
        if (gameAsmError is not null)
        {
            error = gameAsmError;
            return false;
        }

        var scene = sceneFactory.Create(Path.GetFileNameWithoutExtension(path), SceneDimension.ThreeD);
        try
        {
            scenes.Deserialize(scene, path);
        }
        catch (Exception ex)
        {
            scene.Dispose();
            error = ex.Message;
            return false;
        }

        scene.UpdateWorldTransforms();
        if (startedPlay)
        {
            // Level kits (IslandDash) spawn meshes from GameAssembly systems, not the .scene file.
            sceneContext.SetScene(scene);
            RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
            // One tick: Level rebuild (101) then CameraSystem snap (110).
            scene.OnUpdateRuntime(TimeSpan.FromSeconds(1.0 / 60.0));
        }

        // ponytail: zones hide most of castle from the pulled-back camera — strip for draw-cost stress
        foreach (var (_, model) in scene.Context.View<ModelRendererComponent>())
            model.VisibilityZoneEntityId = -1;
        var plan = ScenePlans.Plan("3D", "GlbModels");
        pipeline = new PipelineBenchmarkRun(
            plan.ResultName, durationSeconds, scene,
            plan.DirectionalShadows, plan.PointShadows,
            graphics2D, graphics3D, textures, models,
            vertexArrays, vertexBuffers, indexBuffers,
            clearModelCache: false,
            ExpectedShadow.None,
            onDispose: startedPlay ? scene.OnRuntimeStop : null,
            display: _editorDisplay,
            matchEditorLight: true);
        // Scripted games: pose as-is. Art scenes: orbit pullback for framing.
        if (TryPrimaryLook(scene, out var eye, out var focal, pullback: startedPlay ? 0f : 64f))
            pipeline.Frame(eye, focal);
        return true;
    }

    /// <summary>
    /// When <c>game.config.json</c> + GameAssembly.dll exist, load scripts so level builders can spawn geometry.
    /// Returns false when this is a plain art project (no game assembly) — caller keeps scene-only path.
    /// </summary>
    bool TryLoadAndRegisterGameAssembly(string projectRoot, out string? error)
    {
        error = null;
        var configPath = GameConfiguration.PathFor(projectRoot);
        if (!File.Exists(configPath))
            return false;
        if (!GameConfiguration.TryLoad(configPath, out var config, out var loadError))
        {
            error = loadError;
            return false;
        }

        if (!TryFindGameAssembly(projectRoot, config.GameAssemblyPath, out var dllPath))
        {
            error =
                $"game.config.json found but GameAssembly.dll missing under {projectRoot}. " +
                "Run the game's run.sh --no-run (or equivalent) first.";
            return false;
        }

        scriptEngine.LoadGameAssemblyFromFile(dllPath);
        var assembly = scriptEngine.GetLoadedGameAssembly();
        if (assembly is null)
        {
            error = $"Failed to load game assembly from {dllPath}";
            return false;
        }

        serializers.RegisterFromAssembly(assembly);
        GameAssemblyContainerRegistration.TryRegisterContainer(container, assembly);
        return true;
    }

    static bool TryFindGameAssembly(string projectRoot, string configuredPath, out string dllPath)
    {
        var name = string.IsNullOrWhiteSpace(configuredPath) ? "GameAssembly.dll" : Path.GetFileName(configuredPath);
        string[] candidates =
        [
            Path.Combine(projectRoot, name),
            Path.Combine(projectRoot, ".build", "Release", name),
            Path.Combine(projectRoot, ".build", "Debug", name),
            Path.Combine(projectRoot, ".build", "game-Release", name),
            Path.Combine(projectRoot, ".build", "game-Debug", name),
        ];
        foreach (var c in candidates)
        {
            if (!File.Exists(c))
                continue;
            dllPath = Path.GetFullPath(c);
            return true;
        }

        dllPath = "";
        return false;
    }

    string? SelectedScenePath() =>
        _selectedScene >= 0 && _selectedScene < _scenePaths.Length ? _scenePaths[_selectedScene] : null;

    void SetPreview(PipelineBenchmarkRun? next)
    {
        _preview?.Dispose();
        _preview = next;
    }

    static bool TryPrimaryLook(IScene scene, out Vector3 eye, out Vector3 focal, float pullback = 64f)
    {
        eye = default;
        focal = default;
        foreach (var entity in scene.Entities)
        {
            if (!entity.TryGetComponent<CameraComponent>(out var camera) || !camera.Primary)
                continue;
            if (!entity.TryGetComponent<SceneComponents.TransformComponent>(out var transform))
                continue;

            var world = transform.GetWorldTransform();
            eye = new Vector3(world.M41, world.M42, world.M43);
            var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, world));
            if (pullback <= 0f)
            {
                focal = eye + forward;
                return true;
            }

            // Pull back from in-room cameras for stable art-scene framing.
            const float lookAhead = 12f;
            focal = eye + forward * lookAhead;
            eye = focal - forward * pullback;
            return true;
        }

        return false;
    }
}
