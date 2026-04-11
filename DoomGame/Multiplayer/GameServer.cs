using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Serilog;

namespace DoomGame.Multiplayer;

public class GameServer : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<GameServer>();

    private readonly int _port;
    private UdpClient? _udp;
    private readonly Dictionary<int, (IPEndPoint Endpoint, RemotePlayer Player)> _clients = [];
    private int _nextPlayerId = 1;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private readonly Lock _lock = new();

    public int LocalPlayerId { get; private set; }

    public GameServer(int port = 7777)
    {
        _port = port;
    }

    public void Start()
    {
        _udp = new UdpClient(_port);
        LocalPlayerId = _nextPlayerId++;
        _cts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));
        Logger.Information("Game server started on port {Port}, local player ID = {Id}", _port, LocalPlayerId);
    }

    public void BroadcastPlayerState(RemotePlayer player)
    {
        var packet = new PlayerStatePacket
        {
            Type = PacketType.PlayerState,
            PlayerId = player.Id,
            X = player.Position.X,
            Y = player.Position.Y,
            Angle = player.Angle,
            Health = player.Health,
            Ammo = player.Ammo,
        };
        var data = PacketSerializer.Serialize(packet);

        lock (_lock)
        {
            foreach (var (endpoint, _) in _clients.Values)
                TrySend(data, endpoint);
        }
    }

    public IReadOnlyList<RemotePlayer> GetRemotePlayers()
    {
        lock (_lock)
        {
            return _clients.Values.Select(c => c.Player).ToList();
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp!.ReceiveAsync(ct);
                ProcessPacket(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Server receive error");
            }
        }
    }

    private void ProcessPacket(byte[] data, IPEndPoint sender)
    {
        if (data.Length == 0) return;

        var type = PacketSerializer.PeekType(data);

        switch (type)
        {
            case PacketType.Hello:
            {
                var pkt = PacketSerializer.Deserialize<HelloPacket>(data);
                HandleHello(pkt, sender);
                break;
            }
            case PacketType.PlayerState:
            {
                var pkt = PacketSerializer.Deserialize<PlayerStatePacket>(data);
                HandlePlayerState(pkt);
                BroadcastToAll(data, sender);
                break;
            }
            case PacketType.PlayerLeft:
            {
                var pkt = PacketSerializer.Deserialize<PlayerLeftPacket>(data);
                HandlePlayerLeft(pkt.PlayerId);
                BroadcastToAll(data, sender);
                break;
            }
            case PacketType.Shoot:
                BroadcastToAll(data, sender);
                break;
        }
    }

    private void HandleHello(HelloPacket pkt, IPEndPoint sender)
    {
        lock (_lock)
        {
            if (_clients.ContainsKey(pkt.PlayerId)) return;

            int assignedId = _nextPlayerId++;
            var player = new RemotePlayer { Id = assignedId };
            _clients[assignedId] = (sender, player);

            Logger.Information("Player {Id} joined from {Endpoint}", assignedId, sender);

            // Send assigned ID back
            var reply = new HelloPacket { Type = PacketType.Hello, PlayerId = assignedId };
            TrySend(PacketSerializer.Serialize(reply), sender);

            // Notify existing clients
            var joined = new PlayerStatePacket { Type = PacketType.PlayerState, PlayerId = assignedId };
            BroadcastToAll(PacketSerializer.Serialize(joined), sender);
        }
    }

    private void HandlePlayerState(PlayerStatePacket pkt)
    {
        lock (_lock)
        {
            if (!_clients.TryGetValue(pkt.PlayerId, out var entry)) return;

            entry.Player.Position = new System.Numerics.Vector2(pkt.X, pkt.Y);
            entry.Player.Angle = pkt.Angle;
            entry.Player.Health = pkt.Health;
            entry.Player.Ammo = pkt.Ammo;
            entry.Player.LastUpdateTime = Environment.TickCount64;
        }
    }

    private void HandlePlayerLeft(int playerId)
    {
        lock (_lock)
        {
            _clients.Remove(playerId);
            Logger.Information("Player {Id} left", playerId);
        }
    }

    private void BroadcastToAll(byte[] data, IPEndPoint? exclude = null)
    {
        lock (_lock)
        {
            foreach (var (endpoint, _) in _clients.Values)
            {
                if (exclude != null && endpoint.Equals(exclude)) continue;
                TrySend(data, endpoint);
            }
        }
    }

    private void TrySend(byte[] data, IPEndPoint endpoint)
    {
        try
        {
            _udp?.Send(data, data.Length, endpoint);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to send to {Endpoint}", endpoint);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _receiveTask?.Wait(TimeSpan.FromSeconds(1));
        _udp?.Dispose();
        _cts?.Dispose();
    }
}
