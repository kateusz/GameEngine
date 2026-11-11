using Engine.Renderer.Textures.Compression;

namespace Editor.Publisher;

public partial class GamePublisher
{
    private PublishResult CookTextureSidecars()
    {
        var assets = Path.Combine(projectContext.Root!, "assets");
        if (!Directory.Exists(assets))
            return PublishResult.Ok();

        foreach (var file in Directory.EnumerateFiles(assets, "*.*", SearchOption.AllDirectories))
        {
            if (!TextureBcCook.IsCookable(file))
                continue;

            if (!TextureBcCook.TryCookFile(file, out var error))
            {
                var message = error ?? $"BC cook failed for {file}";
                Logger.Error("Texture sidecar cook failed: {Error}", message);
                return PublishResult.Failed(message);
            }
        }

        return PublishResult.Ok();
    }
}
