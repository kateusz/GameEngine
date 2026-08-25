using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Scripting;
using Editor.Panels;
using Engine.Core;
using Engine.Scene;

namespace Editor.Features.Application;

public sealed class ProjectLifecycleCoordinator(
    IProjectManager projectManager,
    IProjectContext projectContext,
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    GameScriptWorkspace scriptWorkspace,
    IContentBrowserPanel contentBrowserPanel) : IEditorLifecycleListener
{
    private Action _projectOpenedHandler = null!;
    private Action _projectClosingHandler = null!;
    private Action _projectClosedHandler = null!;

    public void Attach()
    {
        _projectClosingHandler = () =>
        {
            if (sceneContext.State == SceneState.Play)
            {
                sceneManager.Stop();
                sceneManager.FlushPendingRuntimeStart();
            }
            else
                sceneContext.ActiveScene?.Dispose();

            scriptWorkspace.RevokeAndUnload();
        };

        _projectOpenedHandler = () =>
            contentBrowserPanel.SetRootDirectory(projectContext.AssetsPath);

        _projectClosedHandler = () =>
        {
            contentBrowserPanel.SetRootDirectory(projectContext.AssetsPath);
            sceneManager.New("");
        };

        projectManager.ProjectClosing += _projectClosingHandler;
        projectManager.ProjectOpened += _projectOpenedHandler;
        projectManager.ProjectClosed += _projectClosedHandler;
    }

    public void Detach()
    {
        projectManager.ProjectClosing -= _projectClosingHandler;
        projectManager.ProjectOpened -= _projectOpenedHandler;
        projectManager.ProjectClosed -= _projectClosedHandler;
    }
}
