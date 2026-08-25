using Editor.Features.Tiled;
using SceneComponents.Rendering;
using Shouldly;

namespace Editor.Tests.Tiled;

public class TiledMapParserTests
{
    [Fact]
    public void OrthogonalMap_ParsesSizeAndLayers()
    {
        var (data, error) = Parse(MapJson("[1,2,0,3]"));
        data.ShouldNotBeNull();
        error.ShouldBeNull();
        data!.Width.ShouldBe(2);
        data.Height.ShouldBe(2);
        data.Layers.Count.ShouldBe(1);
        data.Layers[0].Tiles.Length.ShouldBe(4);
    }

    [Fact]
    public void GidZero_ParsesEmpty()
    {
        var tiles = Parse(MapJson("[0,0,0,0]")).Result!.Layers[0].Tiles;
        tiles.ShouldAllBe(t => t == -1);
    }

    [Fact]
    public void FirstGid_MapsToLocalIndex()
    {
        var layer = Parse(MapJson("[1,2,0,3]")).Result!.Layers[0];
        layer.Tiles[Index(0, 1)].ShouldBe(0);
        layer.Tiles[Index(1, 1)].ShouldBe(1);
        layer.Tiles[Index(0, 0)].ShouldBe(-1);
        layer.Tiles[Index(1, 0)].ShouldBe(2);
    }

    [Fact]
    public void TiledRow0_IsTop_StoredAtHeightMinusOne()
    {
        var layer = Parse(MapJson("[1,1,0,0]")).Result!.Layers[0];
        layer.Tiles[Index(0, 1)].ShouldBe(0);
        layer.Tiles[Index(0, 0)].ShouldBe(-1);
    }

    [Fact]
    public void FlipBits_StrippedFromIndex()
    {
        var gid = 1u | 0x80000000u;
        var layer = Parse(MapJson($"[{gid},0,0,0]")).Result!.Layers[0];
        layer.Tiles[Index(0, 1)].ShouldBe(0);
        layer.Flags[Index(0, 1)].ShouldBe(TileMapComponent.FlipH);
    }

    [Fact]
    public void GzipBase64Layer_MatchesJsonArray()
    {
        var encoded = EncodeLayer("gzip", 1, 2, 0, 3);
        var layer = Parse(MapJson(encoded, extra: "\"encoding\":\"base64\",\"compression\":\"gzip\","))
            .Result!.Layers[0];
        layer.Tiles[Index(0, 1)].ShouldBe(0);
        layer.Tiles[Index(1, 1)].ShouldBe(1);
        layer.Tiles[Index(0, 0)].ShouldBe(-1);
        layer.Tiles[Index(1, 0)].ShouldBe(2);
    }

    [Fact]
    public void CsvLayer_MatchesJsonArray()
    {
        var layer = Parse(MapJson("\"1,2,0,3\"", extra: "\"encoding\":\"csv\",")).Result!.Layers[0];
        layer.Tiles[Index(0, 1)].ShouldBe(0);
        layer.Tiles[Index(1, 0)].ShouldBe(2);
    }

    [Fact]
    public void ExternalTsj_IsResolved()
    {
        var data = Parse(MapJson("[1,0,0,0]")).Result!;
        data.Layers[0].TexturePath.ShouldBe("tiles.png");
    }

    [Fact]
    public void Isometric_IsRejected()
    {
        Parse(MapJson("[1,0,0,0]", orientation: "isometric")).Result.ShouldBeNull();
    }

    [Fact]
    public void Infinite_IsRejected()
    {
        Parse(MapJson("[1,0,0,0]", infinite: true)).Result.ShouldBeNull();
    }

    [Fact]
    public void BadJson_IsRejected()
    {
        TiledMapParser.FromJson("{", Path.GetTempPath()).Result.ShouldBeNull();
    }

    [Fact]
    public void MixedTilesetsOnOneLayer_IsRejected()
    {
        var dir = TiledTestMaps.NewDir();
        File.WriteAllText(Path.Combine(dir, "a.tsj"), Tsj("a.png"));
        File.WriteAllText(Path.Combine(dir, "b.tsj"), Tsj("b.png"));
        var json = """
            {
              "orientation":"orthogonal","infinite":false,
              "width":2,"height":2,"tilewidth":16,"tileheight":16,
              "tilesets":[{"firstgid":1,"source":"a.tsj"},{"firstgid":5,"source":"b.tsj"}],
              "layers":[{"type":"tilelayer","name":"g","data":[1,5,0,0]}]
            }
            """;
        File.WriteAllText(Path.Combine(dir, "map.tmj"), json);
        TiledMapParser.FromFile(Path.Combine(dir, "map.tmj")).Result.ShouldBeNull();
    }

