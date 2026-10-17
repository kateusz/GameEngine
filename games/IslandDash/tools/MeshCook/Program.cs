using System.Reflection;

// The standalone Runtime only loads pre-cooked "<model>.mesh" siblings; the editor cooks them when a model is
// opened in a scene, but this game spawns its models from code. The cooker is internal to Engine, hence reflection.
if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("usage: MeshCook <models-directory>");
    return 2;
}

var builder = typeof(Engine.Core.DI.EngineHostOptions).Assembly
    .GetType("Engine.Renderer.Models.RuntimeMesh.RuntimeMeshBuilder", throwOnError: true)!
    .GetMethod("TryEnsureUpToDate", BindingFlags.Public | BindingFlags.Static)!;

string[] extensions = [".glb", ".gltf", ".fbx"];
var failed = 0;
foreach (var model in Directory.EnumerateFiles(args[0], "*.*", SearchOption.AllDirectories)
             .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant())))
{
    if (builder.Invoke(null, [model]) is not true)
    {
        failed++;
        Console.Error.WriteLine($"MeshCook: failed to cook {model}");
    }
}

return failed == 0 ? 0 : 1;
