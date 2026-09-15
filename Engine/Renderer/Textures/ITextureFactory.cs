namespace Engine.Renderer.Textures;

public interface ITextureFactory : IDisposable
{
    Texture2D GetWhiteTexture();
    Texture2D GetBlackTexture();
    Texture2D GetFlatNormalTexture();

    /// <param name="sRgb">
    /// When true, upload as sRGB (albedo/base color). When false, upload as linear
    /// (metallic-roughness, normals, AO, data maps).
    /// </param>
    Texture2D Create(string path, bool sRgb = false);

    (byte[] Data, int Width, int Height) DecodePreview(string path);
    Texture2D CreateFromRgba(byte[] rgba, int width, int height);
    Texture2D Create(int width, int height);
}
