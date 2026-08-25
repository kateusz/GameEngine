using Engine.Platform.OpenGL.Paper;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Prowl.PaperUI;
using Prowl.Quill;
using Prowl.Scribe;
using Serilog;
using PaperGui = Prowl.PaperUI.Paper;

namespace Engine.UI.Paper;

internal sealed class PaperHostServices(PaperCanvasRenderer renderer, IGraphicsContext graphicsContext)
{
  private static readonly ILogger Logger = Log.ForContext<PaperHostServices>();

  public PaperGui? Paper { get; private set; }
  public FontFile? DefaultFont { get; private set; }

  public bool TryEnsureInitialized(float logicalWidth, float logicalHeight, int framebufferWidth, int framebufferHeight)
  {
    if (!graphicsContext.IsCreated)
      return false;

    if (logicalWidth <= 0 || logicalHeight <= 0 || framebufferWidth <= 0 || framebufferHeight <= 0)
      return false;

    SilkNetContext.EnsureCurrent();

    if (Paper != null)
    {
      Paper.SetResolution(logicalWidth, logicalHeight);
      renderer.UpdateProjection(framebufferWidth, framebufferHeight);
      return true;
    }

    try
    {
      renderer.Initialize(framebufferWidth, framebufferHeight);
      DefaultFont = TryLoadDefaultFont();
      Paper = new PaperGui(renderer, logicalWidth, logicalHeight, new FontAtlasSettings());
      if (DefaultFont != null)
        Paper.AddFallbackFont(DefaultFont);
      return true;
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to initialize Paper runtime UI");
      if (Paper is IDisposable paper)
        paper.Dispose();
      Paper = null;
      renderer.Cleanup();
      return false;
    }
  }

  public void Shutdown()
  {
    SilkNetContext.EnsureCurrent();
    if (Paper is IDisposable paper)
      paper.Dispose();
    Paper = null;
    if (DefaultFont is IDisposable font)
      font.Dispose();
    DefaultFont = null;
    renderer.Cleanup();
  }

  private static FontFile? TryLoadDefaultFont()
  {
    foreach (var path in DefaultFontCandidates())
    {
      if (!File.Exists(path))
        continue;

      try
      {
        return new FontFile(path);
      }
      catch (Exception ex)
      {
        Logger.Warning(ex, "Failed to load UI font from {Path}", path);
      }
    }

    Logger.Warning("No default UI font found; Paper text will be unavailable");
    return null;
  }

  private static IEnumerable<string> DefaultFontCandidates()
  {
    yield return Path.Combine(AppContext.BaseDirectory, "assets", "fonts", "default.ttf");
    yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
    yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
  }
}
