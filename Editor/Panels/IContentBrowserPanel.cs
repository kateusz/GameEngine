namespace Editor.Panels;

public interface IContentBrowserPanel
{
    void Init();
    void Draw();
    void SetRootDirectory(string rootDir);
}
