using Engine.Scripting;
using Shouldly;

namespace Engine.Tests;

public class EngineDoesNotReferenceRoslynTests
{
    [Fact]
    public void EngineAssembly_DoesNotReferenceRoslyn()
    {
        var names = typeof(GameComponentDiscovery).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();
        names.Any(n => n.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)).ShouldBeFalse(
            string.Join(", ", names.Where(n => n.Contains("CodeAnalysis", StringComparison.Ordinal))));
    }
}
