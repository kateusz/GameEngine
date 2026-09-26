using System.Numerics;

namespace Engine.Renderer.Models;

public abstract record ImportedLight;

public sealed record ImportedPointLight(Vector4 Color, float Intensity, float Range) : ImportedLight;

public sealed record ImportedDirectionalLight(Vector4 Color, Vector3 Direction) : ImportedLight;
