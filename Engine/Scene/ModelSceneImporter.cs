using System.Numerics;
using Engine.Renderer;
using Engine.Scene.Components;
using Engine.Scene.Components.Lights;
using Serilog;

namespace Engine.Scene;

public class ModelSceneImporter(IMeshFactory meshFactory)
{
    private static readonly ILogger Logger = Log.ForContext<ModelSceneImporter>();
    
    public ImportResult Import(IScene scene, string modelPath, bool addDefaultLighting = true, bool addCamera = true)
    {
        Logger.Information("Importing model from {ModelPath}", modelPath);

        var (meshes, materials, lights) = meshFactory.LoadModel(modelPath);
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
            meshComponent.ModelPath = modelPath;
            meshComponent.MeshIndex = i;

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

        if (lights.Count > 0)
        {
            foreach (var lightData in lights)
                CreateLightEntity(scene, lightData, result);

            Logger.Information("Imported {Count} lights from model", lights.Count);
        }
        else if (addDefaultLighting)
        {
            var lightEntity = scene.CreateEntity("Sun_Light");
            var light = lightEntity.AddComponent<DirectionalLightComponent>();
            light.Color = new Vector3(1.0f, 0.95f, 0.9f);
            light.Strength = 0.8f;
            var tc = lightEntity.AddComponent<TransformComponent>();
            tc.Translation = new Vector3(-1.39f, 73, 35);
            result.LightEntities.Add(lightEntity);

            Logger.Information("Added default lighting at {Position}", tc.Translation);
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

    private static void CreateLightEntity(IScene scene, ModelLightData data, ImportResult result)
    {
        var entity = scene.CreateEntity(data.Name);
        var tc = entity.AddComponent<TransformComponent>();
        tc.Translation = data.Position;

        switch (data.Type)
        {
            case ModelLightType.Directional:
            {
                var comp = entity.AddComponent<DirectionalLightComponent>();
                comp.Color = data.Color;
                comp.Direction = data.Direction;
                comp.Strength = data.Intensity;
                break;
            }
            case ModelLightType.Point:
            case ModelLightType.Spot:
            {
                var comp = entity.AddComponent<PointLightComponent>();
                comp.Color = data.Color;
                comp.Intensity = data.Intensity * 10f;
                break;
            }
        }

        result.LightEntities.Add(entity);
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
        public List<ECS.Entity> LightEntities { get; } = [];
        public ECS.Entity? CameraEntity { get; set; }
        public Vector3 SceneCenter { get; set; }
        public Vector3 SceneExtents { get; set; }
        public float SceneRadius { get; set; }
    }
}
