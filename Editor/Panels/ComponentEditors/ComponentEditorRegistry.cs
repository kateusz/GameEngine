using System.Numerics;
using ECS;
using Engine.Scene.Components;
using ImGuiNET;
using Editor.UI;

namespace Editor.Panels.ComponentEditors;

public class ComponentEditorRegistry
{
    private readonly Dictionary<Type, IComponentEditor> _editors;

    public ComponentEditorRegistry(AudioSourceComponentEditor audioSourceComponentEditor)
    {
        _editors = new Dictionary<Type, IComponentEditor>
        {
            { typeof(TransformComponent), new TransformComponentEditor() },
            { typeof(CameraComponent), new CameraComponentEditor() },
            { typeof(SpriteRendererComponent), new SpriteRendererComponentEditor() },
            { typeof(MeshComponent), new MeshComponentEditor() },
            { typeof(ModelRendererComponent), new ModelRendererComponentEditor() },
            { typeof(RigidBody2DComponent), new RigidBody2DComponentEditor() },
            { typeof(BoxCollider2DComponent), new BoxCollider2DComponentEditor() },
            { typeof(SubTextureRendererComponent), new SubTextureRendererComponentEditor() },
            { typeof(AudioSourceComponent), audioSourceComponentEditor },
            { typeof(AudioListenerComponent), new AudioListenerComponentEditor() }
        };
    }

    public void DrawAllComponents(Entity entity)
    {
        foreach (var (componentType, editor) in _editors)
        {
            editor.DrawComponent(entity);
        }

        // Special handling for script components
        ScriptComponentUI.DrawScriptComponent(entity);
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