    [Fact]
    public void Rectangle_ConvertsCenterAndHalfExtents()
    {
        var obj = Parse(MapWithObjects("""{"id":1,"name":"wall","x":0,"y":0,"width":32,"height":16}"""))
            .Result!.Objects.Single();
        obj.BoxHalfExtents.ShouldBe(new System.Numerics.Vector2(1f, 0.5f));
        obj.LocalCenter.X.ShouldBe(1f, 0.0001f);
        obj.LocalCenter.Y.ShouldBe(1.5f, 0.0001f);
        obj.IsTrigger.ShouldBeFalse();
    }

    [Fact]
    public void ObjectY_UsesMapHeight_NotWidth()
    {
        var json = """
            {
              "orientation":"orthogonal","infinite":false,
              "width":8,"height":2,"tilewidth":16,"tileheight":16,
              "tilesets":[{"firstgid":1,"source":"tiles.tsj"}],
              "layers":[
                {"type":"tilelayer","name":"ground","width":8,"height":2,"data":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]},
                {"type":"objectgroup","name":"obj","objects":[{"id":1,"name":"wall","x":0,"y":0,"width":16,"height":16}]}
              ]
            }
            """;
        var obj = Parse(json).Result!.Objects.Single();
        obj.LocalCenter.X.ShouldBe(0.5f, 0.0001f);
        obj.LocalCenter.Y.ShouldBe(1.5f, 0.0001f);
    }

    [Fact]
    public void TriggerTrue_SetsIsTrigger()
    {
        var obj = Parse(MapWithObjects(
                """{"id":1,"x":0,"y":0,"width":16,"height":16,"properties":[{"name":"trigger","type":"bool","value":true}]}"""))
            .Result!.Objects.Single();
        obj.IsTrigger.ShouldBeTrue();
    }

    [Fact]
    public void TileObject_UsesAtlasCoordsFromBottom()
    {
        var obj = Parse(MapWithObjects("""{"id":2,"gid":1,"x":0,"y":32,"width":16,"height":16}"""))
            .Result!.Objects.Single();
        obj.SubTexturePath.ShouldBe("tiles.png");
        obj.SubTextureCoords.ShouldBe(new System.Numerics.Vector2(0, 1));
        obj.BoxHalfExtents.ShouldBeNull();
    }

    [Fact]
    public void Ellipse_HasMarkerButNoBox()
    {
        var obj = Parse(MapWithObjects("""{"id":3,"name":"e","x":8,"y":8,"width":16,"height":16,"ellipse":true}"""))
            .Result!.Objects.Single();
        obj.BoxHalfExtents.ShouldBeNull();
        obj.SubTexturePath.ShouldBeNull();
    }

    private static int Index(int x, int y) => y * 2 + x;

    private static (TiledMapData? Result, string? Error) Parse(string mapJson) =>
        TiledTestMaps.ParseMapJson(mapJson);

    private static string Tsj(string image) =>
        $$"""{"tilewidth":16,"tileheight":16,"image":"{{image}}","imagewidth":32,"imageheight":32,"columns":2,"tilecount":4}""";

    private static string MapJson(
        string data,
        string orientation = "orthogonal",
        bool infinite = false,
        string extra = "") =>
        $$"""
        {
          "orientation":"{{orientation}}","infinite":{{infinite.ToString().ToLowerInvariant()}},
          "width":2,"height":2,"tilewidth":16,"tileheight":16,
          "tilesets":[{"firstgid":1,"source":"tiles.tsj"}],
          "layers":[{"type":"tilelayer","name":"ground","width":2,"height":2,{{extra}}"data":{{data}}}]
        }
        """;

    private static string EncodeLayer(string compression, params uint[] gids)
    {
        var raw = new byte[gids.Length * 4];
        for (var i = 0; i < gids.Length; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(i * 4), gids[i]);

        if (compression == "gzip")
        {
            using var ms = new MemoryStream();
            using (var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
                gzip.Write(raw);
            raw = ms.ToArray();
        }

        var b64 = Convert.ToBase64String(raw);
        return $"\"{b64}\"";
    }

    private static string MapWithObjects(string objectsJson) =>
        $$"""
        {
          "orientation":"orthogonal","infinite":false,
          "width":2,"height":2,"tilewidth":16,"tileheight":16,
          "tilesets":[{"firstgid":1,"source":"tiles.tsj"}],
          "layers":[
            {"type":"tilelayer","name":"ground","width":2,"height":2,"data":[0,0,0,0]},
            {"type":"objectgroup","name":"obj","objects":[{{objectsJson}}]}
          ]
        }
        """;
}
