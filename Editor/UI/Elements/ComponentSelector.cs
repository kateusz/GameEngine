using ECS;
using Editor.ComponentEditors;
using Editor.Features.Components;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;
using SceneComponents.Audio;
using SceneComponents.Camera;
using SceneComponents.Lighting;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace Editor.UI.Elements;

public static class ComponentSelector
{
    public static void Draw(Entity entity, IScene scene, GameComponentEditor gameComponentEditor)
    {
        ButtonDrawer.DrawButton("Add Component", () => ImGui.OpenPopup("AddComponent"));

        if (ImGui.BeginPopup("AddComponent"))
        {
            DrawComponentMenuItem<CameraComponent>("Camera", entity, () =>
            {
                var c = new CameraComponent();
                if (scene.GetPrimaryCameraEntity() is null)
                    c.Primary = true;
                c.AspectRatio = (float)DisplayConfig.DefaultWindowWidth / DisplayConfig.DefaultWindowHeight;
                if (!entity.HasComponent<TransformComponent>())
                    entity.AddComponent<TransformComponent>();
                entity.AddComponent<CameraComponent>(c);
            });

            DrawComponentMenuItem<TransformComponent>("Transform", entity);
            DrawComponentMenuItem<SpriteRendererComponent>("Sprite Renderer", entity, () =>
            {
                if (!entity.HasComponent<TransformComponent>())
                    entity.AddComponent<TransformComponent>();
                entity.AddComponent<SpriteRendererComponent>();
            });
            DrawComponentMenuItem<SubTextureRendererComponent>("Sub Texture Renderer", entity, () =>
            {
                if (!entity.HasComponent<TransformComponent>())
                    entity.AddComponent<TransformComponent>();
                entity.AddComponent<SubTextureRendererComponent>();
            });
            DrawComponentMenuItem<RigidBody2DComponent>("Rigidbody 2D", entity);
            DrawComponentMenuItem<BoxCollider2DComponent>("Box Collider 2D", entity);
            DrawComponentMenuItem<ModelRendererComponent>("Model Renderer", entity, () =>
            {
                if (!entity.HasComponent<TransformComponent>())
                    entity.AddComponent<TransformComponent>();
                entity.AddComponent<ModelRendererComponent>();
            });
            DrawComponentMenuItem<AudioSourceComponent>("Audio Source", entity);
            DrawComponentMenuItem<AudioListenerComponent>("Audio Listener", entity);
            DrawComponentMenuItem<AmbientLightComponent>("Ambient Light", entity);
            DrawComponentMenuItem<DirectionalLightComponent>("Directional Light", entity);

            if (ImGui.MenuItem("Game Component"))
            {
                gameComponentEditor.RequestCreate(entity);
                ImGui.CloseCurrentPopup();
            }

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