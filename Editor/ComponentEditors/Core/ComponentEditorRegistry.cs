using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors.Core;

public class ComponentEditorRegistry(
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
    ScriptComponentEditor scriptComponentEditor)
    : IComponentEditorRegistry
{
    private readonly Dictionary<Type, IComponentEditor> _editors = new()
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
        { typeof(AnimationComponent), animationComponentEditor }
    };

    public void DrawAllComponents(Entity entity)
    {
        foreach (var (_, editor) in _editors)
        {
            editor.DrawComponent(entity);
        }

        // Special handling for script components
        scriptComponentEditor.DrawScriptComponent(entity);
    }

    public static void DrawComponent<T>(string name, Entity entity, Action uiFunction) where T : IComponent
    {
        var treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                           | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                           ImGuiTreeNodeFlags.AllowOverlap |
                                                           ImGuiTreeNodeFlags.FramePadding;

        if (entity.TryGetComponent<T>(out _))
        {
            var contentRegionAvailable = ImGui.GetContentRegionAvail();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
            var lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
            ImGui.Separator();

            var open = ImGui.TreeNodeEx(typeof(T).GetHashCode().ToString(), treeNodeFlags, name);
            ImGui.PopStyleVar();

            ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
            ButtonDrawer.DrawButton("-", lineHeight, lineHeight, entity.RemoveComponent<T>);

            if (open)
            {
                uiFunction();
                ImGui.TreePop();
            }
        }
    }
}