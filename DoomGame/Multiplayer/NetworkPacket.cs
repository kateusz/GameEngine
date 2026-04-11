using System.Runtime.InteropServices;

namespace DoomGame.Multiplayer;

public enum PacketType : byte
{
    Hello = 1,
    PlayerState = 2,
    WorldState = 3,
    PlayerLeft = 4,
    Shoot = 5,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct HelloPacket
{
    public PacketType Type;
    public int PlayerId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerStatePacket
{
    public PacketType Type;
    public int PlayerId;
    public float X;
    public float Y;
    public float Angle;
    public int Health;
    public int Ammo;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerLeftPacket
{
    public PacketType Type;
    public int PlayerId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ShootPacket
{
    public PacketType Type;
    public int ShooterId;
    public float OriginX;
    public float OriginY;
    public float DirX;
    public float DirY;
}

public static class PacketSerializer
{
    public static byte[] Serialize<T>(T packet) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        var bytes = new byte[size];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(packet, handle.AddrOfPinnedObject(), false);
        }
        finally
        {
            handle.Free();
        }
        return bytes;
    }

    public static T Deserialize<T>(byte[] bytes) where T : struct
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static PacketType PeekType(byte[] bytes) =>
        bytes.Length > 0 ? (PacketType)bytes[0] : 0;
}
