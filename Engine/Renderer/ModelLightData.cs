using System.Numerics;

namespace Engine.Renderer;

public enum ModelLightType { Directional, Point, Spot }

public record ModelLightData(
    string Name,
    ModelLightType Type,
    Vector3 Position,
    Vector3 Direction,
    Vector3 Color,
    float Intensity);
