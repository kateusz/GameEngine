using Engine.Scene.Components;
using Engine.Scene.Components.Lights;

namespace Editor.ComponentEditors.Core;

public class ComponentEditorCollection(
    TransformComponentEditor transformComponentEditor,
    CameraComponentEditor cameraComponentEditor,
    SpriteRendererComponentEditor spriteRendererComponentEditor,
    MeshComponentEditor meshComponentEditor,
    ModelRendererComponentEditor modelRendererComponentEditor,
    RigidBody2DComponentEditor rigidBody2DComponentEditor,
    BoxCollider2DComponentEditor boxCollider2DComponentEditor,
    SubTextureRendererComponentEditor subTextureRendererComponentEditor,
    AudioSourceComponentEditor audioSourceComponentEditor,
    AudioListenerComponentEditor audioListenerComponentEditor,
    PointLightComponentEditor pointLightComponentEditor,
    DirectionalLightComponentEditor directionalLightComponentEditor,
    AmbientLightComponentEditor ambientLightComponentEditor)
{
    public IReadOnlyDictionary<Type, IComponentEditor> Editors { get; } = new Dictionary<Type, IComponentEditor>
    {
        { typeof(TransformComponent), transformComponentEditor },
        { typeof(CameraComponent), cameraComponentEditor },
        { typeof(SpriteRendererComponent), spriteRendererComponentEditor },
        { typeof(MeshComponent), meshComponentEditor },
        { typeof(ModelRendererComponent), modelRendererComponentEditor },
        { typeof(RigidBody2DComponent), rigidBody2DComponentEditor },
        { typeof(BoxCollider2DComponent), boxCollider2DComponentEditor },
        { typeof(SubTextureRendererComponent), subTextureRendererComponentEditor },
        { typeof(AudioSourceComponent), audioSourceComponentEditor },
        { typeof(AudioListenerComponent), audioListenerComponentEditor },
        { typeof(PointLightComponent), pointLightComponentEditor },
        { typeof(DirectionalLightComponent), directionalLightComponentEditor },
        { typeof(AmbientLightComponent), ambientLightComponentEditor }
    };
}
