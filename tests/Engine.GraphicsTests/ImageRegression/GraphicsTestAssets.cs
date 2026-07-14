using Engine.Core;
using NSubstitute;

namespace Engine.GraphicsTests.ImageRegression;

internal static class GraphicsTestAssets
{
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        var assetsPath = Path.Combine(AppContext.BaseDirectory, "assets");
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(assetsPath);
        PathBuilder.UseProjectContext(context);
        _initialized = true;
    }
}
