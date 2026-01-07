using ECS;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Scene.Components;
using ImGuiNET;
using CameraController = Editor.assets.scripts.CameraController;

namespace Editor.UI.Elements;

public static class ComponentSelector
{
    public static void Draw(Entity entity)
    {
        ButtonDrawer.DrawButton("Add Component", () => ImGui.OpenPopup("AddComponent"));

        if (ImGui.BeginPopup("AddComponent"))
        {
            DrawComponentMenuItem<CameraComponent>("Camera", entity, () =>
            {
                var c = new CameraComponent();
                c.Camera.SetViewportSize(DisplayConfig.DefaultWindowWidth, DisplayConfig.DefaultWindowHeight);
                entity.AddComponent<CameraComponent>(c);
                entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
                {
                    ScriptableEntity = new CameraController()
                });
            });

            DrawComponentMenuItem<SpriteRendererComponent>("Sprite Renderer", entity);
            DrawComponentMenuItem<SubTextureRendererComponent>("Sub Texture Renderer", entity);
            DrawComponentMenuItem<TileMapComponent>("TileMap", entity);
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