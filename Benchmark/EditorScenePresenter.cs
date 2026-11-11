using Engine.Core.Window;
using Engine.Renderer.Pipeline;

namespace Benchmark;

public interface IScenePresenter
{
    void Present(Action drawScene);
}

public sealed class DirectScenePresenter : IScenePresenter
{
    public static readonly DirectScenePresenter Instance = new();

    public void Present(Action drawScene) => drawScene();
}

/// <summary>
/// Same display path as the editor viewport: draw into an HDR target, then tonemap onto the window.
/// </summary>
public sealed class EditorScenePresenter(IGameWindow window, FxaaPass hdrTarget, TonemapPass tonemap) : IScenePresenter
{
    public void Present(Action drawScene)
    {
        var width = Pixels(window.ClientSize.X, window.ContentScale);
        var height = Pixels(window.ClientSize.Y, window.ContentScale);
        Show(width, height, tonemap.Available, drawScene, (w, h, draw) =>
        {
            var hdr = hdrTarget.DrawScene(w, h, draw);
            if (hdr == null)
                return false;

            var spec = hdr.GetSpecification();
            tonemap.Draw(hdr.GetColorAttachmentRendererId(), spec.Width, spec.Height, dest: null);
            return true;
        });
    }

    internal static void Show(
        uint width, uint height, bool tonemapReady, Action drawScene, Func<uint, uint, Action, bool> drawHdrThenTonemap)
    {
        if (width == 0 || height == 0 || !tonemapReady)
        {
            drawScene();
            return;
        }

        if (!drawHdrThenTonemap(width, height, drawScene))
            drawScene();
    }

    public static uint Pixels(float points, float scale) =>
        points <= 0f || scale <= 0f ? 0u : (uint)(points * scale);
}
