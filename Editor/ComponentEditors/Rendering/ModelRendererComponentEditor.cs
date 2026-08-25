using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Renderer.Models;
using Engine.Renderer.Textures;
using Engine.Scene;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(
    IModelFactory modelFactory,
    ITextureFactory textureFactory,
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history,
    ISceneContext sceneContext) : ComponentEditor<ModelRendererComponent>(history)
{
    private string? _pendingRelativePath;
    private Entity? _pendingEntity;
    private bool _showMergeModal;

    protected override string DisplayName => "Model Renderer";

    protected override void DrawContent(ModelRendererComponent component, Entity entity)
    {
        if (entity == _pendingEntity)
            DrawPendingImportModal();

        ModelDropTarget.Draw("Model", relativePath =>
        {
            _pendingRelativePath = relativePath;
            _pendingEntity = entity;
            _showMergeModal = true;
        }, component.ModelPath);

        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);

        TextureDropTarget.Draw("Texture", relativePath =>
        {
            component.TexturePath = relativePath;
        }, textureFactory, component.TexturePath);

        propertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
            newValue => component.TilingFactor = (float)newValue);
    }

    private void DrawPendingImportModal()
    {
        if (!_showMergeModal || _pendingEntity == null || string.IsNullOrEmpty(_pendingRelativePath))
            return;

        var showModal = _showMergeModal;
        ModalDrawer.RenderConfirmationModal(
            "Merge by material?",
            ref showModal,
            "Merge submeshes with the same material into fewer draw calls. " +
            "The model hierarchy will not unpack into child entities.",
            onOk: () => ApplyPendingImport(mergeByMaterial: true),
            onCancel: () => ApplyPendingImport(mergeByMaterial: false),
            okLabel: "Merge",
            cancelLabel: "Keep hierarchy");

        _showMergeModal = showModal;
        if (!showModal && _pendingRelativePath != null)
            ApplyPendingImport(mergeByMaterial: false);
    }

    private void ApplyPendingImport(bool mergeByMaterial)
    {
        if (_pendingEntity == null || string.IsNullOrEmpty(_pendingRelativePath))
            return;

        var relativePath = _pendingRelativePath;
        var entity = _pendingEntity;
        ClearPendingImport();

        if (!entity.TryGetComponent<ModelRendererComponent>(out var component))
            return;

        var resolvedPath = PathBuilder.Resolve(relativePath);
        var model = modelFactory.Create(resolvedPath, mergeByMaterial);
        if (model == null)
            return;

        var scene = sceneContext.ActiveScene;
        if (scene == null)
        {
            component.MergeByMaterial = mergeByMaterial;
            component.ModelPath = relativePath;
            component.MeshIndex = null;
            component.SuppressDraw = false;
            return;
        }

        var command = new ImportModelHierarchyCommand(scene, entity, component, model, relativePath);
        component.MergeByMaterial = mergeByMaterial;
        history.Execute(command);
    }

    private void ClearPendingImport()
    {
        _pendingRelativePath = null;
        _pendingEntity = null;
        _showMergeModal = false;
    }
}
