using System.Numerics;

namespace Engine.Renderer.Pipeline;

public readonly record struct SceneView(Matrix4x4 ViewProjection, Vector3 ViewPosition = default);
