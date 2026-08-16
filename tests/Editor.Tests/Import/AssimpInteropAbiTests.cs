using System.Runtime.InteropServices;
using Shouldly;
using Silk.NET.Assimp;

namespace Editor.Tests.Import;

/// <summary>
/// Silk.NET.Assimp 2.23 maps assimp 6 (QuatKey = 32 bytes) but Ultz.Native.Assimp still
/// loads libassimp.5.dylib (24-byte aiQuatKey). The importer must not use the managed indexer.
/// </summary>
[Trait("Category", "Unit")]
public class AssimpInteropAbiTests
{
    [Fact]
    public void QuatKey_ManagedLayoutIsAssimp6_32Bytes()
    {
        Marshal.SizeOf<QuatKey>().ShouldBe(32);
        typeof(QuatKey).GetField("MInterpolation").ShouldNotBeNull();
    }
}
