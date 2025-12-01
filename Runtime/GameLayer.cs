using System.Numerics;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Serilog;

namespace Runtime;

/// <summary>
/// The main game layer for the runtime.
/// This layer handles rendering and input for the published game.
/// </summary>
public class GameLayer : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<GameLayer>();

    private readonly IGraphics2D _graphics2D;
    private IOrthographicCameraController? _cameraController;

    public GameLayer(IGraphics2D graphics2D)
    {
        _graphics2D = graphics2D;
    }

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("Game layer attached.");
        _cameraController = new OrthographicCameraController(1920.0f / 1080.0f, true);
    }

    public void OnDetach()
    {
        Logger.Information("Game layer detached.");
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController?.OnUpdate(timeSpan);
        
        _graphics2D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        _graphics2D.Clear();

        if (_cameraController != null)
        {
            _graphics2D.BeginScene(_cameraController.Camera);
            // Game rendering logic will be handled by the scripting system
            _graphics2D.EndScene();
        }
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        Logger.Debug("Input event: {Event}", windowEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        Logger.Debug("Window event: {Event}", windowEvent);
        _cameraController?.OnEvent(windowEvent);
    }

    public void Draw()
    {
    }
}
