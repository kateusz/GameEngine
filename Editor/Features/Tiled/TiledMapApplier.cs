using System.Numerics;
using ECS;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace Editor.Features.Tiled;

public static class TiledMapApplier
{
    public static Entity CreateMap(IScene scene, TiledMapData data, string sourceMapPath, string entityName)
    {
        var map = scene.CreateEntity(entityName);
        map.AddComponent(new TransformComponent());
        var tilemap = new TileMapComponent();
        map.AddComponent(tilemap);
        ApplyLayers(tilemap, data, sourceMapPath);
        foreach (var obj in data.Objects)
            CreateChild(scene, map, obj);
        return map;
    }

    public static void Reimport(IScene scene, Entity mapEntity, TiledMapData data, string sourceMapPath)
    {
        if (!mapEntity.TryGetComponent<TileMapComponent>(out var tilemap))
            tilemap = mapEntity.AddComponent(new TileMapComponent());

        ApplyLayers(tilemap, data, sourceMapPath);

        var existing = new Dictionary<int, Entity>();
        foreach (var child in scene.GetChildren(mapEntity))
        {
            if (!child.TryGetComponent<TiledObjectComponent>(out var marker))
                continue;
            existing.TryAdd(marker.TiledId, child);
        }

        var incoming = new HashSet<int>();
        foreach (var obj in data.Objects)
        {
            incoming.Add(obj.Id);
            if (existing.TryGetValue(obj.Id, out var child))
                SyncTiledObjectChild(child, obj, rename: true);
            else
                CreateChild(scene, mapEntity, obj);
        }

        foreach (var (id, child) in existing)
        {
            if (!incoming.Contains(id))
                scene.DestroyEntity(child);
        }
    }

    private static void ApplyLayers(TileMapComponent tilemap, TiledMapData data, string sourceMapPath)
    {
        tilemap.SourceMapPath = sourceMapPath;
        tilemap.Width = data.Width;
        tilemap.Height = data.Height;
        tilemap.TileSize = data.TileSize;
        tilemap.Layers = data.Layers.Select(l => l.Clone()).ToList();
        tilemap.Repair();
    }

    private static void CreateChild(IScene scene, Entity map, TiledObjectData obj)
    {
        var name = string.IsNullOrWhiteSpace(obj.Name) ? $"Tiled_{obj.Id}" : obj.Name;
        var child = scene.CreateEntity(name);
        child.AddComponent(new TransformComponent(obj.LocalCenter, obj.Rotation, obj.Scale));
        SyncTiledObjectChild(child, obj, rename: false);
        scene.SetParent(child, map);
    }

    private static void SyncTiledObjectChild(Entity child, TiledObjectData obj, bool rename)
    {
        if (child.TryGetComponent<TransformComponent>(out var transform))
        {
            transform.Translation = obj.LocalCenter;
            transform.Rotation = obj.Rotation;
            transform.Scale = obj.Scale;
        }

        if (!child.TryGetComponent<TiledObjectComponent>(out var marker))
            marker = child.AddComponent(new TiledObjectComponent());
        marker.TiledId = obj.Id;
        marker.ObjectName = obj.Name;
        marker.ObjectType = obj.Type;
        marker.Properties = new Dictionary<string, string>(obj.Properties, StringComparer.Ordinal);

        if (obj.BoxHalfExtents is { } half)
        {
            if (!child.TryGetComponent<BoxCollider2DComponent>(out var box))
                box = child.AddComponent(new BoxCollider2DComponent());
            box.Size = half;
            box.Offset = Vector2.Zero;
            box.IsTrigger = obj.IsTrigger;
            if (!child.HasComponent<RigidBody2DComponent>())
                child.AddComponent(new RigidBody2DComponent());
        }
        else
        {
            if (child.HasComponent<BoxCollider2DComponent>())
                child.RemoveComponent<BoxCollider2DComponent>();
            if (child.HasComponent<RigidBody2DComponent>())
                child.RemoveComponent<RigidBody2DComponent>();
        }

        if (!string.IsNullOrWhiteSpace(obj.SubTexturePath))
        {
            if (!child.TryGetComponent<SubTextureRendererComponent>(out var sub))
                sub = child.AddComponent(new SubTextureRendererComponent());
            sub.TexturePath = obj.SubTexturePath;
            sub.Coords = obj.SubTextureCoords;
            sub.CellSize = obj.SubTextureCellSize;
            sub.SpriteSize = Vector2.One;
        }
        else if (child.HasComponent<SubTextureRendererComponent>())
        {
            child.RemoveComponent<SubTextureRendererComponent>();
        }

        if (rename && !string.IsNullOrWhiteSpace(obj.Name))
            child.Name = obj.Name;
    }
}
