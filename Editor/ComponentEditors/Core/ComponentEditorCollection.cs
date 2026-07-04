using SceneComponents;
using SceneComponents.Audio;
using SceneComponents.Camera;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Core;

public class ComponentEditorCollection(
    TransformComponentEditor transformComponentEditor,
    CameraComponentEditor cameraComponentEditor,
    SpriteRendererComponentEditor spriteRendererComponentEditor,
    ModelRendererComponentEditor modelRendererComponentEditor,
    RigidBody2DComponentEditor rigidBody2DComponentEditor,
    BoxCollider2DComponentEditor boxCollider2DComponentEditor,
    SubTextureRendererComponentEditor subTextureRendererComponentEditor,
    AudioSourceComponentEditor audioSourceComponentEditor,
    AudioListenerComponentEditor audioListenerComponentEditor)
{
    public IReadOnlyDictionary<Type, IComponentEditor> Editors { get; } = new Dictionary<Type, IComponentEditor>
    {
        { typeof(TransformComponent), transformComponentEditor },
        { typeof(CameraComponent), cameraComponentEditor },
        { typeof(SpriteRendererComponent), spriteRendererComponentEditor },
        { typeof(ModelRendererComponent), modelRendererComponentEditor },
        { typeof(RigidBody2DComponent), rigidBody2DComponentEditor },
        { typeof(BoxCollider2DComponent), boxCollider2DComponentEditor },
        { typeof(SubTextureRendererComponent), subTextureRendererComponentEditor },
        { typeof(AudioSourceComponent), audioSourceComponentEditor },
        { typeof(AudioListenerComponent), audioListenerComponentEditor }
    };
}
