using ECS;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

internal static class ColliderMaterialDrawer
{
    public static void Draw<T>(
        UIPropertyRenderer propertyRenderer,
        Entity entity)
        where T : class, ICollider2DMaterial, IComponent
    {
        propertyRenderer.DrawPropertyField("Density", entity,
            e => e.GetComponent<T>().Density,
            (e, v) => e.GetComponent<T>().Density = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Friction", entity,
            e => e.GetComponent<T>().Friction,
            (e, v) => e.GetComponent<T>().Friction = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Restitution", entity,
            e => e.GetComponent<T>().Restitution,
            (e, v) => e.GetComponent<T>().Restitution = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Is Trigger", entity,
            e => e.GetComponent<T>().IsTrigger,
            (e, v) => e.GetComponent<T>().IsTrigger = v);
    }
}
