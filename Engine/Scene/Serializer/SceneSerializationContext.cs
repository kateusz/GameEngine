using System.Numerics;
using System.Text.Json.Serialization;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Engine.Scene.Serializer;

/// <summary>
/// JSON source generation context for scene serialization.
/// Provides compile-time serialization code generation for improved performance and AOT compatibility.
/// </summary>
[JsonSerializable(typeof(Scene))]
[JsonSerializable(typeof(SceneCamera))]
[JsonSerializable(typeof(ProjectionType))]
[JsonSerializable(typeof(TransformComponent))]
[JsonSerializable(typeof(CameraComponent))]
[JsonSerializable(typeof(SpriteRendererComponent))]
[JsonSerializable(typeof(RigidBody2DComponent))]
[JsonSerializable(typeof(RigidBodyType))]
[JsonSerializable(typeof(BoxCollider2DComponent))]
[JsonSerializable(typeof(NativeScriptComponent))]
[JsonSerializable(typeof(Texture2D))]
[JsonSerializable(typeof(Vector2))]
[JsonSerializable(typeof(Vector3))]
[JsonSerializable(typeof(Vector4))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(object))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    IgnoreReadOnlyProperties = false,
    IncludeFields = false,
    Converters = [typeof(Vector2Converter), typeof(Vector3Converter), typeof(Vector4Converter), typeof(JsonStringEnumConverter)])]
public partial class SceneSerializationContext : JsonSerializerContext
{
}
