namespace Editor.Features.Project;

public interface IGameProjectScriptBootstrapper
{
    bool TryInstallScriptSdkForNewProject(string projectRoot, string projectDisplayName, out string error);

    void TryEnsureScriptSdkAfterOpen(string projectRoot);
}
