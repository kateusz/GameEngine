namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshSceneGraph
{
    public static (List<SourceNode> Nodes, List<SourceLight> Lights) Flatten(ModelSceneNode? root)
    {
        if (root == null)
            return ([], []);

        var nodes = new List<SourceNode>();
        var lights = new List<SourceLight>();
        Visit(root, nodes, lights);
        return (nodes, lights);
    }

    private static int Visit(ModelSceneNode node, List<SourceNode> nodes, List<SourceLight> lights)
    {
        var lightIndex = -1;
        if (node.Light != null)
        {
            lightIndex = lights.Count;
            lights.Add(ToSourceLight(node.Light));
        }

        var index = nodes.Count;
        nodes.Add(new SourceNode
        {
            Name = node.Name,
            LocalTransform = node.LocalTransform,
            LightIndex = lightIndex
        });

        foreach (var meshIndex in node.MeshIndices)
            nodes[index].MeshIndices.Add(meshIndex);

        foreach (var child in node.Children)
            nodes[index].ChildIndices.Add(Visit(child, nodes, lights));

        return index;
    }

    public static ModelSceneNode? Unflatten(
        IReadOnlyList<SourceNode> nodes,
        IReadOnlyList<SourceLight> lights)
    {
        if (nodes.Count == 0)
            return null;

        return BuildNode(nodes, lights, 0);
    }

    private static ModelSceneNode BuildNode(
        IReadOnlyList<SourceNode> nodes,
        IReadOnlyList<SourceLight> lights,
        int index)
    {
        var record = nodes[index];
        ImportedLight? light = null;
        if (record.LightIndex >= 0 && record.LightIndex < lights.Count)
            light = ToImportedLight(lights[record.LightIndex]);

        var children = new List<ModelSceneNode>(record.ChildIndices.Count);
        foreach (var childIndex in record.ChildIndices)
            children.Add(BuildNode(nodes, lights, childIndex));

        return new ModelSceneNode(
            record.Name,
            record.MeshIndices,
            children,
            record.LocalTransform,
            light);
    }

    private static SourceLight ToSourceLight(ImportedLight light) =>
        light switch
        {
            ImportedPointLight point => new SourcePointLight(point.Color, point.Intensity, point.Range),
            ImportedDirectionalLight dir => new SourceDirectionalLight(dir.Color, dir.Direction),
            _ => throw new InvalidOperationException("Unsupported imported light type.")
        };

    private static ImportedLight ToImportedLight(SourceLight light) =>
        light switch
        {
            SourcePointLight point => new ImportedPointLight(point.Color, point.Intensity, point.Range),
            SourceDirectionalLight dir => new ImportedDirectionalLight(dir.Color, dir.Direction),
            _ => throw new InvalidOperationException("Unsupported source light type.")
        };
}
