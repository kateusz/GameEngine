using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using Engine.Renderer.Textures;
using SceneComponents.Rendering;

namespace Editor.Features.Tiled;

public static class TiledMapParser
{
    private const uint FlipH = 0x80000000;
    private const uint FlipV = 0x40000000;
    private const uint FlipD = 0x20000000;
    private const uint FlipMask = FlipH | FlipV | FlipD;

    public static (TiledMapData? Result, string? Error) FromFile(
        string tmjPath, Func<string, string>? toAssetRelative = null)
    {
        if (!File.Exists(tmjPath))
            return (null, $"Map file not found: {tmjPath}");

        string json;
        try
        {
            json = File.ReadAllText(tmjPath);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to read map: {ex.Message}");
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(tmjPath)) ?? "";
        return FromJson(json, dir, toAssetRelative);
    }

    public static (TiledMapData? Result, string? Error) FromJson(
        string json, string mapDirectory, Func<string, string>? toAssetRelative = null)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid JSON: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, "Map JSON must be an object");

            var orientation = GetString(root, "orientation");
            if (!string.Equals(orientation, "orthogonal", StringComparison.OrdinalIgnoreCase))
                return (null, "Only orthogonal Tiled maps are supported");

            if (GetBool(root, "infinite") == true)
                return (null, "Infinite Tiled maps are not supported");

            var width = GetInt(root, "width");
            var height = GetInt(root, "height");
            var tileWidth = GetInt(root, "tilewidth");
            var tileHeight = GetInt(root, "tileheight");
            if (width < 1 || height < 1)
                return (null, "Map width and height must be >= 1");
            if (tileWidth < 1 || tileHeight < 1 || tileWidth != tileHeight)
                return (null, "Map tiles must be square and >= 1 pixel");

            var warnings = new List<string>();
            if (!TryLoadTilesets(root, mapDirectory, tileWidth, toAssetRelative, warnings, out var tilesets, out var tilesetError))
                return (null, tilesetError!);

            var layers = new List<TileMapLayer>();
            var objects = new List<TiledObjectData>();
            var seenObjectIds = new HashSet<int>();
            var unknownGids = 0;
            var diagonalFlips = 0;

            if (root.TryGetProperty("layers", out var layersEl) && layersEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var layer in layersEl.EnumerateArray())
                {
                    var outcome = ProcessLayer(
                        layer, width, height, tileWidth, tilesets, layers, objects, seenObjectIds,
                        warnings, ref unknownGids, ref diagonalFlips);
                    if (outcome is not null)
                        return (null, outcome);
                }
            }

            if (unknownGids > 0)
                warnings.Add($"{unknownGids} unknown tile GID(s) stored as empty");
            if (diagonalFlips > 0)
                warnings.Add($"{diagonalFlips} tile(s) with diagonal flip — rotation ignored");

            return (new TiledMapData
            {
                Width = width,
                Height = height,
                TileSize = tileWidth,
                Layers = layers,
                Objects = objects,
                Warnings = warnings
            }, null);
        }
    }

    private static string? ProcessLayer(
        JsonElement layer,
        int width,
        int height,
        int tileSize,
        List<LoadedTileset> tilesets,
        List<TileMapLayer> layers,
        List<TiledObjectData> objects,
        HashSet<int> seenObjectIds,
        List<string> warnings,
        ref int unknownGids,
        ref int diagonalFlips)
    {
        var type = GetString(layer, "type");
        if (string.Equals(type, "group", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Group layer '{GetString(layer, "name")}' flattened");
            if (layer.TryGetProperty("layers", out var nested) && nested.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in nested.EnumerateArray())
                {
                    var error = ProcessLayer(
                        child, width, height, tileSize, tilesets, layers, objects, seenObjectIds,
                        warnings, ref unknownGids, ref diagonalFlips);
                    if (error is not null)
                        return error;
                }
            }

            return null;
        }

        if (string.Equals(type, "imagelayer", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Image layer '{GetString(layer, "name")}' skipped");
            return null;
        }

        if (string.Equals(type, "objectgroup", StringComparison.OrdinalIgnoreCase))
        {
            ParseObjects(layer, height, tileSize, tilesets, objects, seenObjectIds, warnings);
            return null;
        }

        if (!string.Equals(type, "tilelayer", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Layer '{GetString(layer, "name")}' type '{type}' skipped");
            return null;
        }

        return ParseTileLayer(layer, width, height, tileSize, tilesets, layers, ref unknownGids, ref diagonalFlips);
    }

    private static string? ParseTileLayer(
        JsonElement layer,
        int width,
        int height,
        int tileSize,
        List<LoadedTileset> tilesets,
        List<TileMapLayer> layers,
        ref int unknownGids,
        ref int diagonalFlips)
    {
        var name = GetString(layer, "name") ?? "";
        if (!TryReadLayerGids(layer, name, out var gids, out var dataError))
            return dataError;

        LoadedTileset? layerTileset = null;
        var parsed = new TileMapLayer
        {
            Name = name,
            Visible = GetBool(layer, "visible") != false,
            TileSize = tileSize,
            Tiles = new int[width * height],
            Flags = new byte[width * height]
        };
        Array.Fill(parsed.Tiles, -1);

        var count = System.Math.Min(gids.Length, width * height);
        for (var i = 0; i < count; i++)
        {
            var ty = i / width;
            var x = i % width;
            var gid = gids[i];
            if (gid == 0)
                continue;

            var decoded = DecodeGid(gid, tilesets);
            if (decoded is null)
            {
                unknownGids++;
                continue;
            }

            if (layerTileset is null)
                layerTileset = decoded.Value.Tileset;
            else if (!ReferenceEquals(layerTileset, decoded.Value.Tileset))
                return $"Tile layer '{parsed.Name}' uses more than one tileset";

            if (decoded.Value.Diagonal)
                diagonalFlips++;

            var y = height - 1 - ty;
            byte flags = 0;
            if (decoded.Value.HFlip)
                flags |= TileMapComponent.FlipH;
            if (decoded.Value.VFlip)
                flags |= TileMapComponent.FlipV;
            parsed.SetTile(width, height, x, y, decoded.Value.Local, flags);
        }

        if (layerTileset is not null)
        {
            parsed.TexturePath = layerTileset.ImagePath;
            parsed.Margin = layerTileset.Margin;
            parsed.Spacing = layerTileset.Spacing;
        }

        layers.Add(parsed);
        return null;
    }

    private static bool TryReadLayerGids(JsonElement layer, string name, out uint[] gids, out string? error)
    {
        gids = [];
        error = null;
        if (!layer.TryGetProperty("data", out var data))
        {
            error = $"Tile layer '{name}' has no data";
            return false;
        }

        if (data.ValueKind == JsonValueKind.Array)
        {
            gids = new uint[data.GetArrayLength()];
            var i = 0;
            foreach (var cell in data.EnumerateArray())
            {
                TryGetGid(cell, out gids[i]);
                i++;
            }

            return true;
        }

        if (data.ValueKind != JsonValueKind.String)
        {
            error = $"Tile layer '{name}' data must be a JSON array or encoded string";
            return false;
        }

        var payload = data.GetString() ?? "";
        var encoding = GetString(layer, "encoding") ?? "";
        if (encoding.Equals("csv", StringComparison.OrdinalIgnoreCase))
            return TryParseCsvGids(payload, name, out gids, out error);

        if (!encoding.Equals("base64", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Tile layer '{name}' encoding '{encoding}' is not supported";
            return false;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            error = $"Tile layer '{name}' has invalid base64 data";
            return false;
        }

        var compression = GetString(layer, "compression") ?? "";
        try
        {
            bytes = DecompressLayerBytes(bytes, compression);
        }
        catch (NotSupportedException)
        {
            error = $"Tile layer '{name}' compression '{compression}' is not supported";
            return false;
        }
        catch (InvalidDataException ex)
        {
            error = $"Tile layer '{name}' failed to decompress: {ex.Message}";
            return false;
        }

        if (bytes.Length % 4 != 0)
        {
            error = $"Tile layer '{name}' decoded data length is not a multiple of 4";
            return false;
        }

        gids = new uint[bytes.Length / 4];
        for (var i = 0; i < gids.Length; i++)
            gids[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i * 4));
        return true;
    }

    private static bool TryParseCsvGids(string csv, string name, out uint[] gids, out string? error)
    {
        error = null;
        var parts = csv.Split(',', StringSplitOptions.TrimEntries);
        gids = new uint[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
                continue;
            if (!uint.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out gids[i]))
            {
                error = $"Tile layer '{name}' has invalid CSV GID '{parts[i]}'";
                gids = [];
                return false;
            }
        }

        return true;
    }

    private static byte[] DecompressLayerBytes(byte[] bytes, string compression)
    {
        if (string.IsNullOrEmpty(compression))
            return bytes;

        using var input = new MemoryStream(bytes);
        Stream decoder = compression.ToLowerInvariant() switch
        {
            "gzip" => new GZipStream(input, CompressionMode.Decompress),
            "zlib" => new ZLibStream(input, CompressionMode.Decompress),
            _ => throw new NotSupportedException(compression)
        };
        using (decoder)
        {
            using var output = new MemoryStream();
            decoder.CopyTo(output);
            return output.ToArray();
        }
    }

    private static void ParseObjects(
        JsonElement layer,
        int mapHeight,
        int tileSize,
        List<LoadedTileset> tilesets,
        List<TiledObjectData> objects,
        HashSet<int> seenObjectIds,
        List<string> warnings)
    {
        if (!layer.TryGetProperty("objects", out var list) || list.ValueKind != JsonValueKind.Array)
            return;

        foreach (var obj in list.EnumerateArray())
        {
            var id = GetInt(obj, "id");
            if (id != 0 && !seenObjectIds.Add(id))
            {
                warnings.Add($"Duplicate Tiled object id {id} skipped");
                continue;
            }

            var name = GetString(obj, "name") ?? "";
            var type = GetString(obj, "type") ?? GetString(obj, "class") ?? "";
            var props = ReadProperties(obj);
            var xPx = GetFloat(obj, "x");
            var yPx = GetFloat(obj, "y");
            var wPx = GetFloat(obj, "width");
            var hPx = GetFloat(obj, "height");
            var rotationDeg = GetFloat(obj, "rotation");
            var rotation = new Vector3(0, 0, -rotationDeg * (MathF.PI / 180f));

            if (obj.TryGetProperty("ellipse", out _) ||
                obj.TryGetProperty("polygon", out _) ||
                obj.TryGetProperty("polyline", out _))
            {
                warnings.Add($"Object '{(name.Length > 0 ? name : id.ToString())}' shape has no collider");
                objects.Add(new TiledObjectData
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    Properties = props,
                    LocalCenter = PointToLocal(xPx, yPx, mapHeight, tileSize),
                    Rotation = rotation
                });
                continue;
            }

            if (TryGetGid(obj.TryGetProperty("gid", out var gidEl) ? gidEl : default, out var gid) && gid != 0)
            {
                var decoded = DecodeGid(gid, tilesets);
                var tw = wPx > 0 ? wPx : tileSize;
                var th = hPx > 0 ? hPx : tileSize;
                var (center, _, scale) = ToLocal(xPx, yPx, tw, th, mapHeight, tileSize, bottomLeftOrigin: true, scaleToSize: true);
                Vector2 coords = default;
                string? path = null;
                var cell = new Vector2(tileSize, tileSize);
                if (decoded is not null)
                {
                    path = decoded.Value.Tileset.ImagePath;
                    var cols = decoded.Value.Tileset.Columns;
                    var rows = decoded.Value.Tileset.Rows;
                    if (cols > 0 && rows > 0)
                    {
                        var tileX = decoded.Value.Local % cols;
                        var tileY = decoded.Value.Local / cols;
                        coords = new Vector2(tileX, rows - 1 - tileY);
                    }
                }

                objects.Add(new TiledObjectData
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    Properties = props,
                    LocalCenter = center,
                    Rotation = rotation,
                    Scale = scale,
                    SubTexturePath = path,
                    SubTextureCoords = coords,
                    SubTextureCellSize = cell
                });
                continue;
            }

            if (wPx > 0 && hPx > 0)
            {
                var (center, half, _) = ToLocal(xPx, yPx, wPx, hPx, mapHeight, tileSize, bottomLeftOrigin: false, scaleToSize: false);
                var isTrigger = false;
                if (props.TryGetValue("trigger", out var triggerRaw) && !string.IsNullOrWhiteSpace(triggerRaw))
                {
                    if (triggerRaw.Equals("true", StringComparison.OrdinalIgnoreCase) || triggerRaw == "1")
                        isTrigger = true;
                    else if (!triggerRaw.Equals("false", StringComparison.OrdinalIgnoreCase) && triggerRaw != "0")
                        warnings.Add($"Object '{(name.Length > 0 ? name : id.ToString())}' has invalid trigger '{triggerRaw}'");
                }

                objects.Add(new TiledObjectData
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    Properties = props,
                    LocalCenter = center,
                    Rotation = rotation,
                    BoxHalfExtents = half,
                    IsTrigger = isTrigger
                });
                continue;
            }

            objects.Add(new TiledObjectData
            {
                Id = id,
                Name = name,
                Type = type,
                Properties = props,
                LocalCenter = PointToLocal(xPx, yPx, mapHeight, tileSize),
                Rotation = rotation
            });
        }
    }

    private static (Vector3 Center, Vector2 Half, Vector3 Scale) ToLocal(
        float xPx, float yPx, float wPx, float hPx, int mapHeight, int tileSize,
        bool bottomLeftOrigin, bool scaleToSize)
    {
        var t = (float)tileSize;
        var w = wPx / t;
        var h = hPx / t;
        var left = xPx / t;
        var bottom = bottomLeftOrigin ? mapHeight - yPx / t : mapHeight - yPx / t - h;
        var center = new Vector3(left + w * 0.5f, bottom + h * 0.5f, 0);
        return (center, new Vector2(w * 0.5f, h * 0.5f), scaleToSize ? new Vector3(w, h, 1) : Vector3.One);
    }

    private static Vector3 PointToLocal(float xPx, float yPx, int mapHeight, int tileSize)
    {
        var t = (float)tileSize;
        return new Vector3(xPx / t, mapHeight - yPx / t, 0);
    }

    private static bool TryLoadTilesets(
        JsonElement root,
        string mapDirectory,
        int mapTileSize,
        Func<string, string>? toAssetRelative,
        List<string> warnings,
        out List<LoadedTileset> tilesets,
        out string? error)
    {
        tilesets = [];
        error = null;
        if (!root.TryGetProperty("tilesets", out var list) || list.ValueKind != JsonValueKind.Array)
            return true;

        foreach (var entry in list.EnumerateArray())
        {
            var firstGid = GetInt(entry, "firstgid");
            JsonElement def = entry;
            JsonDocument? owned = null;
            var tilesetDir = mapDirectory;

            if (entry.TryGetProperty("source", out var sourceEl))
            {
                var source = sourceEl.GetString();
                if (string.IsNullOrWhiteSpace(source))
                {
                    warnings.Add("Tileset with empty source skipped");
                    continue;
                }

                var tsjPath = Path.GetFullPath(Path.Combine(mapDirectory, source));
                if (!File.Exists(tsjPath))
                {
                    warnings.Add($"Tileset not found: {source}");
                    continue;
                }

                try
                {
                    owned = JsonDocument.Parse(File.ReadAllText(tsjPath));
                    def = owned.RootElement;
                    tilesetDir = Path.GetDirectoryName(tsjPath) ?? mapDirectory;
                }
                catch (Exception ex)
                {
                    owned?.Dispose();
                    error = $"Failed to read tileset '{source}': {ex.Message}";
                    return false;
                }
            }

            using (owned)
            {
                if (!def.TryGetProperty("image", out var imageEl) || string.IsNullOrWhiteSpace(imageEl.GetString()))
                {
                    error = "Collection-of-images tilesets are not supported";
                    return false;
                }

                var tileWidth = GetInt(def, "tilewidth");
                var tileHeight = GetInt(def, "tileheight");
                if (tileWidth < 1)
                    tileWidth = mapTileSize;
                if (tileHeight < 1)
                    tileHeight = mapTileSize;
                if (tileWidth != mapTileSize || tileHeight != mapTileSize)
                {
                    error = "Tileset tile size must match the map tile size";
                    return false;
                }

                var image = imageEl.GetString()!;
                var imageFull = Path.GetFullPath(Path.Combine(tilesetDir, image));
                var stored = toAssetRelative?.Invoke(imageFull) ?? image.Replace('\\', '/');
                var margin = GetInt(def, "margin");
                var spacing = GetInt(def, "spacing");
                var imageW = GetInt(def, "imagewidth");
                var imageH = GetInt(def, "imageheight");
                var jsonColumns = GetInt(def, "columns");
                var tilecount = GetInt(def, "tilecount");
                var columns = jsonColumns > 0
                    ? jsonColumns
                    : TilesetUv.Columns(imageW, mapTileSize, margin, spacing);
                var rows = columns > 0 && tilecount > 0
                    ? (tilecount + columns - 1) / columns
                    : TilesetUv.Rows(imageH, mapTileSize, margin, spacing);

                tilesets.Add(new LoadedTileset(firstGid, stored, margin, spacing, columns, rows));
            }
        }

        tilesets.Sort((a, b) => b.FirstGid.CompareTo(a.FirstGid));
        return true;
    }

    private static DecodedTile? DecodeGid(uint gid, List<LoadedTileset> tilesets)
    {
        var flags = gid & FlipMask;
        var raw = gid & ~FlipMask;
        LoadedTileset? match = null;
        foreach (var ts in tilesets)
        {
            if (raw >= (uint)ts.FirstGid)
            {
                match = ts;
                break;
            }
        }

        if (match is null)
            return null;

        return new DecodedTile(
            (int)(raw - (uint)match.FirstGid),
            match,
            (flags & FlipH) != 0,
            (flags & FlipV) != 0,
            (flags & FlipD) != 0);
    }

    private static bool TryGetGid(JsonElement el, out uint gid)
    {
        gid = 0;
        if (el.ValueKind != JsonValueKind.Number)
            return false;
        if (el.TryGetUInt32(out gid))
            return true;
        if (el.TryGetInt64(out var signed) && signed >= 0)
        {
            gid = (uint)signed;
            return true;
        }

        return false;
    }

    private static Dictionary<string, string> ReadProperties(JsonElement obj)
    {
        var props = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!obj.TryGetProperty("properties", out var el))
            return props;

        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in el.EnumerateArray())
            {
                var name = GetString(p, "name");
                if (string.IsNullOrEmpty(name))
                    continue;
                props[name] = ValueToString(p.TryGetProperty("value", out var v) ? v : default);
            }
        }
        else if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in el.EnumerateObject())
                props[p.Name] = ValueToString(p.Value);
        }

        return props;
    }

    private static string ValueToString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Number => el.TryGetInt64(out var n) ? n.ToString(CultureInfo.InvariantCulture) : el.GetRawText(),
        JsonValueKind.String => el.GetString() ?? "",
        _ => el.GetRawText()
    };

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int GetInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Number)
            return 0;
        return p.TryGetInt32(out var n) ? n : 0;
    }

    private static float GetFloat(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Number)
            return 0;
        return p.TryGetSingle(out var n) ? n : 0;
    }

    private static bool? GetBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        return p.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private sealed record LoadedTileset(int FirstGid, string ImagePath, int Margin, int Spacing, int Columns, int Rows);

    private readonly record struct DecodedTile(int Local, LoadedTileset Tileset, bool HFlip, bool VFlip, bool Diagonal);
}
