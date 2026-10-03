namespace Benchmark;

public readonly record struct ScenePlan(
    string ResultName,
    bool DirectionalShadows,
    bool PointShadows,
    bool Sun,
    int PointLights,
    bool CastsShadow);

public static class ScenePlans
{
    public static ScenePlan Plan(string category, string testId) => (category, testId) switch
    {
        ("3D", "Cubes") => new ScenePlan("3D_Cubes", false, false, false, 0, false),
        ("3D", "InstancedMesh") => new ScenePlan("3D_InstancedMesh", false, false, false, 0, false),
        ("3D", "UniqueMeshes") => new ScenePlan("3D_UniqueMeshes", false, false, false, 0, false),
        ("3D", "GlbModels") => new ScenePlan("3D_GlbModels", true, true, false, 0, false),
        ("Lighting", "sun") => new ScenePlan("Lighting_Sun", false, false, true, 0, false),
        ("Lighting", "point1") => new ScenePlan("Lighting_Point1", false, false, false, 1, false),
        ("Lighting", "point8") => new ScenePlan("Lighting_Point8", false, false, false, 8, false),
        ("Shadows", "directional") => new ScenePlan("Shadows_Directional", true, false, true, 0, false),
        ("Shadows", "point1") => new ScenePlan("Shadows_Point1", false, true, false, 1, true),
        ("Shadows", "point8") => new ScenePlan("Shadows_Point8", false, true, false, 8, true),
        _ => throw new ArgumentOutOfRangeException(nameof(testId))
    };
}
