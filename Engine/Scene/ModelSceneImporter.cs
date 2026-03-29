using System.Numerics;
using Engine.Renderer;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene;

public class ModelSceneImporter(IMeshFactory meshFactory)
{
    private static readonly ILogger Logger = Log.ForContext<ModelSceneImporter>();

    public ImportResult Import(IScene scene, string modelPath, bool addDefaultLighting = true, bool addCamera = true)
    {
        Logger.Information("Importing model from {ModelPath}", modelPath);

        var (meshes, materials) = meshFactory.LoadModel(modelPath);
        var result = new ImportResult();

        for (var i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];
            var entityName = string.IsNullOrEmpty(mesh.Name)
                ? $"Mesh_{i}"
                : mesh.Name;

            var entity = scene.CreateEntity(entityName);
            entity.AddComponent<TransformComponent>();

            // Keep NodeTransform on the mesh — DrawModel applies mesh.NodeTransform * entityTransform.
            // Decomposing to Euler angles is lossy and corrupts the transform.
            var meshComponent = entity.AddComponent<MeshComponent>();
            meshComponent.SetMesh(mesh);

            var modelRenderer = entity.AddComponent<ModelRendererComponent>();
            modelRenderer.CastShadows = true;
            modelRenderer.ReceiveShadows = true;

            if (i < materials.Count)
                modelRenderer.Materials = [materials[i]];

            result.MeshEntities.Add(entity);
        }

        Logger.Information("Created {Count} mesh entities from model", result.MeshEntities.Count);

        ComputeSceneBounds(meshes, result);
        Logger.Information("Scene bounds — Center: {Center}, Radius: {Radius}", result.SceneCenter, result.SceneRadius);

        if (addDefaultLighting)
        {
            var lightEntity = scene.CreateEntity("Sun_Light");
            lightEntity.AddComponent<TransformComponent>();
            var light = lightEntity.AddComponent<LightingComponent>();
            light.Position = result.SceneCenter + new Vector3(result.SceneRadius * 0.5f, result.SceneRadius, result.SceneRadius * 0.5f);
            light.Color = new Vector3(1.0f, 0.95f, 0.9f);
            result.LightEntity = lightEntity;

            Logger.Information("Added default lighting at {Position}", light.Position);
        }

        if (addCamera)
        {
            var radius = result.SceneRadius > 0 ? result.SceneRadius : 100f;
            var cameraPos = result.SceneCenter + new Vector3(0, radius * 0.3f, radius * 1.5f);
            var farClip = radius * 10f;

            var cameraEntity = scene.CreateEntity("SceneCamera");
            var camTransform = cameraEntity.AddComponent<TransformComponent>();
            camTransform.Translation = cameraPos;

            var cameraComponent = cameraEntity.AddComponent<CameraComponent>();
            cameraComponent.Primary = true;
            cameraComponent.Camera = new SceneCamera();
            cameraComponent.Camera.SetPerspective(MathF.PI / 4f, 0.1f, farClip);
            result.CameraEntity = cameraEntity;

            Logger.Information("Added perspective camera at {Position}, far clip: {Far}", cameraPos, farClip);
        }

        return result;
    }

    private static void ComputeSceneBounds(List<Mesh> meshes, ImportResult result)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var hasVertices = false;

        foreach (var mesh in meshes)
        {
            if (mesh.Vertices.Count == 0)
                continue;

            var transform = mesh.NodeTransform;

            foreach (var vertex in mesh.Vertices)
            {
                var worldPos = Vector3.Transform(vertex.Position, transform);
                min = Vector3.Min(min, worldPos);
                max = Vector3.Max(max, worldPos);
                hasVertices = true;
            }
        }

        if (!hasVertices)
            return;

        result.SceneCenter = (min + max) * 0.5f;
        result.SceneExtents = (max - min) * 0.5f;
        result.SceneRadius = result.SceneExtents.Length();
    }

    public class ImportResult
    {
        public List<ECS.Entity> MeshEntities { get; } = [];
        public ECS.Entity? LightEntity { get; set; }
        public ECS.Entity? CameraEntity { get; set; }
        public Vector3 SceneCenter { get; set; }
        public Vector3 SceneExtents { get; set; }
        public float SceneRadius { get; set; }
    }
}
