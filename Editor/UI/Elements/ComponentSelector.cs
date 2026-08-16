using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
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
using GameComponentEditor = Editor.Features.Components.GameComponentEditor;

namespace Editor.UI.Elements;

public static class ComponentSelector
{
    public static void Draw(Entity entity, IScene scene, GameComponentEditor gameComponentEditor, IEditorHistory history)
    {
        ButtonDrawer.DrawButton("Add Component", () => ImGui.OpenPopup("AddComponent"));

        if (ImGui.BeginPopup("AddComponent"))
        {
            DrawComponentMenuItem<CameraComponent>("Camera", entity, history, () =>
            {
                var c = new CameraComponent();
                if (scene.GetPrimaryCameraEntity() is null)
                    c.Primary = true;
                c.AspectRatio = (float)DisplayConfig.DefaultWindowWidth / DisplayConfig.DefaultWindowHeight;
                history.Execute(new AddComponentCommand(
                    entity, c, autoAddTransform: !entity.HasComponent<TransformComponent>()));
            });

            DrawComponentMenuItem<TransformComponent>("Transform", entity, history);
            DrawComponentMenuItem<SpriteRendererComponent>("Sprite Renderer", entity, history, () =>
            {
                history.Execute(new AddComponentCommand(
                    entity, new SpriteRendererComponent(),
                    autoAddTransform: !entity.HasComponent<TransformComponent>()));
            });
            DrawComponentMenuItem<SubTextureRendererComponent>("Sub Texture Renderer", entity, history, () =>
            {
                history.Execute(new AddComponentCommand(
                    entity, new SubTextureRendererComponent(),
                    autoAddTransform: !entity.HasComponent<TransformComponent>()));
            });
            if (scene.Dimension == SceneDimension.TwoD)
            {
                DrawComponentMenuItem<RigidBody2DComponent>("Rigidbody 2D", entity, history);
                DrawComponentMenuItem<BoxCollider2DComponent>("Box Collider 2D", entity, history);
                DrawComponentMenuItem<CircleCollider2DComponent>("Circle Collider 2D", entity, history);
                DrawComponentMenuItem<EdgeCollider2DComponent>("Edge Collider 2D", entity, history);
            }
            else
            {
                DrawComponentMenuItem<RigidBody3DComponent>("Rigidbody 3D", entity, history);
                DrawComponentMenuItem<BoxCollider3DComponent>("Box Collider 3D", entity, history);
                DrawComponentMenuItem<SphereCollider3DComponent>("Sphere Collider 3D", entity, history);
                DrawComponentMenuItem<CapsuleCollider3DComponent>("Capsule Collider 3D", entity, history);
            }
            DrawComponentMenuItem<ModelRendererComponent>("Model Renderer", entity, history, () =>
            {
                history.Execute(new AddComponentCommand(
                    entity, new ModelRendererComponent(),
                    autoAddTransform: !entity.HasComponent<TransformComponent>()));
            });
            DrawComponentMenuItem<SkeletalPlaybackComponent>("Skeletal Playback", entity, history);
            DrawComponentMenuItem<AudioSourceComponent>("Audio Source", entity, history);
            DrawComponentMenuItem<AudioListenerComponent>("Audio Listener", entity, history);
            DrawComponentMenuItem<AmbientLightComponent>("Ambient Light", entity, history);
            DrawComponentMenuItem<DirectionalLightComponent>("Directional Light", entity, history);
            DrawComponentMenuItem<PointLightComponent>("Point Light", entity, history, () =>
            {
                history.Execute(new AddComponentCommand(
                    entity, new PointLightComponent(),
                    autoAddTransform: !entity.HasComponent<TransformComponent>()));
            });
            DrawComponentMenuItem<SkyLightComponent>("Sky Light", entity, history);

            if (ImGui.MenuItem("Game Component"))
            {
                gameComponentEditor.RequestCreate(entity);
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static void DrawComponentMenuItem<T>(
        string name, Entity entity, IEditorHistory history, Action? customAction = null)
        where T : IComponent, new()
    {
        if (entity.HasComponent<T>()) return;
        if (!ImGui.MenuItem(name)) return;

        if (customAction != null)
            customAction();
        else
            history.Execute(new AddComponentCommand(entity, new T()));
        ImGui.CloseCurrentPopup();
    }
}
