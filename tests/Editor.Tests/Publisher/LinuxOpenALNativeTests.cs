using System.Text;
using Shouldly;

namespace Editor.Tests.Publisher;

public class LinuxOpenALNativeTests
{
    [Theory]
    [InlineData("linux-x64", 62)]
    [InlineData("linux-arm64", 183)]
    public void Vendored_libopenal_is_elf_for_rid(string rid, ushort machine)
    {
        var path = Path.Combine(FindRepoRoot(), "native", "openal-soft", rid, "libopenal.so.1");
        File.Exists(path).ShouldBeTrue(path);

        var header = new byte[20];
        using (var stream = File.OpenRead(path))
            stream.ReadExactly(header);

        ((int)header[0]).ShouldBe(0x7f);
        Encoding.ASCII.GetString(header, 1, 3).ShouldBe("ELF");
        ((int)header[4]).ShouldBe(2); // ELF64
        BitConverter.ToUInt16(header, 18).ShouldBe(machine);
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Runtime", "Runtime.csproj")))
                return dir.FullName;
        }

        throw new InvalidOperationException($"Could not find repo root from {AppContext.BaseDirectory}");
    }
}
