using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Engine.Math;
using Engine.Renderer.Models;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels;

public class SceneHierarchyPanel
{
    private static readonly string[] BodyTypeStrings = [RigidBodyType.Static.ToString(), RigidBodyType.Dynamic.ToString(), RigidBodyType.Kinematic.ToString()];

    private Scene _context;
    private Entity? _selectionContext;

    public Action<Entity> EntitySelected;

    public SceneHierarchyPanel(Scene context)
    {
        _context = context;
    }

    public void SetContext(Scene context)
    {
        _context = context;
        _selectionContext = null;
    }

    public void OnImGuiRender()
    {
        ImGui.Begin("Scene Hierarchy");

        foreach (var entity in _context.Entities)
        {
            DrawEntityNode(entity);
        }

        if (ImGui.IsMouseDown(0) && ImGui.IsWindowHovered())
            _selectionContext = null;

        // Right-click on blank space
        if (ImGui.BeginPopupContextWindow("WindowContextMenu",
                ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
        {
            if (ImGui.MenuItem("Create Empty Entity"))
            {
                var entity = _context.CreateEntity("Empty Entity");
                entity.AddComponent<TransformComponent>();
                entity.AddComponent<IdComponent>();
            }
            
            // Add a new menu item for 3D objects
            if (ImGui.MenuItem("Create 3D Entity"))
            {
                var entity = _context.CreateEntity("3D Entity");
                entity.AddComponent<TransformComponent>();
                entity.AddComponent<MeshComponent>();
                entity.AddComponent<ModelRendererComponent>();
            }

            ImGui.EndPopup();
        }

        ImGui.End();

        ImGui.Begin("Properties");
        DrawComponents();
        ImGui.End();
    }

    private void DrawComponents()
    {
        if (_selectionContext is null)
            return;

        var tag = _selectionContext.Name;

        byte[] buffer = new byte[256];
        Array.Clear(buffer, 0, buffer.Length);
        byte[] tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
        Array.Copy(tagBytes, buffer, Math.Min(tagBytes.Length, buffer.Length - 1));

        if (ImGui.InputText("Tag", buffer, (uint)buffer.Length))
        {
            // Convert byte[] buffer back to string
            tag = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            _selectionContext.Name = tag; // Update entity's name if needed
        }

        ImGui.SameLine();
        ImGui.PushItemWidth(-1);

        if (ImGui.Button("Add Component"))
            ImGui.OpenPopup("AddComponent");

        if (ImGui.BeginPopup("AddComponent"))
        {
            if (!_selectionContext.HasComponent<CameraComponent>())
            {
                if (ImGui.MenuItem("Camera"))
                {
                    _selectionContext.AddComponent<CameraComponent>();
                    _selectionContext.AddComponent(new NativeScriptComponent
                    {
                        ScriptableEntity = new CameraController()
                    });
                    
                    ImGui.CloseCurrentPopup();
                }
            }

            if (!_selectionContext.HasComponent<SpriteRendererComponent>())
            {
                if (ImGui.MenuItem("Sprite Renderer"))
                {
                    _selectionContext.AddComponent<SpriteRendererComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }
            
            if (!_selectionContext.HasComponent<SubTextureRendererComponent>())
            {
                if (ImGui.MenuItem("Sub Texture Renderer"))
                {
                    _selectionContext.AddComponent<SubTextureRendererComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }

            if (!_selectionContext.HasComponent<RigidBody2DComponent>())
            {
                if (ImGui.MenuItem("Rigidbody 2D"))
                {
                    _selectionContext.AddComponent<RigidBody2DComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }

            if (!_selectionContext.HasComponent<BoxCollider2DComponent>())
            {
                if (ImGui.MenuItem("Box Collider 2D"))
                {
                    _selectionContext.AddComponent<BoxCollider2DComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }
            
            if (!_selectionContext.HasComponent<MeshComponent>())
            {
                if (ImGui.MenuItem("Mesh Component"))
                {
                    _selectionContext.AddComponent<MeshComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }

            if (!_selectionContext.HasComponent<ModelRendererComponent>())
            {
                if (ImGui.MenuItem("Model Renderer"))
                {
                    _selectionContext.AddComponent<ModelRendererComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }


            ImGui.EndPopup();
        }

        ImGui.PopItemWidth();

        DrawComponent<TransformComponent>("Transform", _selectionContext, tc =>
        {
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
        });

        DrawComponent<CameraComponent>("Camera", _selectionContext, cameraComponent =>
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

                float perspectiveFar = camera.PerspectiveFar;
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
        });
        
        DrawComponent<SpriteRendererComponent>("Sprite Renderer", _selectionContext, spriteRendererComponent =>
        {
            var newColor = spriteRendererComponent.Color;
            ImGui.ColorEdit4("Color", ref newColor);

            if (spriteRendererComponent.Color != newColor)
            {
                spriteRendererComponent.Color = newColor;
            }

            // Render the "Texture" button with a fixed width of 100.0f and automatic height (0.0f)
            if (ImGui.Button("Texture", new Vector2(100.0f, 0.0f)))
            {
                // Optional: Handle button click logic if needed
            }

            // Begin drag-and-drop target handling
            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    // Accept the drag-and-drop payload with the label "CONTENT_BROWSER_ITEM"
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                    if (payload.NativePtr != null)
                    {
                        // Convert the payload data (wchar_t*) to a C# string
                        var path = Marshal.PtrToStringUni(payload.Data);
                        if (path is null)
                        {
                            return;
                        }

                        // Combine the asset path and the dragged path
                        string texturePath = Path.Combine(AssetsManager.AssetsPath, path);

                        // Set the component's texture (assuming Texture2D.Create takes a string path)
                        spriteRendererComponent.Texture = TextureFactory.Create(texturePath);
                    }

                    // End the drag-and-drop target
                    ImGui.EndDragDropTarget();
                }
            }

            // Render a float slider for the "Tiling Factor"
            float tillingFactor = spriteRendererComponent.TilingFactor;
            if (ImGui.DragFloat("Tiling Factor", ref tillingFactor, 0.1f, 0.0f, 100.0f))
            {
                spriteRendererComponent.TilingFactor = tillingFactor;
            }
        });
        
        DrawComponent<SubTextureRendererComponent>("Sub Texture Renderer", _selectionContext, c =>
        {
            var newCoords = c.Coords;
            DrawVec2Control("Sub texture coords", ref newCoords);

            if (newCoords != c.Coords)
                c.Coords = newCoords;

            // Render the "Texture" button with a fixed width of 100.0f and automatic height (0.0f)
            if (ImGui.Button("Texture", new Vector2(100.0f, 0.0f)))
            {
                // Optional: Handle button click logic if needed
            }
            
            // Begin drag-and-drop target handling
            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    // Accept the drag-and-drop payload with the label "CONTENT_BROWSER_ITEM"
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                    if (payload.NativePtr != null)
                    {
                        // Convert the payload data (wchar_t*) to a C# string
                        var path = Marshal.PtrToStringUni(payload.Data);
                        if (path is null)
                        {
                            return;
                        }

                        // Combine the asset path and the dragged path
                        string texturePath = Path.Combine(AssetsManager.AssetsPath, path);

                        // Set the component's texture (assuming Texture2D.Create takes a string path)
                        c.Texture = TextureFactory.Create(texturePath);
                    }

                    // End the drag-and-drop target
                    ImGui.EndDragDropTarget();
                }
            }
        });

        DrawComponent<RigidBody2DComponent>("Rigidbody 2D", _selectionContext, component =>
        {
            // Get the current body type string based on the component's type
            var currentBodyTypeString = component.BodyType.ToString();

            // Begin the combo box for "Body Type"
            if (ImGui.BeginCombo("Body Type", currentBodyTypeString))
            {
                // Iterate over all body types
                for (var i = 0; i < BodyTypeStrings.Length; i++)
                {
                    var isSelected = currentBodyTypeString == BodyTypeStrings[i];

                    if (ImGui.Selectable(BodyTypeStrings[i], isSelected))
                    {
                        // Update the selected type
                        component.BodyType = (RigidBodyType)i;
                        currentBodyTypeString = BodyTypeStrings[i];
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }
            
            // Checkbox for "Fixed Rotation"

            var fixedRotation = component.FixedRotation;
            if (ImGui.Checkbox("Fixed Rotation", ref fixedRotation))
            {
                component.FixedRotation = fixedRotation;
            }
        });

        DrawComponent<BoxCollider2DComponent>("Box Collider 2D", _selectionContext, component =>
        {
            var offset = component.Offset;
            if (ImGui.DragFloat2("Offset", ref offset))
            {
                component.Offset = offset;
            }
            
            var size = component.Size;
            if (ImGui.DragFloat2("Size", ref size))
            {
                component.Size = size;
            }
            
            var density = component.Density;
            if (ImGui.DragFloat("Density", ref density, 0.1f, 0.0f, 1.0f))
            {
                component.Density = density;
            }
            
            var friction = component.Friction;
            if (ImGui.DragFloat("Friction", ref friction, 0.1f, 0.0f, 1.0f))
            {
                component.Friction = friction;
            }
            
            var restitution = component.Restitution;
            if (ImGui.DragFloat("Restitution", ref restitution, 0.1f, 0.0f, 1.0f))
            {
                component.Restitution = restitution;
            }
            
            // todo: restitution treshold
        });
        
        DrawComponent<MeshComponent>("Mesh", _selectionContext, meshComponent =>
{
    // Render the "Load OBJ" button
    if (ImGui.Button("Load OBJ", new Vector2(100.0f, 0.0f)))
    {
        // In a real implementation, you'd use a file dialog here
        // For now, we'll use a hardcoded path for demonstration
        //string objPath = "assets/objModels/cube.model";
        //string objPath = "assets/objModels/tetrahedron.model";
        //string objPath = "assets/objModels/torus.model";
        string objPath = "assets/objModels/person.model";
        if (File.Exists(objPath))
        {
            var mesh = MeshFactory.Create(objPath);
            mesh.Initialize();
            meshComponent.SetMesh(mesh);
        }
    }

    // Display the mesh name
    ImGui.Text($"Mesh: {meshComponent.Mesh.Name}");
    ImGui.Text($"Vertices: {meshComponent.Mesh.Vertices.Count}");
    ImGui.Text($"Indices: {meshComponent.Mesh.Indices.Count}");
    
    // Begin drag-and-drop target handling for OBJ files
    if (ImGui.BeginDragDropTarget())
    {
        unsafe
        {
            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
            if (payload.NativePtr != null)
            {
                var path = Marshal.PtrToStringUni(payload.Data);
                if (path is not null && path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                {
                    string fullPath = Path.Combine(AssetsManager.AssetsPath, path);
                    var mesh = MeshFactory.Create(fullPath);
                    mesh.Initialize();
                    meshComponent.SetMesh(mesh);
                }
            }
            ImGui.EndDragDropTarget();
        }
    }
});

// Add this DrawComponent method for ModelRendererComponent

DrawComponent<ModelRendererComponent>("Model Renderer", _selectionContext, modelRendererComponent =>
{
    var newColor = modelRendererComponent.Color;
    ImGui.ColorEdit4("Color", ref newColor);

    if (modelRendererComponent.Color != newColor)
    {
        modelRendererComponent.Color = newColor;
    }
    
    // Render the "Texture" button
    if (ImGui.Button("Texture", new Vector2(100.0f, 0.0f)))
    {
        // Optional: Handle button click logic if needed
    }
    
    // Begin drag-and-drop target handling
    if (ImGui.BeginDragDropTarget())
    {
        unsafe
        {
            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
            if (payload.NativePtr != null)
            {
                var path = Marshal.PtrToStringUni(payload.Data);
                if (path is null)
                {
                    return;
                }

                string texturePath = Path.Combine(AssetsManager.AssetsPath, path);
                if (File.Exists(texturePath) && (texturePath.EndsWith(".png") || texturePath.EndsWith(".jpg")))
                {
                    modelRendererComponent.OverrideTexture = TextureFactory.Create(texturePath);
                }
            }
            ImGui.EndDragDropTarget();
        }
    }
    
    // Shadow options
    bool castShadows = modelRendererComponent.CastShadows;
    if (ImGui.Checkbox("Cast Shadows", ref castShadows))
    {
        modelRendererComponent.CastShadows = castShadows;
    }
    
    bool receiveShadows = modelRendererComponent.ReceiveShadows;
    if (ImGui.Checkbox("Receive Shadows", ref receiveShadows))
    {
        modelRendererComponent.ReceiveShadows = receiveShadows;
    }
});

        ImGui.End();
    }


    public static void DrawComponent<T>(string name, Entity entity, Action<T> uiFunction) where T : Component
    {
        ImGuiTreeNodeFlags treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                                          | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                                          ImGuiTreeNodeFlags.AllowOverlap |
                                                                          ImGuiTreeNodeFlags.FramePadding;

        if (entity.HasComponent<T>())
        {
            T component = entity.GetComponent<T>();
            Vector2 contentRegionAvailable = ImGui.GetContentRegionAvail();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
            float lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
            ImGui.Separator();

            bool open = ImGui.TreeNodeEx(typeof(T).GetHashCode().ToString(), treeNodeFlags, name);
            ImGui.PopStyleVar();

            ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
            if (ImGui.Button("+", new Vector2(lineHeight, lineHeight)))
            {
                ImGui.OpenPopup("ComponentSettings");
            }

            bool removeComponent = false;
            if (ImGui.BeginPopup("ComponentSettings"))
            {
                if (ImGui.MenuItem("Remove component"))
                    removeComponent = true;
                ImGui.EndPopup();
            }

            if (open)
            {
                uiFunction(component);
                ImGui.TreePop();
            }

            if (removeComponent)
                entity.RemoveComponent<T>();
        }
    }

    public void SetSelectedEntity(Entity entity)
    {
        _selectionContext = entity;
    }

    private void DrawEntityNode(Entity entity)
    {
        var tag = entity.Name;

        var flags = (_selectionContext?.Id == entity.Id ? ImGuiTreeNodeFlags.Selected : 0) |
                    ImGuiTreeNodeFlags.OpenOnArrow;
        var opened = ImGui.TreeNodeEx(tag, flags, tag);
        if (ImGui.IsItemClicked())
        {
            EntitySelected.Invoke(entity);
            _selectionContext = entity;
        }

        bool entityDeleted = false;
        if (ImGui.BeginPopupContextItem())
        {
            if (ImGui.MenuItem("Delete Entity"))
                entityDeleted = true;

            ImGui.EndPopup();
        }

        if (opened)
        {
            flags = ImGuiTreeNodeFlags.OpenOnArrow;
            opened = ImGui.TreeNodeEx((IntPtr)9817239, flags, tag);
            if (opened)
                ImGui.TreePop();
            ImGui.TreePop();
        }

        if (entityDeleted)
        {
            _context.DestroyEntity(entity);
            if (Equals(_selectionContext, entity))
                _selectionContext = null;
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

    // todo: remove duplication from DrawVec3Control
    public void DrawVec2Control(string label, ref Vector2 values, float resetValue = 0, float columnWidth = 100.0f)
    {
        ImGui.PushID(label);

        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, columnWidth);
        ImGui.Text(label);
        ImGui.NextColumn();

        // Get the total width available for the DragFloat controls
        var itemWidth = ImGui.CalcItemWidth();
        const float buttonWidth = 20.0f; // Width of the reset button (X, Y, Z)
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        // Define button sizes
        var buttonSize = new Vector2(buttonWidth, ImGui.GetFrameHeight());

        // Handle X
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        if (ImGui.Button("X", buttonSize))
            values.X = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(itemWidth / 3 - buttonSize.X - spacing);
        ImGui.InputFloat("##X", ref values.X);

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
        ImGui.InputFloat("##Y", ref values.Y);

        ImGui.Columns(1);

        ImGui.PopID();
    }

    
    public Entity? GetSelectedEntity() => _selectionContext;
}