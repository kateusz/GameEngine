namespace Editor.Panels;

public interface IContentBrowserPanel : IDisposable
{
    void Init();
    void Draw();
    void RenderPopups();
    void SetRootDirectory(string rootDir);
}
