using System.Numerics;
using ECS;
using Engine.Math;
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
            if (ImGui.TreeNodeEx(typeof(TransformComponent).GetHashCode(), ImGuiTreeNodeFlags.DefaultOpen, "Transform"))
            {
                var tc = entity.GetComponent<TransformComponent>();
                
                var newTranslation = tc.Translation;
                DrawVec3Control("Translation", ref newTranslation);

                if (newTranslation != tc.Translation) 
                    tc.Translation = newTranslation;

                var rotationRadians = tc.Rotation;
                Vector3 rotationDegrees = MathHelpers.ToDegrees(rotationRadians);
                DrawVec3Control("Rotation", ref rotationDegrees);
                var newRotationRadians = MathHelpers.ToRadians(rotationDegrees);

                if (newRotationRadians != tc.Rotation) 
                    tc.Rotation = newRotationRadians;

                var newScale = tc.Scale;
                DrawVec3Control("Scale", ref newScale, 1.0f);

                if (newScale != tc.Scale)
                    tc.Scale = newScale;

                ImGui.TreePop();
            }
        }

        if (entity.HasComponent<SpriteRendererComponent>())
        {
            if (ImGui.TreeNodeEx(typeof(SpriteRendererComponent).GetHashCode(), ImGuiTreeNodeFlags.DefaultOpen,
                    "Sprite Renderer"))
            {
                var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
                var newColor = spriteRendererComponent.Color;
                ImGui.ColorEdit4("Square Color", ref newColor);

                if (spriteRendererComponent.Color != newColor)
                {
                    spriteRendererComponent.Color = newColor;
                }

                ImGui.Separator();
                ImGui.End();
            }
        }
        
        if (entity.HasComponent<CameraComponent>())
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();
            if (ImGui.TreeNodeEx("Camera", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var camera = cameraComponent.Camera;

                bool primary = cameraComponent.Primary;
                if (ImGui.Checkbox("Primary", ref primary))
                {
                    cameraComponent.Primary = primary;
                }

                string[] projectionTypeStrings = { "Perspective", "Orthographic" };
                var currentProjectionType = camera.ProjectionType;
                string currentProjectionTypeString = projectionTypeStrings[(int)currentProjectionType];

                if (ImGui.BeginCombo("Projection", currentProjectionTypeString))
                {
                    for (int i = 0; i < projectionTypeStrings.Length; i++)
                    {
                        bool isSelected = currentProjectionTypeString == projectionTypeStrings[i];
                        if (ImGui.Selectable(projectionTypeStrings[i], isSelected))
                        {
                            currentProjectionTypeString = projectionTypeStrings[i];
                            camera.SetProjectionType((ProjectionType)i);
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }

                if (camera.ProjectionType == ProjectionType.Perspective)
                {
                    float verticalFov = MathHelpers.RadiansToDegrees(camera.PerspectiveFOV);
                    if (ImGui.DragFloat("Vertical FOV", ref verticalFov))
                    {
                        camera.SetPerspectiveVerticalFOV(MathHelpers.DegreesToRadians(verticalFov));
                    }

                    float perspectiveNear = camera.PerspectiveNear;
                    if (ImGui.DragFloat("Near", ref perspectiveNear))
                    {
                        camera.SetPerspectiveNearClip(perspectiveNear);
                    }

                    float perspectiveFar = camera.OrthographicFar;
                    if (ImGui.DragFloat("Far", ref perspectiveFar))
                    {
                        camera.SetPerspectiveFarClip(perspectiveFar);
                    }
                }

                if (camera.ProjectionType == ProjectionType.Orthographic)
                {
                    float orthoSize = camera.OrthographicSize;
                    if (ImGui.DragFloat("Size", ref orthoSize))
                    {
                        camera.SetOrthographicSize(orthoSize);
                    }

                    float orthoNear = camera.OrthographicNear;
                    if (ImGui.DragFloat("Near", ref orthoNear))
                    {
                        camera.SetOrthographicNearClip(orthoNear);
                    }

                    float orthoFar = camera.OrthographicFar;
                    if (ImGui.DragFloat("Far", ref orthoFar))
                    {
                        camera.SetOrthographicFarClip(orthoFar);
                    }

                    bool fixedAspectRatio = cameraComponent.FixedAspectRatio;
                    if (ImGui.Checkbox("Fixed Aspect Ratio", ref fixedAspectRatio))
                    {
                        cameraComponent.FixedAspectRatio = fixedAspectRatio;
                    }
                }

                ImGui.TreePop();
            }
        }

        ImGui.End();

        //controller.Render();
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

    public void DrawVec3Control(string label, ref Vector3 values, float resetValue = 0.0f, float columnWidth = 100.0f)
    {
        ImGui.PushID(label);

        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, columnWidth);
        ImGui.Text(label);
        ImGui.NextColumn();

        // Get the total width available for the DragFloat controls
        float itemWidth = ImGui.CalcItemWidth();
        float buttonWidth = 20.0f; // Width of the reset button (X, Y, Z)
        float spacing = ImGui.GetStyle().ItemSpacing.X;

        // Define button sizes
        Vector2 buttonSize = new Vector2(buttonWidth, ImGui.GetFrameHeight());

        // Handle X
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        if (ImGui.Button("X", buttonSize))
            values.X = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(itemWidth / 3 - buttonSize.X - spacing);
        ImGui.DragFloat("##X", ref values.X, 0.1f, 0.0f, 0.0f, "%.2f");

        // Handle Y
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.8f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        if (ImGui.Button("Y", buttonSize))
            values.Y = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(itemWidth / 3 - buttonSize.X - spacing);
        ImGui.DragFloat("##Y", ref values.Y, 0.1f, 0.0f, 0.0f, "%.2f");

        // Handle Z
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.25f, 0.8f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.35f, 0.9f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.25f, 0.8f, 1.0f));
        if (ImGui.Button("Z", buttonSize))
            values.Z = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(itemWidth / 3 - buttonSize.X - spacing);
        ImGui.DragFloat("##Z", ref values.Z, 0.1f, 0.0f, 0.0f, "%.2f");

        ImGui.Columns(1);

        ImGui.PopID();
    }
}