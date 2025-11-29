using System.Numerics;

namespace Engine.Math;

public static class MathHelpers
{
    public const float RadToDegFactor = 180.0f / MathF.PI;
    public const float DegToRadFactor = MathF.PI / 180.0f;
    
    public static float RadiansToDegrees(float radians)
    {
        return radians * RadToDegFactor;
    }

    public static float DegreesToRadians(float degrees)
    {
        return degrees * DegToRadFactor;
    }
    
    public static Vector3 ToDegrees(Vector3 radians)
    {
        return new Vector3(
            RadiansToDegrees(radians.X),
            RadiansToDegrees(radians.Y),
            RadiansToDegrees(radians.Z)
        );
    }

    public static Vector3 ToRadians(Vector3 degrees)
    {
        return new Vector3(
            DegreesToRadians(degrees.X),
            DegreesToRadians(degrees.Y),
            DegreesToRadians(degrees.Z)
        );
    }
    
     public static bool DecomposeTransform(Matrix4x4 transform, out Vector3 translation, out Vector3 rotation, out Vector3 scale)
    {
        // Create a local copy of the transform matrix
        var localMatrix = transform;

        // Normalize the matrix. Ensure the last element is not zero to proceed.
        if (System.Math.Abs(localMatrix.M44) < float.Epsilon)
        {
            translation = Vector3.Zero;
            rotation = Vector3.Zero;
            scale = Vector3.One;
            return false;
        }

        // First, isolate perspective (if present) by clearing the perspective partition
        if (System.Math.Abs(localMatrix.M14) > float.Epsilon || System.Math.Abs(localMatrix.M24) > float.Epsilon || System.Math.Abs(localMatrix.M34) > float.Epsilon)
        {
            localMatrix.M14 = localMatrix.M24 = localMatrix.M34 = 0;
            localMatrix.M44 = 1;
        }

        // Extract translation
        translation = new Vector3(localMatrix.M41, localMatrix.M42, localMatrix.M43);
        localMatrix.M41 = localMatrix.M42 = localMatrix.M43 = 0;

        // Extract scale
        var rows = new Vector3[3];
        rows[0] = new Vector3(localMatrix.M11, localMatrix.M12, localMatrix.M13);
        rows[1] = new Vector3(localMatrix.M21, localMatrix.M22, localMatrix.M23);
        rows[2] = new Vector3(localMatrix.M31, localMatrix.M32, localMatrix.M33);

        // Extract and normalize the scales
        scale = new Vector3(rows[0].Length(), rows[1].Length(), rows[2].Length());

        rows[0] = Vector3.Normalize(rows[0]);
        rows[1] = Vector3.Normalize(rows[1]);
        rows[2] = Vector3.Normalize(rows[2]);

        // Detect coordinate system flip
        // In C++, this section was disabled, but it is included here for completeness
        var pdum3 = Vector3.Cross(rows[1], rows[2]);
        if (Vector3.Dot(rows[0], pdum3) < 0)
        {
            scale *= -1;
            rows[0] *= -1;
            rows[1] *= -1;
            rows[2] *= -1;
        }

        // Extract rotation (assuming no scale shearing)
        rotation.Y = (float)System.Math.Asin(-rows[0].Z);
        if (System.Math.Abs(System.Math.Cos(rotation.Y)) > float.Epsilon)
        {
            rotation.X = (float)System.Math.Atan2(rows[1].Z, rows[2].Z);
            rotation.Z = (float)System.Math.Atan2(rows[0].Y, rows[0].X);
        }
        else
        {
            rotation.X = (float)System.Math.Atan2(-rows[2].X, rows[1].Y);
            rotation.Z = 0;
        }

        return true;
    }
     
    public static Quaternion QuaternionFromEuler(Vector3 euler)
    {
        var roll = euler.X;
        var pitch = euler.Y;
        var yaw = euler.Z;

        // Compute quaternion from Euler angles
        var cy = (float)System.Math.Cos(yaw * 0.5);
        var sy = (float)System.Math.Sin(yaw * 0.5);
        var cp = (float)System.Math.Cos(pitch * 0.5);
        var sp = (float)System.Math.Sin(pitch * 0.5);
        var cr = (float)System.Math.Cos(roll * 0.5);
        var sr = (float)System.Math.Sin(roll * 0.5);

        Quaternion q;
        q.W = cy * cp * cr + sy * sp * sr;
        q.X = cy * cp * sr - sy * sp * cr;
        q.Y = sy * cp * sr + cy * sp * cr;
        q.Z = sy * cp * cr - cy * sp * sr;

        return q;
    }
    
    public static Matrix4x4 MatrixFromQuaternion(Quaternion q)
    {
        var mat = new Matrix4x4();

        var x2 = q.X + q.X;
        var y2 = q.Y + q.Y;
        var z2 = q.Z + q.Z;
        var xx = q.X * x2;
        var xy = q.X * y2;
        var xz = q.X * z2;
        var yy = q.Y * y2;
        var yz = q.Y * z2;
        var zz = q.Z * z2;
        var wx = q.W * x2;
        var wy = q.W * y2;
        var wz = q.W * z2;

        mat.M11 = 1.0f - (yy + zz);
        mat.M12 = xy + wz;
        mat.M13 = xz - wy;
        mat.M14 = 0.0f;

        mat.M21 = xy - wz;
        mat.M22 = 1.0f - (xx + zz);
        mat.M23 = yz + wx;
        mat.M24 = 0.0f;

        mat.M31 = xz + wy;
        mat.M32 = yz - wx;
        mat.M33 = 1.0f - (xx + yy);
        mat.M34 = 0.0f;

        mat.M41 = 0.0f;
        mat.M42 = 0.0f;
        mat.M43 = 0.0f;
        mat.M44 = 1.0f;

        return mat;
    }
}