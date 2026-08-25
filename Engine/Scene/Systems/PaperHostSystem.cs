using ECS.Systems;
using Engine.Core.Window;
using Engine.UI.Paper;
using Input;
using Serilog;
using UI.Paper;

namespace Engine.Scene.Systems;

internal sealed class PaperHostSystem(
    PaperHostServices hostServices,
    PaperInputAdapter inputAdapter,
    PaperInputGate inputGate,
    IPointerSurface pointerSurface,
    IContentScaleProvider contentScaleProvider,
    IMouseInput mouseInput,
    IKeyboardInput keyboardInput,
    Func<IEnumerable<IPaperUi>> resolvePaperUi) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PaperHostSystem>();

    public int Priority => SystemPriorities.PaperHostSystem;

    public void OnInit()
    {
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var logicalWidth = pointerSurface.Size.X;
        var logicalHeight = pointerSurface.Size.Y;
        if (logicalWidth <= 0 || logicalHeight <= 0)
            return;
        var scale = contentScaleProvider.ContentScale;
        var fbW = System.Math.Max(1, (int)(logicalWidth * scale));
        var fbH = System.Math.Max(1, (int)(logicalHeight * scale));
        if (!hostServices.TryEnsureInitialized(logicalWidth, logicalHeight, fbW, fbH))
            return;
        var paper = hostServices.Paper;
        if (paper is null)
            return;
        inputAdapter.Forward(paper, mouseInput, keyboardInput, pointerSurface, scale);
        var began = false;
        var blocksKeyboard = false;
        try
        {
            paper.BeginFrame((float)deltaTime.TotalSeconds, scale);
            began = true;
            foreach (var ui in resolvePaperUi())
            {
                if (ui.BlocksGameplayInput)
                    blocksKeyboard = true;
                ui.Draw(paper, hostServices.DefaultFont);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Paper UI frame failed");
        }
        finally
        {
            if (began)
                paper.EndFrame();
        }
        inputGate.SetCapture(paper.WantsCapturePointer, blocksKeyboard || paper.WantsCaptureKeyboard);
    }

    public void OnShutdown() => hostServices.Shutdown();
}