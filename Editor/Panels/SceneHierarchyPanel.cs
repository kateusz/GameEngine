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
    private static readonly string[] BodyTypeStrings =
        [RigidBodyType.Static.ToString(), RigidBodyType.Dynamic.ToString(), RigidBodyType.Kinematic.ToString()];

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
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
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

        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Properties");
        DrawComponents();
        ImGui.End();

        // Render script component UI
        ScriptComponentUI.Render();
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

        // Use two columns for label and input
        ImGui.Columns(2, "tag_columns", false);
        ImGui.SetColumnWidth(0, 60.0f); // Label column width
        ImGui.Text("Tag");
        ImGui.NextColumn();
        ImGui.PushItemWidth(-1);
        if (ImGui.InputText("##TagInput", buffer, (uint)buffer.Length))
        {
            // Convert byte[] buffer back to string
            tag = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            _selectionContext.Name = tag; // Update entity's name if needed
        }

        ImGui.PopItemWidth();
        ImGui.Columns(1);

        // Add some vertical spacing
        ImGui.Spacing();

        if (ImGui.Button("Add Component"))
            ImGui.OpenPopup("AddComponent");

        if (ImGui.BeginPopup("AddComponent"))
        {
            if (!_selectionContext.HasComponent<CameraComponent>())
            {
                if (ImGui.MenuItem("Camera"))
                {
                    var c = new CameraComponent();
                    c.Camera.SetViewportSize(1280, 720);
                    _selectionContext.AddComponent(c);
                    
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

            if (!_selectionContext.HasComponent<NativeScriptComponent>())
            {
                if (ImGui.MenuItem("Script"))
                {
                    _selectionContext.AddComponent<NativeScriptComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }


            ImGui.EndPopup();
        }

        ImGui.PopItemWidth();

        DrawComponent<TransformComponent>("Transform", _selectionContext, entity =>
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
            
            entity.SetComponent(tc);
        });

        DrawComponent<CameraComponent>("Camera", _selectionContext, entity =>
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();
            var camera = cameraComponent.Camera;

            bool primary = cameraComponent.Primary;
            UIPropertyRenderer.DrawPropertyRow("Primary", () => ImGui.Checkbox("##Primary", ref primary));
            if (cameraComponent.Primary != primary)
                cameraComponent.Primary = primary;

            string[] projectionTypeStrings = { "Perspective", "Orthographic" };
            var currentProjectionType = camera.ProjectionType;
            string currentProjectionTypeString = projectionTypeStrings[(int)currentProjectionType];
            UIPropertyRenderer.DrawPropertyRow("Projection", () =>
            {
                if (ImGui.BeginCombo("##Projection", currentProjectionTypeString))
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
            });

            if (camera.ProjectionType == ProjectionType.Perspective)
            {
                float verticalFov = MathHelpers.RadiansToDegrees(camera.PerspectiveFOV);
                UIPropertyRenderer.DrawPropertyRow("Vertical FOV",
                    () => ImGui.DragFloat("##VerticalFOV", ref verticalFov));
                if (verticalFov != MathHelpers.RadiansToDegrees(camera.PerspectiveFOV))
                {
                    camera.SetPerspectiveVerticalFOV(MathHelpers.DegreesToRadians(verticalFov));
                }

                float perspectiveNear = camera.PerspectiveNear;
                UIPropertyRenderer.DrawPropertyRow("Near",
                    () => ImGui.DragFloat("##PerspectiveNear", ref perspectiveNear));
                if (camera.PerspectiveNear != perspectiveNear)
                {
                    camera.SetPerspectiveNearClip(perspectiveNear);
                }

                float perspectiveFar = camera.PerspectiveFar;
                UIPropertyRenderer.DrawPropertyRow("Far",
                    () => ImGui.DragFloat("##PerspectiveFar", ref perspectiveFar));
                if (camera.PerspectiveFar != perspectiveFar)
                {
                    camera.SetPerspectiveFarClip(perspectiveFar);
                }
            }

            if (camera.ProjectionType == ProjectionType.Orthographic)
            {
                float orthoSize = camera.OrthographicSize;
                UIPropertyRenderer.DrawPropertyRow("Size", () => ImGui.DragFloat("##OrthoSize", ref orthoSize));
                if (camera.OrthographicSize != orthoSize)
                {
                    camera.SetOrthographicSize(orthoSize);
                }

                float orthoNear = camera.OrthographicNear;
                UIPropertyRenderer.DrawPropertyRow("Near", () => ImGui.DragFloat("##OrthoNear", ref orthoNear));
                if (camera.OrthographicNear != orthoNear)
                {
                    camera.SetOrthographicNearClip(orthoNear);
                }

                float orthoFar = camera.OrthographicFar;
                UIPropertyRenderer.DrawPropertyRow("Far", () => ImGui.DragFloat("##OrthoFar", ref orthoFar));
                if (camera.OrthographicFar != orthoFar)
                {
                    camera.SetOrthographicFarClip(orthoFar);
                }

                bool fixedAspectRatio = cameraComponent.FixedAspectRatio;
                UIPropertyRenderer.DrawPropertyRow("Fixed Aspect Ratio",
                    () => ImGui.Checkbox("##FixedAspectRatio", ref fixedAspectRatio));
                if (cameraComponent.FixedAspectRatio != fixedAspectRatio)
                {
                    cameraComponent.FixedAspectRatio = fixedAspectRatio;
                }
            }
            
            entity.SetComponent(cameraComponent);
        });

        DrawComponent<SpriteRendererComponent>("Sprite Renderer", _selectionContext, entity =>
        {
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            var newColor = spriteRendererComponent.Color;
            UIPropertyRenderer.DrawPropertyRow("Color", () => ImGui.ColorEdit4("##Color", ref newColor));
            if (spriteRendererComponent.Color != newColor)
            {
                spriteRendererComponent.Color = newColor;
            }

            UIPropertyRenderer.DrawPropertyRow("Texture", () =>
            {
                if (ImGui.Button("Texture", new Vector2(-1, 0.0f)))
                {
                    // Optional: Handle button click logic if needed
                }
                
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
                            spriteRendererComponent.Texture = TextureFactory.Create(texturePath);
                        }

                        ImGui.EndDragDropTarget();
                    }
                }
            });
            // Drag-and-drop for texture
            

            float tillingFactor = spriteRendererComponent.TilingFactor;
            UIPropertyRenderer.DrawPropertyRow("Tiling Factor",
                () => ImGui.DragFloat("##TilingFactor", ref tillingFactor, 0.1f, 0.0f, 100.0f));
            if (spriteRendererComponent.TilingFactor != tillingFactor)
            {
                spriteRendererComponent.TilingFactor = tillingFactor;
            }
            
            entity.SetComponent(spriteRendererComponent);
        });

        DrawComponent<SubTextureRendererComponent>("Sub Texture Renderer", _selectionContext, entity =>
        {
            var c = entity.GetComponent<SubTextureRendererComponent>();
            var newCoords = c.Coords;
            UIPropertyRenderer.DrawPropertyRow("Sub texture coords",
                () => ImGui.DragFloat2("##SubTexCoords", ref newCoords));
            if (newCoords != c.Coords)
                c.Coords = newCoords;
            UIPropertyRenderer.DrawPropertyRow("Texture", () =>
            {
                if (ImGui.Button("Texture", new Vector2(-1, 0.0f)))
                {
                    // Optional: Handle button click logic if needed
                }
                
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
                            c.Texture = TextureFactory.Create(texturePath);
                        }

                        ImGui.EndDragDropTarget();
                    }
                }
            });
            
            
            entity.SetComponent(c);
        });

        DrawComponent<RigidBody2DComponent>("Rigidbody 2D", _selectionContext, entity =>
        {
            var component = entity.GetComponent<RigidBody2DComponent>();
            var currentBodyTypeString = component.BodyType.ToString();
            UIPropertyRenderer.DrawPropertyRow("Body Type", () =>
            {
                if (ImGui.BeginCombo("##BodyType", currentBodyTypeString))
                {
                    for (var i = 0; i < BodyTypeStrings.Length; i++)
                    {
                        var isSelected = currentBodyTypeString == BodyTypeStrings[i];
                        if (ImGui.Selectable(BodyTypeStrings[i], isSelected))
                        {
                            component.BodyType = (RigidBodyType)i;
                            currentBodyTypeString = BodyTypeStrings[i];
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }
            });
            bool fixedRotation = component.FixedRotation;
            UIPropertyRenderer.DrawPropertyRow("Fixed Rotation",
                () => ImGui.Checkbox("##FixedRotation", ref fixedRotation));
            if (component.FixedRotation != fixedRotation)
                component.FixedRotation = fixedRotation;
            
            entity.SetComponent(component);
        });

        DrawComponent<BoxCollider2DComponent>("Box Collider 2D", _selectionContext, entity =>
        {
            var component = entity.GetComponent<BoxCollider2DComponent>();
            var offset = component.Offset;
            UIPropertyRenderer.DrawPropertyRow("Offset", () => ImGui.DragFloat2("##Offset", ref offset));
            if (component.Offset != offset)
                component.Offset = offset;
            var size = component.Size;
            UIPropertyRenderer.DrawPropertyRow("Size", () => ImGui.DragFloat2("##Size", ref size));
            if (component.Size != size)
                component.Size = size;
            float density = component.Density;
            UIPropertyRenderer.DrawPropertyRow("Density",
                () => ImGui.DragFloat("##Density", ref density, 0.1f, 0.0f, 1.0f));
            if (component.Density != density)
                component.Density = density;
            float friction = component.Friction;
            UIPropertyRenderer.DrawPropertyRow("Friction",
                () => ImGui.DragFloat("##Friction", ref friction, 0.1f, 0.0f, 1.0f));
            if (component.Friction != friction)
                component.Friction = friction;
            float restitution = component.Restitution;
            UIPropertyRenderer.DrawPropertyRow("Restitution",
                () => ImGui.DragFloat("##Restitution", ref restitution, 0.1f, 0.0f, 1.0f));
            if (component.Restitution != restitution)
                component.Restitution = restitution;
            
            entity.SetComponent(component);
        });

        DrawComponent<MeshComponent>("Mesh", _selectionContext, entity =>
        {
            var meshComponent = entity.GetComponent<MeshComponent>();
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
            
            entity.SetComponent(meshComponent);
        });

        DrawComponent<ModelRendererComponent>("Model Renderer", _selectionContext, entity =>
        {
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();
            var newColor = modelRendererComponent.Color;
            UIPropertyRenderer.DrawPropertyRow("Color", () => ImGui.ColorEdit4("##ModelColor", ref newColor));
            if (modelRendererComponent.Color != newColor)
            {
                modelRendererComponent.Color = newColor;
            }

            UIPropertyRenderer.DrawPropertyRow("Texture", () =>
            {
                if (ImGui.Button("Texture", new Vector2(-1, 0.0f)))
                {
                    // Optional: Handle button click logic if needed
                }
                
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
            });

            // Shadow options
            bool castShadows = modelRendererComponent.CastShadows;
            UIPropertyRenderer.DrawPropertyRow("Cast Shadows", () => ImGui.Checkbox("##CastShadows", ref castShadows));
            if (modelRendererComponent.CastShadows != castShadows)
                modelRendererComponent.CastShadows = castShadows;

            bool receiveShadows = modelRendererComponent.ReceiveShadows;
            UIPropertyRenderer.DrawPropertyRow("Receive Shadows",
                () => ImGui.Checkbox("##ReceiveShadows", ref receiveShadows));
            if (modelRendererComponent.ReceiveShadows != receiveShadows)
                modelRendererComponent.ReceiveShadows = receiveShadows;
            
            entity.SetComponent(modelRendererComponent);
        });

        ScriptComponentUI.DrawScriptComponent(_selectionContext);

        ImGui.End();
    }

    public static void DrawComponent<T>(string name, Entity entity, Action<Entity> uiFunction) where T : IComponent
    {
        ImGuiTreeNodeFlags treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                                          | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                                          ImGuiTreeNodeFlags.AllowOverlap |
                                                                          ImGuiTreeNodeFlags.FramePadding;

        if (entity.HasComponent<T>())
        {
            Vector2 contentRegionAvailable = ImGui.GetContentRegionAvail();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
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

        // Remove the redundant nested TreeNodeEx call
        // Only display the entity as a single node
        if (opened)
        {
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
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.PushID(label);
        float itemWidth = ImGui.GetContentRegionAvail().X;
        float buttonWidth = 20.0f;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        Vector2 buttonSize = new Vector2(buttonWidth, ImGui.GetFrameHeight());
        float controlWidth = (itemWidth - 2 * (buttonWidth + spacing)) / 3.0f;
        // X
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        if (ImGui.Button("X", buttonSize)) values.X = resetValue;
        ImGui.PopStyleColor(3);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(controlWidth);
        ImGui.DragFloat("##X", ref values.X, 0.1f, 0.0f, 0.0f, "%.2f");
        // Y
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.8f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        if (ImGui.Button("Y", buttonSize)) values.Y = resetValue;
        ImGui.PopStyleColor(3);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(controlWidth);
        ImGui.DragFloat("##Y", ref values.Y, 0.1f, 0.0f, 0.0f, "%.2f");
        // Z
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.25f, 0.8f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.35f, 0.9f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.25f, 0.8f, 1.0f));
        if (ImGui.Button("Z", buttonSize)) values.Z = resetValue;
        ImGui.PopStyleColor(3);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(controlWidth);
        ImGui.DragFloat("##Z", ref values.Z, 0.1f, 0.0f, 0.0f, "%.2f");
        ImGui.PopID();
        ImGui.Columns(1);
    }

    // todo: remove duplication from DrawVec3Control
    public void DrawVec2Control(string label, ref Vector2 values, float resetValue = 0, float columnWidth = 100.0f)
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.PushID(label);
        float itemWidth = ImGui.GetContentRegionAvail().X;
        float buttonWidth = 20.0f;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        Vector2 buttonSize = new Vector2(buttonWidth, ImGui.GetFrameHeight());
        float controlWidth = (itemWidth - (buttonWidth + spacing)) / 2.0f;
        // X
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        if (ImGui.Button("X", buttonSize)) values.X = resetValue;
        ImGui.PopStyleColor(3);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(controlWidth);
        ImGui.InputFloat("##X", ref values.X);
        // Y
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.8f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        if (ImGui.Button("Y", buttonSize)) values.Y = resetValue;
        ImGui.PopStyleColor(3);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(controlWidth);
        ImGui.InputFloat("##Y", ref values.Y);
        ImGui.PopID();
        ImGui.Columns(1);
    }


    public Entity? GetSelectedEntity() => _selectionContext;
}