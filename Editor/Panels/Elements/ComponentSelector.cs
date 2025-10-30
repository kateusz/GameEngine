using ECS;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.Elements;

public static class ComponentSelector
{
    public static void Draw(Entity entity)
    {
        if (ImGui.Button("Add Component"))
            ImGui.OpenPopup("AddComponent");

        if (ImGui.BeginPopup("AddComponent"))
        {
            DrawComponentMenuItem<CameraComponent>("Camera", entity, () =>
            {
                var c = new CameraComponent();
                c.Camera.SetViewportSize(1280, 720);
                entity.AddComponent<CameraComponent>(c);
                entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
                {
                    ScriptableEntity = new CameraController()
                });
            });

            DrawComponentMenuItem<SpriteRendererComponent>("Sprite Renderer", entity);
            DrawComponentMenuItem<SubTextureRendererComponent>("Sub Texture Renderer", entity);
            DrawComponentMenuItem<RigidBody2DComponent>("Rigidbody 2D", entity);
            DrawComponentMenuItem<BoxCollider2DComponent>("Box Collider 2D", entity);
            DrawComponentMenuItem<ModelRendererComponent>("Model Renderer", entity);
            DrawComponentMenuItem<MeshComponent>("Mesh", entity);
            DrawComponentMenuItem<AudioSourceComponent>("Audio Source", entity);
            DrawComponentMenuItem<AudioListenerComponent>("Audio Listener", entity);
            DrawComponentMenuItem<AnimationComponent>("Animation Component", entity);

            ImGui.EndPopup();
        }
    }

    private static void DrawComponentMenuItem<T>(string name, Entity entity, Action? customAction = null)
        where T : IComponent, new()
    {
        if (entity.HasComponent<T>()) return;
        if (!ImGui.MenuItem(name)) return;

        if (customAction != null)
            customAction();
        else
            entity.AddComponent<T>();
        ImGui.CloseCurrentPopup();
    }
}