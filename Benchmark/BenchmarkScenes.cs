using System.Numerics;
using Engine.Project;
using Engine.Renderer.Models;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;

namespace Benchmark;

public readonly record struct CityLot(
    Vector3 Position,
    Vector3 Scale,
    float Yaw,
    Vector4 Color,
    float Metallic,
    float Roughness);

public static class BenchmarkScenes
{
    public const int DistrictObjects = 800;
    public const int UniqueMeshCap = 500;
    public const string TemplateAsset = "assets/meshes/template.gltf";

    const int BlockPeriod = 5;
    const float LotSize = 2.6f;

    static readonly Vector4[] Palette =
    [
        new(0.76f, 0.71f, 0.62f, 1f),
        new(0.55f, 0.28f, 0.20f, 1f),
        new(0.46f, 0.48f, 0.51f, 1f),
        new(0.35f, 0.40f, 0.46f, 1f),
        new(0.45f, 0.32f, 0.18f, 1f),
        new(0.62f, 0.64f, 0.68f, 1f)
    ];

    public static void AddAmbient(IScene scene)
    {
        var ambient = scene.CreateEntity("ambient");
        ambient.AddComponent(new AmbientLightComponent { Color = Vector4.One, Strength = 0.1f });
    }

    public static void AddDistrict(IScene scene, int count, string? modelPath = null)
    {
        var side = SideFor(count);
        var span = side * LotSize;
        AddProp(scene, "ground", new Vector3(0f, -0.15f, 0f), new Vector3(span, 0.3f, span), 0f,
            new Vector4(0.18f, 0.19f, 0.20f, 1f), 0f, 0.95f, null);

        foreach (var lot in Lots(count))
            AddProp(scene, "block", lot.Position, lot.Scale, lot.Yaw, lot.Color, lot.Metallic, lot.Roughness, modelPath);
    }

    public static IEnumerable<CityLot> Lots(int count)
    {
        var side = SideFor(count);
        var origin = (side - 1) * 0.5f;
        var placed = 0;
        for (var row = 0; row < side && placed < count; row++)
        for (var col = 0; col < side && placed < count; col++)
        {
            if (col % BlockPeriod == BlockPeriod - 1 || row % BlockPeriod == BlockPeriod - 1)
                continue;

            var hash = Hash(col, row);
            var (scale, color, metal, rough, yaw) = Kind(hash);
            yield return new CityLot(
                new Vector3(
                    (col - origin) * LotSize + Jitter(hash),
                    scale.Y * 0.5f,
                    (row - origin) * LotSize + Jitter(hash >> 8)),
                scale,
                yaw,
                color,
                metal,
                rough);
            placed++;
        }
    }

