using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels;

public class SceneHierarchyPanel
{
    private Scene _context;
    private Entity? _selectionContext;

    public SceneHierarchyPanel(Scene context)
    {
        _context = context;
    }

    public void SetContext(Scene context)
    {
        _context = context;
    }

    public void OnImGuiRender()
    {
        ImGui.Begin("Scene Hierarchy");

        foreach (var entity in Context.Instance.Entities)
        {
            DrawEntityNode(entity);
        }

        if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered())
            _selectionContext = null;

        ImGui.End();

        ImGui.Begin("Properties");
        if (_selectionContext is not null)
            DrawComponents(_selectionContext);

        ImGui.End();
    }

    private void DrawComponents(Entity entity)
    {
        var tag = entity.Name;

        byte[] buffer = new byte[256];
        Array.Clear(buffer, 0, buffer.Length);
        byte[] tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
        Array.Copy(tagBytes, buffer, Math.Min(tagBytes.Length, buffer.Length - 1));

        if (ImGui.InputText("Tag", buffer, (uint)buffer.Length))
        {
            // Convert byte[] buffer back to string
            tag = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            entity.Name = tag; // Update entity's name if needed
        }


        if (entity.HasComponent<TransformComponent>())
        {
            // ImGui TreeNode for TransformComponent
            if (ImGui.TreeNodeEx(typeof(TransformComponent).GetHashCode(), ImGuiTreeNodeFlags.DefaultOpen, "Transform"))
            {
                // Get reference to TransformComponent's Transform
                var transform = entity.GetComponent<TransformComponent>().Transform;

                // ImGui DragFloat3 for editing position
                Vector3 position = new Vector3(transform.M41, transform.M42, transform.M43);
                if (ImGui.DragFloat3("Position", ref position, 0.1f))
                {
                    // Update transform with new position values
                    transform.M41 = position[0];
                    transform.M42 = position[1];
                    transform.M43 = position[2];
                    // Update TransformComponent in entity if needed
                    entity.GetComponent<TransformComponent>().Transform = transform;
                }

                ImGui.TreePop(); // Close TreeNode
            }
        }
    }

    private void DrawEntityNode(Entity entity)
    {
        var tag = entity.Name;

        var flags = ((_selectionContext == entity) ? ImGuiTreeNodeFlags.Selected : 0) | ImGuiTreeNodeFlags.OpenOnArrow;
        var opened = ImGui.TreeNodeEx(tag, flags, tag);
        if (ImGui.IsItemClicked())
        {
            _selectionContext = entity;
        }

        if (opened)
        {
            flags = ImGuiTreeNodeFlags.OpenOnArrow;
            opened = ImGui.TreeNodeEx((IntPtr)9817239, flags, tag);
            if (opened)
                ImGui.TreePop();
            ImGui.TreePop();
        }
    }
}