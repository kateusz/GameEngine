using ECS;

namespace Engine.Scene.Components;

/// <summary>
/// Initializes component cloners in the ComponentRegistry.
/// Called once during application startup to register custom cloning logic.
/// </summary>
public static class ComponentRegistryInitializer
{
    private static bool _initialized = false;

    /// <summary>
    /// Registers all component cloners. Safe to call multiple times.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        // TransformComponent - simple value types, use default shallow copy
        ComponentRegistry.RegisterCloner<TransformComponent>(c => new TransformComponent
        {
            Translation = c.Translation,
            Rotation = c.Rotation,
            Scale = c.Scale
        });

        // SpriteRendererComponent - has Texture reference
        ComponentRegistry.RegisterCloner<SpriteRendererComponent>(c => new SpriteRendererComponent
        {
            Color = c.Color,
            Texture = c.Texture, // Shared reference is intentional - textures are immutable
            TilingFactor = c.TilingFactor
        });

        // SubTextureRendererComponent - has Texture reference
        ComponentRegistry.RegisterCloner<SubTextureRendererComponent>(c => new SubTextureRendererComponent
        {
            Coords = c.Coords,
            Texture = c.Texture // Shared reference is intentional - textures are immutable
        });

        // CameraComponent - has SceneCamera object
        ComponentRegistry.RegisterCloner<CameraComponent>(c => new CameraComponent
        {
            Camera = c.Camera, // SceneCamera should implement proper cloning if needed
            Primary = c.Primary,
            FixedAspectRatio = c.FixedAspectRatio
        });

        // NativeScriptComponent - ScriptableEntity should not be cloned
        ComponentRegistry.RegisterCloner<NativeScriptComponent>(c => new NativeScriptComponent
        {
            ScriptableEntity = null // Reset script instance for new entity
        });

        // RigidBody2DComponent - RuntimeBody must be reset
        ComponentRegistry.RegisterCloner<RigidBody2DComponent>(c => new RigidBody2DComponent
        {
            BodyType = c.BodyType,
            FixedRotation = c.FixedRotation,
            RuntimeBody = null // Reset physics body - will be recreated on runtime start
        });

        // BoxCollider2DComponent - simple value types
        ComponentRegistry.RegisterCloner<BoxCollider2DComponent>(c => new BoxCollider2DComponent
        {
            Size = c.Size,
            Offset = c.Offset,
            Density = c.Density,
            Friction = c.Friction,
            Restitution = c.Restitution,
            RestitutionThreshold = c.RestitutionThreshold,
            IsTrigger = c.IsTrigger
        });

        // MeshComponent - Mesh reference should be shared
        ComponentRegistry.RegisterCloner<MeshComponent>(c => new MeshComponent
        {
            Mesh = c.Mesh // Shared reference - meshes are typically immutable
        });

        // ModelRendererComponent - has optional Texture reference
        ComponentRegistry.RegisterCloner<ModelRendererComponent>(c => new ModelRendererComponent
        {
            Color = c.Color,
            OverrideTexture = c.OverrideTexture, // Shared reference
            CastShadows = c.CastShadows,
            ReceiveShadows = c.ReceiveShadows
        });

        _initialized = true;
    }
}
