using System.Net;
using System.Net.Sockets;
using Serilog;

namespace DoomGame.Multiplayer;

public class GameClient : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<GameClient>();

    private readonly string _serverHost;
    private readonly int _serverPort;
    private UdpClient? _udp;
    private IPEndPoint? _serverEndpoint;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private readonly Dictionary<int, RemotePlayer> _remotePlayers = [];
    private readonly Lock _lock = new();

    public int LocalPlayerId { get; private set; }
    public bool IsConnected { get; private set; }

    public GameClient(string serverHost, int serverPort = 7777)
    {
        _serverHost = serverHost;
        _serverPort = serverPort;
    }

    public void Connect()
    {
        _udp = new UdpClient();
        _serverEndpoint = new IPEndPoint(IPAddress.Parse(_serverHost), _serverPort);
        _cts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

        // Send Hello to request a player ID
        var hello = new HelloPacket { Type = PacketType.Hello, PlayerId = 0 };
        Send(PacketSerializer.Serialize(hello));

        Logger.Information("Connecting to server {Host}:{Port}", _serverHost, _serverPort);
    }

    public void SendPlayerState(int playerId, System.Numerics.Vector2 position, float angle, int health, int ammo)
    {
        if (!IsConnected) return;

        var packet = new PlayerStatePacket
        {
            Type = PacketType.PlayerState,
            PlayerId = playerId,
            X = position.X,
            Y = position.Y,
            Angle = angle,
            Health = health,
            Ammo = ammo,
        };
        Send(PacketSerializer.Serialize(packet));
    }

    public void SendShoot(int shooterId, System.Numerics.Vector2 origin, System.Numerics.Vector2 direction)
    {
        if (!IsConnected) return;

        var packet = new ShootPacket
        {
            Type = PacketType.Shoot,
            ShooterId = shooterId,
            OriginX = origin.X,
            OriginY = origin.Y,
            DirX = direction.X,
            DirY = direction.Y,
        };
        Send(PacketSerializer.Serialize(packet));
    }

    public void Disconnect()
    {
        if (!IsConnected || LocalPlayerId == 0) return;

        var packet = new PlayerLeftPacket { Type = PacketType.PlayerLeft, PlayerId = LocalPlayerId };
        Send(PacketSerializer.Serialize(packet));
        IsConnected = false;
    }

    public IReadOnlyList<RemotePlayer> GetRemotePlayers()
    {
        lock (_lock)
        {
            return _remotePlayers.Values.ToList();
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp!.ReceiveAsync(ct);
                ProcessPacket(result.Buffer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Client receive error");
            }
        }
    }

    private void ProcessPacket(byte[] data)
    {
        if (data.Length == 0) return;

        var type = PacketSerializer.PeekType(data);

        switch (type)
        {
            case PacketType.Hello:
            {
                var pkt = PacketSerializer.Deserialize<HelloPacket>(data);
                LocalPlayerId = pkt.PlayerId;
                IsConnected = true;
                Logger.Information("Connected to server, assigned player ID {Id}", LocalPlayerId);
                break;
            }
            case PacketType.PlayerState:
            {
                var pkt = PacketSerializer.Deserialize<PlayerStatePacket>(data);
                if (pkt.PlayerId != LocalPlayerId)
                    UpdateRemotePlayer(pkt);
                break;
            }
            case PacketType.PlayerLeft:
            {
                var pkt = PacketSerializer.Deserialize<PlayerLeftPacket>(data);
                lock (_lock) { _remotePlayers.Remove(pkt.PlayerId); }
                Logger.Information("Player {Id} left", pkt.PlayerId);
                break;
            }
        }
    }

    private void UpdateRemotePlayer(PlayerStatePacket pkt)
    {
        lock (_lock)
        {
            if (!_remotePlayers.TryGetValue(pkt.PlayerId, out var player))
            {
                player = new RemotePlayer { Id = pkt.PlayerId };
                _remotePlayers[pkt.PlayerId] = player;
                Logger.Information("New remote player {Id}", pkt.PlayerId);
            }

            player.Position = new System.Numerics.Vector2(pkt.X, pkt.Y);
            player.Angle = pkt.Angle;
            player.Health = pkt.Health;
            player.Ammo = pkt.Ammo;
            player.LastUpdateTime = Environment.TickCount64;
        }
    }

    private void Send(byte[] data)
    {
        try
        {
            _udp?.Send(data, data.Length, _serverEndpoint);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Send failed");
        }
    }

    public void Dispose()
    {
        Disconnect();
        _cts?.Cancel();
        _receiveTask?.Wait(TimeSpan.FromSeconds(1));
        _udp?.Dispose();
        _cts?.Dispose();
    }
}
