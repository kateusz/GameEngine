using System.Numerics;
using ECS;
using Engine.Scene.Components;
using ImGuiNET;
using Editor.UI;

namespace Editor.Panels.ComponentEditors;

public class ComponentEditorRegistry : IComponentEditorRegistry
{
    private readonly Dictionary<Type, IComponentEditor> _editors;
    private readonly ScriptComponentUI _scriptComponentUI;

    public ComponentEditorRegistry(
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
        AnimationComponentEditor animationComponentEditor,
        TileMapComponentEditor tileMapComponentEditor,
        ScriptComponentUI scriptComponentUI
        )
    {
        _editors = new Dictionary<Type, IComponentEditor>
        {
            { typeof(TransformComponent), transformComponentEditor },
            { typeof(CameraComponent), cameraComponentEditor},
            { typeof(SpriteRendererComponent), spriteRendererComponentEditor },
            { typeof(MeshComponent), meshComponentEditor },
            { typeof(ModelRendererComponent), modelRendererComponentEditor },
            { typeof(RigidBody2DComponent), rigidBody2DComponentEditor },
            { typeof(BoxCollider2DComponent), boxCollider2DComponentEditor },
            { typeof(SubTextureRendererComponent), subTextureRendererComponentEditor },
            { typeof(AudioSourceComponent), audioSourceComponentEditor },
            { typeof(AudioListenerComponent), audioListenerComponentEditor },
            { typeof(AnimationComponent), animationComponentEditor },
            { typeof(TileMapComponent), tileMapComponentEditor }
        };
        _scriptComponentUI = scriptComponentUI;
    }

    public void DrawAllComponents(Entity entity)
    {
        foreach (var (componentType, editor) in _editors)
        {
            editor.DrawComponent(entity);
        }

        // Special handling for script components
        _scriptComponentUI.DrawScriptComponent(entity);
    }

    public static void DrawComponent<T>(string name, Entity entity, Action<Entity> uiFunction) where T : IComponent
    {
        ImGuiTreeNodeFlags treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                                          | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                                          ImGuiTreeNodeFlags.AllowOverlap |
                                                                          ImGuiTreeNodeFlags.FramePadding;

        if (entity.TryGetComponent<T>(out _))
        {
            Vector2 contentRegionAvailable = ImGui.GetContentRegionAvail();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
            float lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
            ImGui.Separator();

            bool open = ImGui.TreeNodeEx(typeof(T).GetHashCode().ToString(), treeNodeFlags, name);
            ImGui.PopStyleVar();

            ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
            if (ImGui.Button("-", new Vector2(lineHeight, lineHeight)))
            {
                entity.RemoveComponent<T>();
                return;
            }

            if (open)
            {
                uiFunction(entity);
                ImGui.TreePop();
            }
        }
    }
}