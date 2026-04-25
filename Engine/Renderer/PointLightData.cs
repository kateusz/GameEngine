using System.Numerics;

namespace Engine.Renderer;

public readonly record struct PointLightData(Vector3 Position, Vector3 Color, float Intensity);
