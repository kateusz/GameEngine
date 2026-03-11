using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene;

/// <summary>
/// Imports a 3D model file (FBX, glTF, OBJ, etc.) into an existing scene,
/// creating one entity per mesh with PBR materials and optional lights/camera.
/// </summary>
public class ModelSceneImporter(IMeshFactory meshFactory)
{
    private static readonly ILogger Logger = Log.ForContext<ModelSceneImporter>();

    /// <summary>
    /// Imports a model file into the scene, creating entities for all meshes.
    /// Returns the root entity that parents all mesh entities.
    /// </summary>
    /// <param name="scene">Target scene to import into</param>
    /// <param name="modelPath">Absolute or relative path to the model file (FBX, glTF, OBJ)</param>
    /// <param name="addDefaultLighting">If true, adds a directional sun light and ambient fill</param>
    /// <param name="addCamera">If true, adds a perspective camera positioned to view the scene</param>
    public ImportResult Import(IScene scene, string modelPath, bool addDefaultLighting = true, bool addCamera = true)
    {
        Logger.Information("Importing model from {ModelPath}", modelPath);

        var model = meshFactory.CreateModel(modelPath);
        var result = new ImportResult();

        // Create entities for each mesh
        var meshIndex = 0;
        foreach (var mesh in model.Meshes)
        {
            var entityName = string.IsNullOrEmpty(mesh.Name) || mesh.Name == "Model_Mesh"
                ? $"Mesh_{meshIndex}"
                : mesh.Name;

            var entity = scene.CreateEntity(entityName);
            var transform = entity.AddComponent<TransformComponent>();

            // Decompose the accumulated node transform from the Assimp scene graph
            if (Matrix4x4.Decompose(mesh.NodeTransform, out var scale, out var rotation, out var translation))
            {
                transform.Translation = translation;
                transform.Scale = scale;
                // Convert quaternion to Euler angles
                var q = rotation;
                var sinP = 2.0f * (q.W * q.X - q.Z * q.Y);
                var pitch = MathF.Abs(sinP) >= 1.0f ? MathF.CopySign(MathF.PI / 2, sinP) : MathF.Asin(sinP);
                var yaw = MathF.Atan2(2.0f * (q.W * q.Y + q.X * q.Z), 1.0f - 2.0f * (q.Y * q.Y + q.X * q.X));
                var roll = MathF.Atan2(2.0f * (q.W * q.Z + q.X * q.Y), 1.0f - 2.0f * (q.Z * q.Z + q.X * q.X));
                transform.Rotation = new Vector3(pitch, yaw, roll);
            }

            var meshComponent = entity.AddComponent<MeshComponent>();
            meshComponent.SetMesh(mesh);

            var modelRenderer = entity.AddComponent<ModelRendererComponent>();
            modelRenderer.CastShadows = true;
            modelRenderer.ReceiveShadows = true;

            // Set color from PBR material if available
            if (mesh.Material != null)
            {
                modelRenderer.Color = mesh.Material.AlbedoColor;
            }

            result.MeshEntities.Add(entity);
            meshIndex++;
        }

        Logger.Information("Created {Count} mesh entities from model", result.MeshEntities.Count);

        // Add default directional light (sun)
        if (addDefaultLighting)
        {
            var sunEntity = scene.CreateEntity("Sun_DirectionalLight");
            sunEntity.AddComponent<TransformComponent>();
            var light = sunEntity.AddComponent<LightComponent>();
            light.Type = LightType.Directional;
            light.Direction = Vector3.Normalize(new Vector3(-0.5f, -1.0f, -0.3f));
            light.Color = new Vector3(1.0f, 0.95f, 0.9f); // Warm sunlight
            light.Intensity = 2.0f;
            light.CastShadows = true;
            result.LightEntity = sunEntity;

            // Add a fill light (point light) for ambient bounce approximation
            var fillEntity = scene.CreateEntity("Fill_PointLight");
            var fillTransform = fillEntity.AddComponent<TransformComponent>();
            fillTransform.Translation = new Vector3(0, 5, 0);
            var fillLight = fillEntity.AddComponent<LightComponent>();
            fillLight.Type = LightType.Point;
            fillLight.Color = new Vector3(0.4f, 0.5f, 0.7f); // Cool sky fill
            fillLight.Intensity = 0.5f;
            fillLight.Range = 100.0f;
            fillLight.CastShadows = false;

            Logger.Information("Added default lighting (sun + fill)");
        }

        // Add a perspective camera
        if (addCamera)
        {
            var cameraEntity = scene.CreateEntity("SceneCamera");
            var camTransform = cameraEntity.AddComponent<TransformComponent>();
            camTransform.Translation = new Vector3(0, 5, 15);

            var cameraComponent = cameraEntity.AddComponent<CameraComponent>();
            cameraComponent.Primary = true;
            cameraComponent.Camera = new SceneCamera();
            cameraComponent.Camera.SetPerspective(MathF.PI / 4f, 0.1f, 1000.0f);
            result.CameraEntity = cameraEntity;

            Logger.Information("Added perspective camera");
        }

        return result;
    }

    public class ImportResult
    {
        public List<ECS.Entity> MeshEntities { get; } = [];
        public ECS.Entity? LightEntity { get; set; }
        public ECS.Entity? CameraEntity { get; set; }
    }
}
