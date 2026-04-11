using System.Numerics;
using DoomGame.Player;
using Serilog;

namespace DoomGame.Multiplayer;

public enum NetworkMode
{
    Offline,
    Server,
    Client,
}

public class NetworkManager : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<NetworkManager>();

    private readonly NetworkMode _mode;
    private readonly GameServer? _server;
    private readonly GameClient? _client;
    private float _sendInterval = 1f / 20f; // 20 Hz state updates
    private float _sendTimer;

    public int LocalPlayerId { get; private set; }
    public NetworkMode Mode => _mode;

    public NetworkManager(NetworkMode mode, string serverHost = "127.0.0.1", int port = 7777)
    {
        _mode = mode;

        switch (mode)
        {
            case NetworkMode.Server:
                _server = new GameServer(port);
                _server.Start();
                LocalPlayerId = _server.LocalPlayerId;
                Logger.Information("Running as server (player ID {Id})", LocalPlayerId);
                break;
            case NetworkMode.Client:
                _client = new GameClient(serverHost, port);
                _client.Connect();
                Logger.Information("Running as client, connecting to {Host}:{Port}", serverHost, port);
                break;
            default:
                LocalPlayerId = 1;
                break;
        }
    }

    public void Update(TimeSpan deltaTime, LocalPlayer localPlayer)
    {
        _sendTimer -= (float)deltaTime.TotalSeconds;
        if (_sendTimer > 0f) return;
        _sendTimer = _sendInterval;

        if (_mode == NetworkMode.Client && _client is { IsConnected: true })
        {
            LocalPlayerId = _client.LocalPlayerId;
            _client.SendPlayerState(
                LocalPlayerId,
                localPlayer.Position,
                localPlayer.Angle,
                localPlayer.Health,
                localPlayer.Ammo);

            if (localPlayer.JustShot)
                _client.SendShoot(LocalPlayerId, localPlayer.Position, localPlayer.Direction);
        }
        else if (_mode == NetworkMode.Server && _server != null)
        {
            // Server broadcasts local player state as well
            var localState = new RemotePlayer
            {
                Id = LocalPlayerId,
                Position = localPlayer.Position,
                Angle = localPlayer.Angle,
                Health = localPlayer.Health,
                Ammo = localPlayer.Ammo,
            };
            _server.BroadcastPlayerState(localState);
        }
    }

    public IReadOnlyList<RemotePlayer> GetRemotePlayers()
    {
        return _mode switch
        {
            NetworkMode.Server => _server?.GetRemotePlayers() ?? [],
            NetworkMode.Client => _client?.GetRemotePlayers() ?? [],
            _ => [],
        };
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _server?.Dispose();
        _client?.Dispose();
    }
}
