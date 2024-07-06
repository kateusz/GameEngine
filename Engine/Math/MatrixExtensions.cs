using System.Numerics;

namespace Engine.Math;

public static class MatrixExtensions
{
    public static Vector4 GetColumn(this Matrix4x4 matrix, int index)
    {
        switch (index)
        {
            case 0:
                return new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41);
            case 1:
                return new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42);
            case 2:
                return new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43);
            case 3:
                return new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44);
            default:
                throw new ArgumentOutOfRangeException("Index must be between 0 and 3.");
        }
    }
}