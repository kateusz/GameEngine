using Engine.Platform.SilkNet;

namespace Engine.Renderer;

public static class SkyboxFactory
{
    /// <summary>
    /// Creates a skybox from 6 separate image files
    /// </summary>
    /// <param name="rightPath">Path to the right (+X) face texture</param>
    /// <param name="leftPath">Path to the left (-X) face texture</param>
    /// <param name="topPath">Path to the top (+Y) face texture</param>
    /// <param name="bottomPath">Path to the bottom (-Y) face texture</param>
    /// <param name="frontPath">Path to the front (+Z) face texture</param>
    /// <param name="backPath">Path to the back (-Z) face texture</param>
    /// <returns>A new Skybox instance</returns>
    public static Skybox Create(
        string rightPath, 
        string leftPath, 
        string topPath, 
        string bottomPath, 
        string frontPath, 
        string backPath)
    {
        return new Skybox(new[] 
        { 
            rightPath, 
            leftPath, 
            topPath, 
            bottomPath, 
            frontPath, 
            backPath 
        });
    }

    /// <summary>
    /// Creates a skybox from a directory containing the 6 face textures with standard naming
    /// </summary>
    /// <param name="directory">The directory containing the skybox textures</param>
    /// <param name="extension">The file extension (default: ".png")</param>
    /// <param name="prefix">The file name prefix (default: "")</param>
    /// <returns>A new Skybox instance</returns>
    /// <remarks>
    /// Expected file names: 
    /// - prefix_right.extension
    /// - prefix_left.extension
    /// - prefix_top.extension
    /// - prefix_bottom.extension
    /// - prefix_front.extension
    /// - prefix_back.extension
    /// </remarks>
    public static Skybox CreateFromDirectory(string directory, string extension = ".png", string prefix = "")
    {
        string rightPath = Path.Combine(directory, $"{prefix}right{extension}");
        string leftPath = Path.Combine(directory, $"{prefix}left{extension}");
        string topPath = Path.Combine(directory, $"{prefix}top{extension}");
        string bottomPath = Path.Combine(directory, $"{prefix}bottom{extension}");
        string frontPath = Path.Combine(directory, $"{prefix}front{extension}");
        string backPath = Path.Combine(directory, $"{prefix}back{extension}");

        return Create(rightPath, leftPath, topPath, bottomPath, frontPath, backPath);
    }
}