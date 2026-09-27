using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Engine.Renderer.Models.RuntimeMesh;

internal ref struct RuntimeMeshBinaryWriter(Span<byte> buffer)
{
    private readonly Span<byte> _buffer = buffer;
    private int _position = 0;

    public void WriteMagic(ReadOnlySpan<byte> magic)
    {
        magic.CopyTo(_buffer.Slice(_position, magic.Length));
        _position += magic.Length;
    }

    public void WriteUInt32(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_position, 4), value);
        _position += 4;
    }

    public void WriteUInt64(ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.Slice(_position, 8), value);
        _position += 8;
    }

    public void WriteSingle(float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.Slice(_position, 4), value);
        _position += 4;
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(_buffer.Slice(_position, bytes.Length));
        _position += bytes.Length;
    }

    public void WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > RuntimeMeshFormat.MaxStringBytes)
            throw new InvalidOperationException("String too long.");

        WriteUInt32((uint)bytes.Length);
        WriteBytes(bytes);
    }

    public void WriteVector3(Vector3 value)
    {
        WriteSingle(value.X);
        WriteSingle(value.Y);
        WriteSingle(value.Z);
    }

    public void WriteVector4(Vector4 value)
    {
        WriteSingle(value.X);
        WriteSingle(value.Y);
        WriteSingle(value.Z);
        WriteSingle(value.W);
    }

    public void WriteMatrix(Matrix4x4 matrix)
    {
        WriteSingle(matrix.M11);
        WriteSingle(matrix.M12);
        WriteSingle(matrix.M13);
        WriteSingle(matrix.M14);
        WriteSingle(matrix.M21);
        WriteSingle(matrix.M22);
        WriteSingle(matrix.M23);
        WriteSingle(matrix.M24);
        WriteSingle(matrix.M31);
        WriteSingle(matrix.M32);
        WriteSingle(matrix.M33);
        WriteSingle(matrix.M34);
        WriteSingle(matrix.M41);
        WriteSingle(matrix.M42);
        WriteSingle(matrix.M43);
        WriteSingle(matrix.M44);
    }

    public void WriteZeros(int count)
    {
        _buffer.Slice(_position, count).Clear();
        _position += count;
    }
}

internal ref struct RuntimeMeshBinaryReader(ReadOnlySpan<byte> buffer, int start = 0)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;

    public int Position { get; private set; } = start;

    public void Seek(int position) => Position = position;

    public bool TryReadUInt32(out uint value)
    {
        if (Position + 4 > _buffer.Length)
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(Position, 4));
        Position += 4;
        return true;
    }

    public bool TryReadInt32(out int value)
    {
        if (!TryReadUInt32(out var raw))
        {
            value = 0;
            return false;
        }

        value = (int)raw;
        return true;
    }

    public bool TryReadUInt64(out ulong value)
    {
        if (Position + 8 > _buffer.Length)
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.Slice(Position, 8));
        Position += 8;
        return true;
    }

    public bool TryReadSingle(out float value)
    {
        if (Position + 4 > _buffer.Length)
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadSingleLittleEndian(_buffer.Slice(Position, 4));
        Position += 4;
        return true;
    }

    public bool TryReadBytes(int count, out ReadOnlySpan<byte> slice)
    {
        if (count < 0 || Position + count > _buffer.Length)
        {
            slice = default;
            return false;
        }

        slice = _buffer.Slice(Position, count);
        Position += count;
        return true;
    }

    public bool TryReadString(out string value)
    {
        value = string.Empty;
        if (!TryReadUInt32(out var byteCount) || byteCount > RuntimeMeshFormat.MaxStringBytes)
            return false;

        if (!TryReadBytes((int)byteCount, out var bytes))
            return false;

        try
        {
            value = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    public bool TryReadVector3(out Vector3 value)
    {
        value = default;
        if (!TryReadSingle(out var x) || !TryReadSingle(out var y) || !TryReadSingle(out var z))
            return false;

        value = new Vector3(x, y, z);
        return true;
    }

    public bool TryReadVector4(out Vector4 value)
    {
        value = default;
        if (!TryReadSingle(out var x) || !TryReadSingle(out var y) || !TryReadSingle(out var z) ||
            !TryReadSingle(out var w))
            return false;

        value = new Vector4(x, y, z, w);
        return true;
    }

    public bool TryReadMatrix(out Matrix4x4 matrix)
    {
        matrix = default;
        if (!TryReadSingle(out var m11) || !TryReadSingle(out var m12) || !TryReadSingle(out var m13) ||
            !TryReadSingle(out var m14) ||
            !TryReadSingle(out var m21) || !TryReadSingle(out var m22) || !TryReadSingle(out var m23) ||
            !TryReadSingle(out var m24) ||
            !TryReadSingle(out var m31) || !TryReadSingle(out var m32) || !TryReadSingle(out var m33) ||
            !TryReadSingle(out var m34) ||
            !TryReadSingle(out var m41) || !TryReadSingle(out var m42) || !TryReadSingle(out var m43) ||
            !TryReadSingle(out var m44))
            return false;

        matrix = new Matrix4x4(
            m11, m12, m13, m14,
            m21, m22, m23, m24,
            m31, m32, m33, m34,
            m41, m42, m43, m44);
        return true;
    }
}
