using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Engine.Math;
using Engine.Renderer.Models;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using ImGuiNET;

namespace Editor.Panels;

public class SceneHierarchyPanel
{
    private static readonly string[] BodyTypeStrings =
        [nameof(RigidBodyType.Static), nameof(RigidBodyType.Dynamic), nameof(RigidBodyType.Kinematic)];

    private Scene _context;
    private Entity? _selectionContext;
    public Action<Entity> EntitySelected;

    private bool _showSavePrefabPopup = false;
    private string _prefabName = "";
    private string _prefabSaveError = "";

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

        // Buttons row
        if (ImGui.Button("Add Component"))
            ImGui.OpenPopup("AddComponent");

        ImGui.SameLine();

        // Save as Prefab button
        if (ImGui.Button("Save as Prefab"))
        {
            _prefabName = _selectionContext.Name; // Default to entity name
            _prefabSaveError = "";
            _showSavePrefabPopup = true;
        }

        // Add Component popup
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

            if (!_selectionContext.HasComponent<ModelRendererComponent>())
            {
                if (ImGui.MenuItem("Model Renderer"))
                {
                    _selectionContext.AddComponent<ModelRendererComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }

            if (!_selectionContext.HasComponent<MeshComponent>())
            {
                if (ImGui.MenuItem("Mesh"))
                {
                    _selectionContext.AddComponent<MeshComponent>();
                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.EndPopup();
        }

        // Render Save Prefab popup
        RenderSavePrefabPopup();


        DrawComponent<TransformComponent>("Transform", _selectionContext, entity =>
        {
            var tc = entity.GetComponent<TransformComponent>();
            var newTranslation = tc.Translation;
            VectorPanel.DrawVec3Control("Translation", ref newTranslation);

            if (newTranslation != tc.Translation)
                tc.Translation = newTranslation;

            var rotationRadians = tc.Rotation;
            Vector3 rotationDegrees = MathHelpers.ToDegrees(rotationRadians);
            VectorPanel.DrawVec3Control("Rotation", ref rotationDegrees);
            var newRotationRadians = MathHelpers.ToRadians(rotationDegrees);

            if (newRotationRadians != tc.Rotation)
                tc.Rotation = newRotationRadians;

            var newScale = tc.Scale;
            VectorPanel.DrawVec3Control("Scale", ref newScale, 1.0f);

            if (newScale != tc.Scale)
                tc.Scale = newScale;
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
                            if (File.Exists(texturePath) &&
                                (texturePath.EndsWith(".png") || texturePath.EndsWith(".jpg")))
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

    private void RenderSavePrefabPopup()
    {
        if (_showSavePrefabPopup)
        {
            ImGui.OpenPopup("Save as Prefab");
        }

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        if (ImGui.BeginPopupModal("Save as Prefab", ref _showSavePrefabPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Prefab Name:");
            ImGui.InputText("##PrefabName", ref _prefabName, 100);
            ImGui.Separator();

            bool isValid = !string.IsNullOrWhiteSpace(_prefabName) &&
                           System.Text.RegularExpressions.Regex.IsMatch(_prefabName, @"^[a-zA-Z0-9_\- ]+$");

            if (!isValid && !string.IsNullOrEmpty(_prefabName))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped(
                    "Prefab name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.");
                ImGui.PopStyleColor();
            }

            if (!string.IsNullOrEmpty(_prefabSaveError))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped(_prefabSaveError);
                ImGui.PopStyleColor();
            }

            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Save", new Vector2(120, 0)))
            {
                try
                {
                    var currentProjectPath = GetCurrentProjectPath(); // You'll need to implement this
                    PrefabSerializer.SerializeToPrefab(_selectionContext, _prefabName, currentProjectPath);
                    Console.WriteLine($"Saved prefab: {_prefabName}.prefab");
                    _showSavePrefabPopup = false;
                    _prefabSaveError = "";
                }
                catch (Exception ex)
                {
                    _prefabSaveError = $"Failed to save prefab: {ex.Message}";
                }
            }

            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showSavePrefabPopup = false;
                _prefabSaveError = "";
            }

            ImGui.EndPopup();
        }
    }

    private string GetCurrentProjectPath()
    {
        // This should return the root path of the current project
        // You may need to adjust this based on how your project paths are managed
        var assetsPath = AssetsManager.AssetsPath;
        return Path.GetDirectoryName(assetsPath) ?? Environment.CurrentDirectory;
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

        // Add drag & drop target functionality for prefabs
        if (ImGui.BeginDragDropTarget())
        {
            unsafe
            {
                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                if (payload.NativePtr != null)
                {
                    var path = Marshal.PtrToStringUni(payload.Data);
                    if (path != null && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            string fullPath = Path.Combine(AssetsManager.AssetsPath, path);
                            PrefabSerializer.ApplyPrefabToEntity(entity, fullPath);
                            Console.WriteLine($"Applied prefab {path} to entity {entity.Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to apply prefab: {ex.Message}");
                        }
                    }
                }

                ImGui.EndDragDropTarget();
            }
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
            ImGui.TreePop();
        }

        if (entityDeleted)
        {
            _context.DestroyEntity(entity);
            if (Equals(_selectionContext, entity))
                _selectionContext = null;
        }
    }

    public Entity? GetSelectedEntity() => _selectionContext;
}