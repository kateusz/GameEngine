using System.Numerics;
using Shouldly;

namespace Engine.Tests.Renderer;

/// <summary>
/// OpenGL PackMatrix4x4 + transpose=true stores Numerics matrices so GLSL <c>v * m</c>
/// matches <see cref="Vector4.Transform"/>.
/// </summary>
[Trait("Category", "Unit")]
public class SkinnedMatrixConventionTests
{
    [Fact]
    public void GlUploadedMatrix_GlslRowMultiply_MatchesVector4Transform_Translation()
    {
        var bone = Matrix4x4.CreateTranslation(3f, 4f, 5f);
        var pos = new Vector4(1f, 2f, 3f, 1f);
        var expected = Vector4.Transform(pos, bone);

        var gl = ToGlColumnMajorAfterTransposeUpload(bone);
        var skinned = GlslVec4TimesMat4(pos, gl);

        skinned.X.ShouldBe(expected.X, 1e-5f);
        skinned.Y.ShouldBe(expected.Y, 1e-5f);
        skinned.Z.ShouldBe(expected.Z, 1e-5f);
        skinned.W.ShouldBe(expected.W, 1e-5f);
    }

    [Fact]
    public void GlUploadedMatrix_GlslRowMultiply_MatchesVector4Transform_Rotation()
    {
        var bone = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, 0.7f);
        var pos = new Vector4(0.2f, -1.3f, 0.8f, 1f);
        var expected = Vector4.Transform(pos, bone);

        var gl = ToGlColumnMajorAfterTransposeUpload(bone);
        var skinned = GlslVec4TimesMat4(pos, gl);

        skinned.X.ShouldBe(expected.X, 1e-5f);
        skinned.Y.ShouldBe(expected.Y, 1e-5f);
        skinned.Z.ShouldBe(expected.Z, 1e-5f);
        skinned.W.ShouldBe(expected.W, 1e-5f);
    }

    private static float[] ToGlColumnMajorAfterTransposeUpload(Matrix4x4 matrix)
    {
        // PackMatrix4x4 row-major + UniformMatrix4(transpose:true) → GL columns = Numerics columns
        // (M11,M21,M31,M41), ...
        return
        [
            matrix.M11, matrix.M21, matrix.M31, matrix.M41,
            matrix.M12, matrix.M22, matrix.M32, matrix.M42,
            matrix.M13, matrix.M23, matrix.M33, matrix.M43,
            matrix.M14, matrix.M24, matrix.M34, matrix.M44
        ];
    }

    // GLSL vec4 * mat4: result[i] = dot(v, column i)
    private static Vector4 GlslVec4TimesMat4(Vector4 v, float[] mColMajor)
    {
        static float At(float[] m, int col, int row) => m[col * 4 + row];

        return new Vector4(
            v.X * At(mColMajor, 0, 0) + v.Y * At(mColMajor, 0, 1) + v.Z * At(mColMajor, 0, 2) + v.W * At(mColMajor, 0, 3),
            v.X * At(mColMajor, 1, 0) + v.Y * At(mColMajor, 1, 1) + v.Z * At(mColMajor, 1, 2) + v.W * At(mColMajor, 1, 3),
            v.X * At(mColMajor, 2, 0) + v.Y * At(mColMajor, 2, 1) + v.Z * At(mColMajor, 2, 2) + v.W * At(mColMajor, 2, 3),
            v.X * At(mColMajor, 3, 0) + v.Y * At(mColMajor, 3, 1) + v.Z * At(mColMajor, 3, 2) + v.W * At(mColMajor, 3, 3));
    }
}
