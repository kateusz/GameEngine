using System.Runtime.InteropServices;
using Shouldly;

namespace Engine.Tests.Renderer;

/// <summary>
/// Pins the managed Silk.NET.Assimp struct ABI to the assimp 5.x native binaries shipped per RID
/// (libassimp.5.dylib on macOS, Assimp64.dll on Windows, libassimp.so.5 on Linux).
/// Silk.NET.Assimp 2.23.0 regenerated key structs for assimp 6 (QuatKey grew 24→32 bytes via
/// MInterpolation) while still loading the assimp 5 native — corrupting every rotation key after
/// index 0. These asserts fail on ANY platform immediately after an incompatible package bump,
/// without needing a GL context or a model cook. See Engine.csproj pin comment and
/// SkinnedAnimRotationKeysTests for the behavioral guard.
/// </summary>
[Trait("Category", "Unit")]
public class AssimpInteropAbiTests
{
    [Fact]
    public void QuatKey_MatchesAssimp5NativeLayout_24Bytes_NoInterpolationField()
    {
        Marshal.SizeOf<Silk.NET.Assimp.QuatKey>().ShouldBe(24,
            "aiQuatKey in assimp 5.x is {double mTime; float w,x,y,z} = 24 bytes; " +
            "a 32-byte managed struct means an assimp-6 ABI and scrambled rotation keys");

        typeof(Silk.NET.Assimp.QuatKey).GetField("MInterpolation")
            .ShouldBeNull("MInterpolation only exists in the assimp 6 ABI");
    }

    [Fact]
    public void VectorKey_MatchesAssimp5NativeLayout_24Bytes()
    {
        // 8 (mTime) + 12 (aiVector3D) padded to 24. Identical in the assimp 6 ABI —
        // which is why translations survived the 2.23.0 mismatch while rotations broke.
        Marshal.SizeOf<Silk.NET.Assimp.VectorKey>().ShouldBe(24);
    }

    [Fact]
    public void NativeAssimp_LoadedAtRuntime_IsMajorVersion5()
    {
        // Loads whichever native binary this RID resolves — the per-platform half of the ABI pair.
        using var assimp = Silk.NET.Assimp.Assimp.GetApi();
        assimp.GetVersionMajor().ShouldBe(5u,
            $"managed structs are pinned to the assimp 5 ABI but the loaded native is " +
            $"{assimp.GetVersionMajor()}.{assimp.GetVersionMinor()}.{assimp.GetVersionPatch()}");
    }
}
