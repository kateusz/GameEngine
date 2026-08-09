using Silk.NET.Assimp;

namespace Engine.Renderer;

/// <summary>Shared Assimp scene import settings for all cook paths.</summary>
internal static class AssimpSceneImport
{
    // assimp/config.h — FBX files often import with ~100 uniform scale (cm); convert to meters.
    private const string FbxConvertToMeters = "AI_CONFIG_FBX_CONVERT_TO_M";

    public static unsafe Silk.NET.Assimp.Scene* Import(Assimp assimp, string path, uint flags)
    {
        var props = assimp.CreatePropertyStore();
        try
        {
            assimp.SetImportPropertyInteger(props, FbxConvertToMeters, 1);
            return assimp.ImportFileExWithProperties(path, flags, (FileIO*)null, props);
        }
        finally
        {
            assimp.ReleasePropertyStore(props);
        }
    }
}
