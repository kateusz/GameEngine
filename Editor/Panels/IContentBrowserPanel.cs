namespace Editor.Panels;

public interface IContentBrowserPanel
{
    void Init();
    void Draw();
    void RenderPopups();
    void SetRootDirectory(string rootDir);
}
