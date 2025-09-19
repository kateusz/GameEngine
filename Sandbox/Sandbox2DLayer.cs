using System.Numerics;
using Engine.Core;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using NLog;

namespace Sandbox;

public class Sandbox2DLayer : ILayer
{
    private const int _mapWidth = 24;
    private const int _mapHeight = 24;

    private const string _mapTiles =
        """
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        """;


    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _orthographicCameraController;
    private Texture2D _spriteSheet;
    private SubTexture2D _textureBarrel;

    private readonly Dictionary<char, SubTexture2D> _textureMap = new();
    private readonly char[,] _mapArray;

    public Sandbox2DLayer()
    {
        _mapArray = ConvertMapTo2DArray(_mapTiles, _mapWidth, _mapHeight);
    }

    public void OnAttach()
    {
        Logger.Debug("Sandbox2DLayer OnAttach.");

        _orthographicCameraController = new OrthographicCameraController(3840.0f / 2160.0f, true);
        _spriteSheet = TextureFactory.Create("assets/textures/RPGpack_sheet_2X.png");

        _textureBarrel =
            SubTexture2D.CreateFromCoords(_spriteSheet, new Vector2(8, 2), new Vector2(128, 128), new Vector2(1, 1));

        _textureMap['D'] =
            SubTexture2D.CreateFromCoords(_spriteSheet, new Vector2(6, 11), new Vector2(128, 128), new Vector2(1, 1));
        _textureMap['W'] = SubTexture2D.CreateFromCoords(_spriteSheet, new Vector2(11, 11), new Vector2(128, 128),
            new Vector2(1, 1));
    }

    public void OnDetach()
    {
        Logger.Debug("Sandbox2DLayer OnDetach.");
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _orthographicCameraController.OnUpdate(timeSpan);

        Graphics2D.Instance.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        Graphics2D.Instance.Clear();

        Graphics2D.Instance.BeginScene(_orthographicCameraController.Camera);

        Graphics2D.Instance.DrawQuad(Vector2.Zero, Vector2.One, new Vector4(100, 100, 100, 100));

        Graphics2D.Instance.DrawLine(Vector3.Zero, new Vector3(5, 5, 0), new Vector4(100, 100, 100, 100), 5);
        Graphics2D.Instance.DrawRect(Vector3.Zero, new Vector2(5, 5), new Vector4(100, 100, 100, 100), 5);

        // for (var row = 0; row < _mapHeight; row++)
        // {
        //     for (var col = 0; col < _mapWidth; col++)
        //     {
        //         var subTextureCode = _mapArray[row, col];
        //         var subTexture = _textureMap[subTextureCode];
        //         
        //         // _mapHeight - row because openGl reads from bottom left to top right
        //         Renderer2D.Instance.DrawQuad(new Vector3(col - _mapWidth / 2.0f, _mapHeight - row / 2.0f, 0.5f), new Vector2(1, 1), rotation: 0, subTexture);
        //     }
        // }

        Graphics2D.Instance.EndScene();
    }

    public void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);

        _orthographicCameraController.OnEvent(@event);
    }

    public void OnImGuiRender()
    {
    }

    static char[,] ConvertMapTo2DArray(string mapTiles, int mapWidth, int mapHeight)
    {
        char[,] mapArray = new char[mapHeight, mapWidth];
        string[] rows = mapTiles.Trim().Split('\n');

        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                mapArray[row, col] = rows[row][col];
            }
        }

        return mapArray;
    }
}