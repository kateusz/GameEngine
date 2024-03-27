using Common;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using OpenTK.Mathematics;

namespace _1.FlappyBirdClone;

public class GameLayer : Layer
{
    private readonly Level _level;
    private OrthographicCameraController _cameraController;

    private TimeSpan _time;
    private bool _blink;
    private GameState _state;

    public GameLayer(string name) : base(name)
    {
        _level = new Level();
        
        _state = GameState.Play;
    }

    public override void OnAttach()
    {
        _level.Init();
        CreateCamera(1280, 1024);

        //m_Font = io.Fonts->AddFontFromFileTTF("assets/OpenSans-Regular.ttf", 120.0f);
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController.OnUpdate(timeSpan);
        
        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();
        
        _time += timeSpan;

        if (_level.IsGameOver())
            _state = GameState.GameOver;

        //var playerPos = _level.Player.Position;
        //_cameraController.Camera.SetPosition(new Vector3(playerPos.X, playerPos.Y, 0.0f));

        switch (_state)
        {
            case GameState.Play:
            {
                _level.OnUpdate(timeSpan);
                break;
            }
        }
        
        // Render
        RendererCommand.SetClearColor(new Vector4(0.0f, 0.0f, 0.0f, 1));
        RendererCommand.Clear();

        Renderer2D.Instance.BeginScene(_cameraController.Camera);
        _level.OnRender();
        Renderer2D.Instance.EndScene();
    }

    void CreateCamera(int width, int height)
    {
        float aspectRatio = (float)width / (float)height;

        float camWidth = 8.0f;
        float bottom = -camWidth;
        float top = camWidth;
        float left = bottom * aspectRatio;
        float right = top * aspectRatio;
        
        var camera = new OrthographicCamera(left, right, bottom, top);
        _cameraController = new OrthographicCameraController(camera, aspectRatio);
    }
}