    public static void AddSun(IScene scene)
    {
        var sun = scene.CreateEntity("sun");
        sun.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0.3f, -1f, 0.2f),
            Color = Vector4.One,
            Intensity = 1f
        });
    }

    public static void AddLamps(IScene scene, int count, bool castsShadow)
    {
        for (var i = 0; i < count; i++)
        {
            var lamp = scene.CreateEntity($"lamp_{i}");
            var transform = lamp.AddComponent<TransformComponent>();
            transform.Translation = count == 1
                ? new Vector3(0f, 8f, 0f)
                : StreetLamp(i);
            lamp.AddComponent(new PointLightComponent
            {
                Color = LampColor(i),
                Intensity = 1.4f,
                Range = count == 1 ? 36f : 18f,
                CastsShadow = castsShadow
            });
        }
    }

    public static bool TryAddMesh(IScene scene, IModelFactory models, Vector3 position,
        string path, byte[]? bytes, out string? error)
    {
        error = null;
        if (models.Create(path, bytes) == null)
        {
            error = $"Mesh failed to load: {path}";
            return false;
        }

        var entity = scene.CreateEntity("mesh");
        var transform = entity.AddComponent<TransformComponent>();
        transform.Translation = position;
        entity.AddComponent(new ModelRendererComponent { ModelPath = path });
        return true;
    }

    public static bool TryAddUniqueMeshes(IScene scene, IModelFactory models, int count, byte[] bytes,
        out string? error)
    {
        var index = 0;
        foreach (var lot in Lots(count))
        {
            var path = PathBuilder.Resolve($"benchmark-unique-{index}.gltf");
            if (models.Create(path, bytes) == null)
            {
                error = $"Mesh failed to load: {path}";
                scene.Dispose();
                models.Clear();
                return false;
            }

            AddProp(scene, "mesh", lot.Position, lot.Scale, lot.Yaw, lot.Color, lot.Metallic, lot.Roughness, path);
            index++;
        }

        error = null;
        return true;
    }

    static void AddProp(IScene scene, string name, Vector3 position, Vector3 scale, float yaw,
        Vector4 color, float metallic, float roughness, string? modelPath)
    {
        var entity = scene.CreateEntity(name);
        var transform = entity.AddComponent<TransformComponent>();
        transform.Translation = position;
        transform.Scale = scale;
        transform.Rotation = new Vector3(0f, yaw, 0f);
        entity.AddComponent(new ModelRendererComponent
        {
            ModelPath = modelPath,
            Color = color,
            Metallic = metallic,
            Roughness = roughness
        });
    }

    static int SideFor(int count)
    {
        var blocks = 1;
        var usable = BlockPeriod - 1;
        while (blocks * blocks * usable * usable < count)
            blocks++;
        return blocks * BlockPeriod;
    }

    static (Vector3 Scale, Vector4 Color, float Metal, float Rough, float Yaw) Kind(uint hash)
    {
        var color = Palette[hash % (uint)Palette.Length];
        var yaw = (hash & 1u) == 0 ? 0f : MathF.PI / 2f;
        return (hash % 6) switch
        {
            0 => (new Vector3(1.05f, 4f + (hash >> 8) % 7, 1.05f), color, 0f, 0.82f, 0f),
            1 => (new Vector3(2.05f, 1.3f + (hash >> 10) % 3 * 0.6f, 1.45f), color, 0f, 0.9f, yaw),
            2 => (new Vector3(1.5f, 2.2f + (hash >> 6) % 4, 1.2f), Palette[3], 0.95f, 0.18f, 0f),
            3 => (new Vector3(0.55f, 0.55f, 0.55f), Palette[4], 0f, 0.72f, yaw),
            4 => (new Vector3(2.2f, 1.5f, 0.32f), Palette[2], 0f, 0.88f, yaw),
            _ => (new Vector3(0.42f, 6f + (hash >> 12) % 4, 0.42f), new Vector4(0.22f, 0.22f, 0.24f, 1f), 0.1f, 0.95f, 0f)
        };
    }

    static readonly (int Col, int Row)[] EightLamps =
    [
        (4, 4), (34, 4),
        (14, 14), (24, 14),
        (14, 24), (24, 24),
        (4, 34), (34, 34)
    ];

    static Vector3 StreetLamp(int index)
    {
        var side = SideFor(DistrictObjects);
        var origin = (side - 1) * 0.5f;
        var (col, row) = EightLamps[index];
        return new Vector3((col - origin) * LotSize, 12f, (row - origin) * LotSize);
    }

    static Vector4 LampColor(int index) => index switch
    {
        0 => new Vector4(1f, 0.95f, 0.85f, 1f),
        1 => new Vector4(0.75f, 0.85f, 1f, 1f),
        2 => new Vector4(1f, 0.75f, 0.55f, 1f),
        _ => new Vector4(0.9f, 0.95f, 0.8f, 1f)
    };

    static float Jitter(uint hash) => ((hash & 255) / 255f - 0.5f) * 0.25f;

    static uint Hash(int col, int row)
    {
        unchecked
        {
            var x = (uint)col * 747796405u + (uint)row * 2891336453u;
            x = (x ^ (x >> 16)) * 2246822519u;
            return x ^ (x >> 16);
        }
    }
}
