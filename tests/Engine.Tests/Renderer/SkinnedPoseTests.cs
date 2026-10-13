using System.Numerics;
using Engine.Renderer.Models;

namespace Engine.Tests.Renderer;

public class SkinnedPoseTests
{
    [Fact]
    public void RestPose_TwoJointChain_IsIdentity()
    {
        var rootTranslation = new Vector3(1f, 2f, 3f);
        var rootRotation = Quaternion.CreateFromYawPitchRoll(0.4f, 0.2f, 0.1f);
        var rootScale = new Vector3(1f, 1.5f, 0.5f);
        var childTranslation = new Vector3(0f, 1f, 0f);
        var childRotation = Quaternion.CreateFromYawPitchRoll(0f, 1f, 0f);
        var childScale = Vector3.One;

        var rootGlobal = Local(rootTranslation, rootRotation, rootScale);
        var childGlobal = Local(childTranslation, childRotation, childScale) * rootGlobal;
        Matrix4x4.Invert(rootGlobal, out var rootInverse);
        Matrix4x4.Invert(childGlobal, out var childInverse);

        var skin = new SkinData
        {
            Joints =
            [
                new JointData
                {
                    Parent = -1,
                    InverseBind = rootInverse,
                    RestTranslation = rootTranslation,
                    RestRotation = rootRotation,
                    RestScale = rootScale
                },
                new JointData
                {
                    Parent = 0,
                    InverseBind = childInverse,
                    RestTranslation = childTranslation,
                    RestRotation = childRotation,
                    RestScale = childScale
                }
            ],
            Clips = []
        };

        var pose = new Matrix4x4[2];
        SkinnedPose.BuildRest(skin, pose);
        NearlyIdentity(pose[0]);
        NearlyIdentity(pose[1]);
    }

    private static Matrix4x4 Local(Vector3 translation, Quaternion rotation, Vector3 scale) =>
        Matrix4x4.CreateScale(scale)
        * Matrix4x4.CreateFromQuaternion(rotation)
        * Matrix4x4.CreateTranslation(translation);

    private static void NearlyIdentity(Matrix4x4 matrix)
    {
        var identity = Matrix4x4.Identity;
        Assert.True(MathF.Abs(matrix.M11 - identity.M11) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M12 - identity.M12) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M13 - identity.M13) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M14 - identity.M14) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M21 - identity.M21) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M22 - identity.M22) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M23 - identity.M23) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M24 - identity.M24) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M31 - identity.M31) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M32 - identity.M32) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M33 - identity.M33) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M34 - identity.M34) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M41 - identity.M41) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M42 - identity.M42) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M43 - identity.M43) < 1e-4f);
        Assert.True(MathF.Abs(matrix.M44 - identity.M44) < 1e-4f);
    }
}
